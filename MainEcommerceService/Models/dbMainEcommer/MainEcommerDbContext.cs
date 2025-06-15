using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace MainEcommerceService.Models.dbMainEcommer;

public partial class MainEcommerDBContext : DbContext
{
    public MainEcommerDBContext()
    {
    }

    public MainEcommerDBContext(DbContextOptions<MainEcommerDBContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Address> Addresses { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Client> Clients { get; set; }

    public virtual DbSet<Coupon> Coupons { get; set; }

    public virtual DbSet<LoginLog> LoginLogs { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    public virtual DbSet<OrderStatus> OrderStatuses { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SellerProfile> SellerProfiles { get; set; }

    public virtual DbSet<Shipment> Shipments { get; set; }

    public virtual DbSet<ShipperProfile> ShipperProfiles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserCoupon> UserCoupons { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=MainDbService");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(e => e.AddressId).HasName("PK__Addresse__091C2AFB8678047E");

            entity.HasIndex(e => e.IsDeleted, "IX_Addresses_IsDeleted");

            entity.Property(e => e.AddressLine1).HasMaxLength(255);
            entity.Property(e => e.AddressLine2).HasMaxLength(255);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.User).WithMany(p => p.Addresses)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Addresses_Users");
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.CartId).HasName("PK__Carts__51BCD7B7CD6D23CD");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.User).WithMany(p => p.Carts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Carts_Users");
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.CartItemId).HasName("PK__CartItem__488B0B0ABF9595CB");

            entity.HasIndex(e => e.IsDeleted, "IX_CartItems_IsDeleted");

            entity.HasIndex(e => e.ProductId, "IX_CartItems_ProductId");

            entity.Property(e => e.AddedAt).HasColumnType("datetime");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Cart).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.CartId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CartItems_Carts");
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.ClientId).HasName("PK__Clients__E67E1A243532E993");

            entity.Property(e => e.ClientName).HasMaxLength(255);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DeviceName).HasMaxLength(50);
            entity.Property(e => e.Ipaddress).HasColumnName("Ipaddress ");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.HasKey(e => e.CouponId).HasName("PK__Coupons__384AF1BA2BE549CA");

            entity.HasIndex(e => e.CouponCode, "UQ_Coupons_CouponCode").IsUnique();

            entity.Property(e => e.CouponCode).HasMaxLength(20);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.DiscountPercent).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
            entity.Property(e => e.UsageCount).HasDefaultValue(0);
        });

        modelBuilder.Entity<LoginLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__LoginLog__5E5486482A2ED825");

            entity.ToTable("LoginLog");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.LoginTime).HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
            entity.Property(e => e.Username).HasMaxLength(50);

            entity.HasOne(d => d.Client).WithMany(p => p.LoginLogs)
                .HasForeignKey(d => d.ClientId)
                .HasConstraintName("FK_LoginLog_Clients");

            entity.HasOne(d => d.User).WithMany(p => p.LoginLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LoginLog_Users");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BCFF75D0E6B");

            entity.HasIndex(e => e.IsDeleted, "IX_Orders_IsDeleted");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.OrderDate).HasColumnType("datetime");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Coupon).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CouponId)
                .HasConstraintName("FK_Orders_Coupons");

            entity.HasOne(d => d.OrderStatus).WithMany(p => p.Orders)
                .HasForeignKey(d => d.OrderStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_OrderStatuses");

            entity.HasOne(d => d.ShippingAddress).WithMany(p => p.Orders)
                .HasForeignKey(d => d.ShippingAddressId)
                .HasConstraintName("FK_Orders_Addresses");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Users");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId).HasName("PK__OrderIte__57ED06816F0202CF");

            entity.HasIndex(e => e.IsDeleted, "IX_OrderItems_IsDeleted");

            entity.HasIndex(e => e.ProductId, "IX_OrderItems_ProductId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.TotalPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderItems)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrderItems_Orders");
        });

        modelBuilder.Entity<OrderStatus>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("PK__OrderSta__C8EE206382E6E975");

            entity.HasIndex(e => e.StatusName, "UQ_OrderStatuses_StatusName").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.StatusName).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A388D4EDB4F");

            entity.HasIndex(e => e.IsDeleted, "IX_Payments_IsDeleted");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.PaymentDate).HasColumnType("datetime");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.TransactionId).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Orders");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.TokenId).HasName("PK__RefreshT__658FEEEADABDDC91");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DeviceId).HasMaxLength(100);
            entity.Property(e => e.DeviceName).HasMaxLength(100);
            entity.Property(e => e.DeviceOs)
                .HasMaxLength(50)
                .HasColumnName("DeviceOS");
            entity.Property(e => e.ExpiryDate).HasColumnType("datetime");
            entity.Property(e => e.Ipaddress)
                .HasMaxLength(50)
                .HasColumnName("IPAddress");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.IsRevoked).HasDefaultValue(false);
            entity.Property(e => e.RevokedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Client).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.ClientId)
                .HasConstraintName("FK_RefreshTokens_Clients");

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RefreshTokens_Users");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1A56755B93");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.RoleName).HasMaxLength(50);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<SellerProfile>(entity =>
        {
            entity.HasKey(e => e.SellerId).HasName("PK__SellerPr__7FE3DB81425909A3");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.StoreName).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.User).WithMany(p => p.SellerProfiles)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SellerProfiles_Users");
        });

        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.HasKey(e => e.ShipmentId).HasName("PK__Shipment__5CAD37EDF9594F7B");

            entity.HasIndex(e => e.IsDeleted, "IX_Shipments_IsDeleted");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DeliveredDate).HasColumnType("datetime");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.ShippedDate).HasColumnType("datetime");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.TrackingNumber).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Order).WithMany(p => p.Shipments)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Shipments_Orders");

            entity.HasOne(d => d.Shipper).WithMany(p => p.Shipments)
                .HasForeignKey(d => d.ShipperId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Shipments_ShipperProfiles");
        });

        modelBuilder.Entity<ShipperProfile>(entity =>
        {
            entity.HasKey(e => e.ShipperId).HasName("PK__ShipperP__1F8AFE59FC2C0565");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.User).WithMany(p => p.ShipperProfiles)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ShipperProfiles_UserId");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4CD2E145B4");

            entity.HasIndex(e => e.IsDeleted, "IX_Users_IsDeleted");

            entity.HasIndex(e => e.Email, "UQ_Users_Email").IsUnique();

            entity.HasIndex(e => e.Username, "UQ_Users_Username").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.LastLogin).HasColumnType("datetime");
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
            entity.Property(e => e.Username).HasMaxLength(50);
        });

        modelBuilder.Entity<UserCoupon>(entity =>
        {
            entity.HasKey(e => e.UserCouponId).HasName("PK__UserCoup__22994B533049D85C");

            entity.HasIndex(e => e.IsDeleted, "IX_UserCoupons_IsDeleted");

            entity.HasIndex(e => new { e.UserId, e.CouponId }, "UQ_UserCoupons").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.IsUsed).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
            entity.Property(e => e.UsedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Coupon).WithMany(p => p.UserCoupons)
                .HasForeignKey(d => d.CouponId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserCoupons_Coupons");

            entity.HasOne(d => d.User).WithMany(p => p.UserCoupons)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserCoupons_Users");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.UserRoleId).HasName("PK__UserRole__3D978A35B7DAB393");

            entity.HasIndex(e => new { e.UserId, e.RoleId }, "UQ_UserRoles").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserRoles_Roles");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserRoles_Users");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
