using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ProductService.Models.dbProduct;

public class JwtAuthService
{
    private readonly string? _key;
    private readonly string? _issuer;
    private readonly string? _audience;
    private readonly ProductDBContext _context;
    public JwtAuthService(IConfiguration Configuration, ProductDBContext db)
    {
        _key = Configuration["jwt:Secret-Key"];
        _issuer = Configuration["jwt:Issuer"];
        _audience = Configuration["jwt:Audience"];
        _context = db;
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

}

