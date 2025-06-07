using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MainEcommerceService.Models.ViewModel
{
    /// <summary>
    /// View model dùng để hiển thị thông tin người dùng
    /// </summary>
    public class UserVM
    {
        /// <summary>
        /// ID của người dùng
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Họ tên đầy đủ của người dùng
        /// </summary>
        public string FirstName { get; set; }

        public string LastName { get; set; }
        public string UserName { get; set; }

        /// <summary>
        /// Địa chỉ email của người dùng
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Vai trò của người dùng trong hệ thống
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// Ngày tham gia hệ thống
        /// </summary>
        public DateTime? JoinedDate { get; set; }

        /// <summary>
        /// Trạng thái hoạt động của tài khoản
        /// </summary>
        public bool? IsActive { get; set; }
        public bool? IsDeleted { get; set; }
    }

    /// <summary>
    /// View model dùng để update thông tin người dùng
    /// </summary>
    public class UserListVM
    {
        /// <summary>
        /// ID của người dùng
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Họ tên đầy đủ của người dùng
        /// </summary>
        public string FirstName { get; set; }
        public string LastName { get; set; }
        /// <summary>
        /// Địa chỉ email của người dùng
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Vai trò của người dùng trong hệ thống
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// Trạng thái hoạt động của tài khoản
        /// </summary>
        public bool? IsActive { get; set; }
        public bool? IsDeleted { get; set; }
    }
    /// <summary>
    /// lay role
    /// </summary>
    public class RoleVM
    {
        /// <summary>
        /// Tên của vai trò
        /// </summary>
        public string RoleName { get; set; }
    }
}