namespace MainEcommerceService.Models.ViewModel
{
    public class AdminOrdersCompleteView
    {
        public List<OrderWithCompleteDetailsVM> Orders { get; set; } = new();
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public DateTime LoadedAt { get; set; }
    }

    public class OrderWithCompleteDetailsVM : OrderVM
    {
        // Order Status info
        public string OrderStatusName { get; set; } = "";
        
        // Customer info
        public string CustomerFirstName { get; set; } = "";
        public string CustomerLastName { get; set; } = "";
        public string CustomerEmail { get; set; } = "";
        public string CustomerPhone { get; set; } = "";
        
        // Order Items with product details
        public List<OrderItemWithProductDetailsVM> OrderItems { get; set; } = new();
        
        // Computed properties
        public string CustomerFullName => $"{CustomerFirstName} {CustomerLastName}".Trim();
        public string CustomerInitials => 
            (string.IsNullOrEmpty(CustomerFirstName) ? "" : CustomerFirstName[0].ToString()) +
            (string.IsNullOrEmpty(CustomerLastName) ? "" : CustomerLastName[0].ToString());
    }

    public class OrderItemWithProductDetailsVM : OrderItemVM
    {
        public string ProductName { get; set; } = "";
        public string ProductImage { get; set; } = "";
        public int SellerId { get; set; }
        public string SellerStoreName { get; set; } = "";
    }

    public class ProductBasicInfo
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string ProductImage { get; set; } = "";
        public int SellerId { get; set; }
        public string SellerStoreName { get; set; } = "";
    }
}