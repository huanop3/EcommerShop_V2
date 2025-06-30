using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace BlazorWebApp.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string email, string resetToken);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendPasswordResetEmailAsync(string email, string resetToken)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("EcommerShop", _configuration["EmailSettings:FromEmail"]));
                message.To.Add(new MailboxAddress("", email));
                message.Subject = "Reset Your Password";

                var resetLink = $"{_configuration["AppSettings:BaseUrl"]}/reset-password?token={resetToken}&email={Uri.EscapeDataString(email)}";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                            <h2 style='color: #333;'>Password Reset Request</h2>
                            <p>You requested to reset your password for your Grocery Shop account.</p>
                            <p>Click the button below to reset your password:</p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{resetLink}' style='background-color: #5caf90; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block;'>Reset Password</a>
                            </div>
                            <p style='color: #666; font-size: 14px;'>This link will expire in 24 hours.</p>
                            <p style='color: #666; font-size: 14px;'>If you didn't request this, please ignore this email.</p>
                            <hr style='margin: 30px 0; border: none; border-top: 1px solid #eee;'>
                            <p style='color: #999; font-size: 12px;'>EcommerShop - Your trusted online store</p>
                        </div>"
                };

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                
                // Sử dụng SecureSocketOptions.StartTls cho port 587
                await client.ConnectAsync(_configuration["EmailSettings:SmtpHost"], 
                    int.Parse(_configuration["EmailSettings:SmtpPort"]), SecureSocketOptions.StartTls);
                
                await client.AuthenticateAsync(_configuration["EmailSettings:Username"], 
                    _configuration["EmailSettings:Password"]);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
                
                Console.WriteLine($"Password reset email sent successfully to {email}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email sending failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}