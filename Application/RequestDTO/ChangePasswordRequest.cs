namespace Application.RequestDTO;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);

public record SetPasswordRequest(int UserId, string NewPassword, string ConfirmPassword);
