
public static class CodeGenerator
{

    public static string GenerateTransactionCode(string prefix = "TR")
    {
        string datePart = DateTime.Now.ToString("yyMMdd"); // 250129
        string uniquePart = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();

        return $"{prefix}-{datePart}-{uniquePart}";
    }
}