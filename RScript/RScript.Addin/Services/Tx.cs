using Autodesk.Revit.DB;
using System;
using System.IO;

namespace RScript.Addin.Services
{
    public static class Tx
    {
        private static readonly string _logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "TxDebugLog.txt");

        public static void TransactWithDoc(Document doc, string transactionName, Action<Document> action)
        {
            File.AppendAllText(_logPath, $"Starting transaction: {transactionName} at {DateTime.Now}\n");

            if (doc == null)
            {
                File.AppendAllText(_logPath, "Document is null in Transact\n");
                throw new InvalidOperationException("Document is null in Transact");
            }

            using var transaction = new Transaction(doc, transactionName);
            try
            {
                _ = transaction.Start();
                action(doc);
                _ = transaction.Commit();
                File.AppendAllText(_logPath, $"Transaction committed: {transactionName}\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logPath, $"Transaction failed: {transactionName} - {ex.Message}\n");
                if (transaction.GetStatus() == TransactionStatus.Started)
                {
                    _ = transaction.RollBack();
                }
                throw;
            }
        }
    }
}