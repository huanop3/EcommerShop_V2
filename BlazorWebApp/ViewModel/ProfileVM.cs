using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MainEcommerceService.Models.ViewModel
{
    /// <summary>
    /// View model dùng để hiển thị thông tin người dùng
    /// </summary>
    public class ProfileVM
    {
        public int Id { get; set; }
        [MaxLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string? FirstName { get; set; }

        [MaxLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string? LastName { get; set; }
        
        [MaxLength(50, ErrorMessage = "Username cannot exceed 50 characters")]
        public string? UserName { get; set; }

        /// <summary>
        /// Địa chỉ email của người dùng
        /// </summary>
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; } = "";

        [Phone(ErrorMessage = "Please enter a valid phone number")]
        public string? PhoneNumber { get; set; }
    }
}