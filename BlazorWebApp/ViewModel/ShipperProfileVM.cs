namespace MainEcommerceService.Models.ViewModel
{
    public class ShipperProfileVM
    {
        public int ShipperId { get; set; }
        public int UserId { get; set; }
        
        // Thông tin từ User table (join)
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? FullName => $"{FirstName} {LastName}".Trim();
        
        // Thông tin từ ShipperProfile table
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsDeleted { get; set; }
    }
}