using BCrypt.Net;

namespace MainEcommerceService.Helper;
public static class PasswordHelper
{
    /// <summary>
    /// Hash một mật khẩu sử dụng BCrypt với salt tự động
    /// </summary>
    /// <param name="password">Mật khẩu cần hash</param>
    /// <param name="workFactor">Độ phức tạp (mặc định là 12)</param>
    /// <returns>Chuỗi mật khẩu đã được hash</returns>
    public static string HashPassword(string password="", int workFactor = 12)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("Mật khẩu không được để trống", nameof(password));
        }

        if (workFactor < 4 || workFactor > 31)
        {
            throw new ArgumentException("Work factor phải nằm trong khoảng 4-31", nameof(workFactor));
        }

        return BCrypt.Net.BCrypt.HashPassword(password, workFactor);
    }

    /// <summary>
    /// Xác thực mật khẩu với hash đã lưu
    /// </summary>
    /// <param name="password">Mật khẩu cần kiểm tra</param>
    /// <param name="hashedPassword">Chuỗi hash đã lưu</param>
    /// <returns>True nếu mật khẩu khớp, False nếu không khớp</returns>
    public static bool VerifyPassword(string password="", string hashedPassword="")
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("Mật khẩu không được để trống", nameof(password));
        }

        if (string.IsNullOrEmpty(hashedPassword))
        {
            throw new ArgumentException("Hash không được để trống", nameof(hashedPassword));
        }

        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }
}


