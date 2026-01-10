
public static class CodeGenerator
{

    public static string GenerateTransactionCode(string prefix = "TR", int? sequenceNumber = null)
    {
        string datePart = DateTime.UtcNow.ToString("yyMMdd"); // 250129
        
        if (sequenceNumber.HasValue)
        {
            // Use sequential numbering: 001, 002, 003, etc.
            string sequencePart = sequenceNumber.Value.ToString("D3");
            return $"{prefix}-{datePart}-{sequencePart}";
        }
        else
        {
            // Fallback to unique identifier if no sequence provided
            string uniquePart = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
            return $"{prefix}-{datePart}-{uniquePart}";
        }
    }
}