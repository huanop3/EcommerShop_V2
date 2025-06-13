using System;
using System.Collections.Generic;

namespace MainEcommerceService.Models.dbMainEcommer;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? PhoneNumber { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? LastLogin { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual ICollection<LoginLog> LoginLogs { get; set; } = new List<LoginLog>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual ICollection<SellerProfile> SellerProfiles { get; set; } = new List<SellerProfile>();

    public virtual ICollection<ShipperProfile> ShipperProfiles { get; set; } = new List<ShipperProfile>();

    public virtual ICollection<UserCoupon> UserCoupons { get; set; } = new List<UserCoupon>();

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
