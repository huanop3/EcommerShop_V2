using System.ComponentModel.DataAnnotations;

namespace web_api_base.Models.ViewModel;

public class UserLoginVM
{
    [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
    public string? Username { get; set; }

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự")]
    public string Password { get; set; }

}

public class RegisterLoginVM
{
    [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ 3 đến 50 ký tự")]
    public string Username { get; set; }

    [Required(ErrorMessage = "Họ là bắt buộc")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Họ phải từ 1 đến 50 ký tự")]
    [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Họ chỉ chứa chữ cái và khoảng trắng")]
    public string LastName { get; set; }

    [Required(ErrorMessage = "Tên là bắt buộc")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Tên phải từ 1 đến 50 ký tự")]
    [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Tên chỉ chứa chữ cái và khoảng trắng")]
    public string FirstName { get; set; }

    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
        ErrorMessage = "Mật khẩu phải có ít nhất 1 chữ hoa, 1 chữ thường và 1 số")]
    public string Password { get; set; }

    [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
    [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
    public string ConfirmPassword { get; set; }

    [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
    [Phone(ErrorMessage = "Số điện thoại không đúng định dạng")]
    [RegularExpression(@"^(0|\+84)[3|5|7|8|9][0-9]{8}$",
        ErrorMessage = "Số điện thoại không đúng định dạng Việt Nam")]
    public string PhoneNumber { get; set; }
}
public class UserLoginResponseVM
{
    public string Username { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
}
public class DeviceInfoVM
{
    public string DeviceID { get; set; }
    public string DeviceName { get; set; }
    public string DeviceOS { get; set; }
    public string ClientName { get; set; }
    public string IPAddress { get; set; }
    public DateTime CollectedAt { get; set; }
}
public class LoginRequestVM : DeviceInfoVM
{
    // Thông tin đăng nhập

    public string? Username { get; set; }
    public string Password { get; set; }
    // Thông tin thiết bị
}
public class ForgotPasswordVM
{
    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    public string Email { get; set; }
}

public class ResetPasswordVM
{
    [Required(ErrorMessage = "Token là bắt buộc")]
    public string Token { get; set; }
    [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
    [MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự")]
    public string NewPassword { get; set; }

    [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
    [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
    public string ConfirmPassword { get; set; }
}

public class ForgotPasswordResponseVM
{
    public string Token { get; set; }
    public string Message { get; set; }
    public bool IsSuccess { get; set; }
}

public class ResetPasswordResponseVM
{
    public string Message { get; set; }
    public bool IsSuccess { get; set; }
}

