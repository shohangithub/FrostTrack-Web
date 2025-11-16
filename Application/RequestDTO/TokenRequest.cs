namespace Application.RequestDTO
{
    public class TokenRequest
    {
        public string? Email { get; set; }
        public required string Password { get; set; }
        public string? UserName { get; set; }
        public required bool IsUseEmail { get; set; }
    }
}
