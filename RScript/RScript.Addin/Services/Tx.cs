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
            File.AppendAllText(_logPath, $"Starting TransactWithDoc: {transactionName} at {DateTime.Now}\n");

            if (doc == null)
            {
                File.AppendAllText(_logPath, "Document is null in Transact\n");
                throw new InvalidOperationException("Document is null in Transact");
            }

            using var transaction = new Transaction(doc, transactionName);
            try
            {
                File.AppendAllText(_logPath, $"Starting transaction: {transactionName}\n");
                _ = transaction.Start();
                action(doc);
                File.AppendAllText(_logPath, $"Committing transaction: {transactionName}\n");
                _ = transaction.Commit();
                File.AppendAllText(_logPath, $"Transaction committed successfully: {transactionName}\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logPath, $"Exception in TransactWithDoc: {ex.Message}\n");
                if (transaction.GetStatus() == TransactionStatus.Started)
                {
                    File.AppendAllText(_logPath, $"Rolling back transaction: {transactionName}\n");
                    _ = transaction.RollBack();
                }
                throw;
            }
        }
    }
}