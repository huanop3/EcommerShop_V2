using System.ComponentModel.DataAnnotations;

namespace MainEcommerceService.Models.ViewModel;

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
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
}


// Tạo ViewModel mới tổng hợp
public class LoginRequestVM
{
    // Thông tin đăng nhập
    [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
    public string? Username { get; set; }

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự")]
    public string Password { get; set; }

    // Thông tin thiết bị
    [Required(ErrorMessage = "ID thiết bị không được để trống")]
    [StringLength(100, ErrorMessage = "ID thiết bị không được vượt quá 100 ký tự")]
    public string DeviceID { get; set; }
    [Required(ErrorMessage = "Tên thiết bị là bắt buộc")]
    [StringLength(100, ErrorMessage = "Tên thiết bị không được vượt quá 100 ký tự")]
    public string DeviceName { get; set; }
    [Required(ErrorMessage = "Hệ điều hành không được để trống")]
    [StringLength(20, ErrorMessage = "Hệ điều hành không được vượt quá 20 ký tự")]
    public string DeviceOS { get; set; }
    [Required(ErrorMessage = "Tên trình duyệt không được để trống")]
    [StringLength(20, ErrorMessage = "Tên trình duyệt không được vượt quá 20 ký tự")]
    public string ClientName { get; set; }
    [Required(ErrorMessage = "Địa chỉ IP không được để trống")]
    [RegularExpression(@"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$|^(([a-zA-Z]|[a-zA-Z][a-zA-Z0-9\-]*[a-zA-Z0-9])\.)*([A-Za-z]|[A-Za-z][A-Za-z0-9\-]*[A-Za-z0-9])$|^\s*((([0-9A-Fa-f]{1,4}:){7}([0-9A-Fa-f]{1,4}|:))|(([0-9A-Fa-f]{1,4}:){6}(:[0-9A-Fa-f]{1,4}|((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3})|:))|(([0-9A-Fa-f]{1,4}:){5}(((:[0-9A-Fa-f]{1,4}){1,2})|:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3})|:))|(([0-9A-Fa-f]{1,4}:){4}(((:[0-9A-Fa-f]{1,4}){1,3})|((:[0-9A-Fa-f]{1,4})?:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))|(([0-9A-Fa-f]{1,4}:){3}(((:[0-9A-Fa-f]{1,4}){1,4})|((:[0-9A-Fa-f]{1,4}){0,2}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))|(([0-9A-Fa-f]{1,4}:){2}(((:[0-9A-Fa-f]{1,4}){1,5})|((:[0-9A-Fa-f]{1,4}){0,3}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))|(([0-9A-Fa-f]{1,4}:){1}(((:[0-9A-Fa-f]{1,4}){1,6})|((:[0-9A-Fa-f]{1,4}){0,4}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:))|(:(((:[0-9A-Fa-f]{1,4}){1,7})|((:[0-9A-Fa-f]{1,4}){0,5}:((25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)(\.(25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)){3}))|:)))(%.+)?\s*$",
            ErrorMessage = "Địa chỉ IP không hợp lệ")]
    public string IPAddress { get; set; }
    [DataType(DataType.DateTime)]
    public DateTime CollectedAt { get; set; }
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