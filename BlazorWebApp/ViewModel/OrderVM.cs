using System.ComponentModel.DataAnnotations;

namespace MainEcommerceService.Models.ViewModel
{
    /// <summary>
    /// View model dùng để hiển thị thông tin thanh toán
    /// </summary>
    public class OrderVM
    {
        public int OrderId { get; set; }

        public int UserId { get; set; }

        public int OrderStatusId { get; set; }

        public DateTime OrderDate { get; set; }

        public decimal TotalAmount { get; set; }

        public int? ShippingAddressId { get; set; }

        public int? CouponId { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool? IsDeleted { get; set; }

        // 🔥 THÊM: Để hỗ trợ tạo order với items
        public List<OrderItemVM>? OrderItems { get; set; }
    }

    // 🔥 THÊM: DTO cho việc tạo order hoàn chỉnh
    public class CreateOrderRequest
    {
        public int UserId { get; set; }
        public int? ShippingAddressId { get; set; }
        public int CouponId { get; set; }
        public List<OrderItemRequest> OrderItems { get; set; } = new List<OrderItemRequest>();
    }

    public class OrderItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class OrderItemVM
    {
        public int OrderItemId { get; set; }

        public int OrderId { get; set; }

        public int ProductId { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool? IsDeleted { get; set; }
    }
    public class OrderStatusVM
    {
        public int StatusId { get; set; }

        public string StatusName { get; set; } = null!;

        public string? Description { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool? IsDeleted { get; set; }
    }
    public class OrderWithDetailsVM : OrderVM
    {
        public List<OrderItemWithProductVM> OrderItems { get; set; } = new List<OrderItemWithProductVM>();
    }

    public class OrderItemWithProductVM : OrderItemVM
    {
        public int SellerId { get; set; }
        public string? ProductName { get; set; }
    }
}
