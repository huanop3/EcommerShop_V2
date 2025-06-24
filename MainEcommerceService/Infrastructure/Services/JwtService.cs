using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using MainEcommerceService.Models.dbMainEcommer;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

public class JwtAuthService
{
    private readonly string? _key;
    private readonly string? _issuer;
    private readonly string? _audience;
    private readonly MainEcommerDBContext _context;
    public JwtAuthService(IConfiguration Configuration, MainEcommerDBContext db)
    {
        _key = Configuration["jwt:Secret-Key"];
        _issuer = Configuration["jwt:Issuer"];
        _audience = Configuration["jwt:Audience"];
        _context = db;
    }

    public string GenerateToken(User userLogin, int minutes=60)
    {
        // Khóa bí mật để ký token
        var key = Encoding.ASCII.GetBytes(_key);
        // Tạo danh sách các claims cho token
        var claims = new List<Claim>
        {
            new Claim("UserId", userLogin.UserId.ToString()), // Claim mặc định cho ID người dùng
            new Claim(ClaimTypes.Name, userLogin.Username), // Claim mặc định cho usernameư
            // new Claim(ClaimTypes.Role, userLogin.UserRoles.FirstOrDefault().Role.RoleName), // Claim mặc định cho vai trò
            // Claim mặc định cho username
            new Claim(JwtRegisteredClaimNames.Sub, userLogin.Username),   // Subject của token
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Unique ID của token
            new Claim(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString()) // Thời gian tạo token
        };
        //Lấy những role chưa bị xóa
        var userRoles = _context.UserRoles
            .Where(ur => ur.UserId == userLogin.UserId && ur.Role.IsDeleted == false)
            .Select(ur => ur.Role.RoleName)
            .ToList();
        // Add role claims
        foreach (var userRole in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, userRole));
        }
        // foreach (var role in userRoles)
        // {
        //     claims.Add(new Claim(ClaimTypes.Role, role));
        // }
        // "role": ["admin", "user"], // Nếu nhiều vai trò, có thể là mảng

        // Tạo khóa bí mật để ký token
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256Signature
        );


        // Thiết lập thông tin cho token
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(minutes), // Token hết hạn sau 1 giờ
            SigningCredentials = credentials,
            Issuer = _issuer,                 // Thêm Issuer vào token
            Audience = _audience,              // Thêm Audience vào token

        };
        // Tạo token bằng JwtSecurityTokenHandler
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        // Trả về chuỗi token đã mã hóa
        return tokenHandler.WriteToken(token);
    }

    public string DecodePayloadToken(string token)
    {
        try
        {
            // Kiểm tra token có null hoặc rỗng không
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("Token không được để trống", nameof(token));
            }
            // Tạo handler và đọc token
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            // Lấy username từ claims (thường nằm trong claim "sub" hoặc "name")
            var usernameClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "unique_name"); // Common in some identity providers
            if (usernameClaim == null)
            {
                throw new InvalidOperationException("Không tìm thấy username trong payload");
            }
            return usernameClaim.Value;
        }
        catch (Exception ex)
        {
            // Xử lý lỗi (có thể log lỗi ở đây)
            throw new InvalidOperationException($"Lỗi khi decode token: {ex.Message}", ex);
        }
    }
    public TokenResult DecodePayloadTokenInfo(string token)
    {
        try
        {
            // Kiểm tra token có null hoặc rỗng không
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("Token không được để trống", nameof(token));
            }
            // Tạo handler và đọc token
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            // Lấy username từ claims (thường nằm trong claim "sub" hoặc "name")
            var usernameClaim = jwtToken.Claims.FirstOrDefault(); // Common in some identity providers
            if (usernameClaim == null)
            {
                throw new InvalidOperationException("Không tìm thấy username trong payload");
            }
            var tokenResult = new TokenResult
            {
                UserId = jwtToken.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value,
                UserName = jwtToken.Claims.FirstOrDefault(c => c.Type == "unique_name")?.Value,
                Role = jwtToken.Claims.FirstOrDefault(c => c.Type == "role")?.Value,
                Sub = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value,
                Jti = jwtToken.Claims.FirstOrDefault(c => c.Type == "jti")?.Value,
                Iat = int.TryParse(jwtToken.Claims.FirstOrDefault(c => c.Type == "iat")?.Value, out var iat) ? iat : 0,
                Nbf = int.TryParse(jwtToken.Claims.FirstOrDefault(c => c.Type == "nbf")?.Value, out var nbf) ? nbf : 0,
                Exp = int.TryParse(jwtToken.Claims.FirstOrDefault(c => c.Type == "exp")?.Value, out var exp) ? exp : 0,
                Iss = jwtToken.Issuer,
                Aud = jwtToken.Audiences.FirstOrDefault()
            };

            return tokenResult;
        }
        catch (Exception ex)
        {
            // Xử lý lỗi (có thể log lỗi ở đây)
          return null;
        }
    }

    

    public string? RefreshToken(string expiredToken)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = DecodePayloadTokenInfo(expiredToken);

            if (principal == null)
            {
                return null;
            }

            // Chuyển từ Unix timestamp sang DateTime
            var expirationTime = DateTimeOffset.FromUnixTimeSeconds(principal.Exp).UtcDateTime;
            var rft = _context.RefreshTokens.FirstOrDefault(token => token.Token == expiredToken);
            // So sánh với thời gian hiện tại (UTC)
            if (DateTime.UtcNow > expirationTime || DateTime.Now > rft.ExpiryDate)
            {
                //Hết hạn thì bắt đăng nhập lại
                return null;
            }
            User? user = _context.Users.FirstOrDefault(u => u.Email == principal.UserName | u.Username == principal.UserName);
            // Tạo token mới
            return GenerateToken(user);
        }
        catch (Exception ex)
        {
            return null;
        }
    }



}

