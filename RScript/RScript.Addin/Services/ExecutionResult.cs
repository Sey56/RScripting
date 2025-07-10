namespace RScript.Addin.Services
{
    public class ExecutionResult
    {
        public bool IsSuccess { get; set; }
        public string ResultMessage { get; set; }          // Printable output or final status
        public string ErrorMessage { get; set; }           // High-level error summary
        public string[] ErrorDetails { get; set; }         // Multi-line diagnostics or inner messages
        public dynamic ReturnValue { get; set; }           // If the script returns a value
        public string ScriptName { get; set; }             // Optional: which script triggered this
        public DateTime Timestamp { get; set; }            // Optional: time the result was finalized

        public ExecutionResult()
        {
            Timestamp = DateTime.Now;
            ErrorDetails = Array.Empty<string>();
        }
    }
}
