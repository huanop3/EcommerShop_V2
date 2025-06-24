using System.ComponentModel.DataAnnotations;

namespace BlazorWebApp.ViewModel
{
    public class ShipmentDashboardVM
    {
        public OrderInfoVM OrderInfo { get; set; } = new();
        public BuyerInfoVM BuyerInfo { get; set; } = new();
        public ShippingAddressVM ShippingAddress { get; set; } = new();
        public List<OrderItemVM> OrderItems { get; set; } = new();
        public ShipmentInfoVM? ShipmentInfo { get; set; }
        public List<OrderStatusOptionVM> AvailableStatusUpdates { get; set; } = new();
    }

    public class OrderInfoVM
    {
        public int OrderId { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime OrderDate { get; set; }
        public string CurrentOrderStatus { get; set; } = string.Empty;
        public int OrderStatusId { get; set; }
        public string FormattedOrderDate => OrderDate.ToString("dd/MM/yyyy HH:mm");
        public string FormattedTotalAmount => TotalAmount.ToString("N0") + " VND";
    }

    public class BuyerInfoVM
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class ShippingAddressVM
    {
        public int AddressId { get; set; }
        public string AddressLine1 { get; set; } = string.Empty;
        public string AddressLine2 { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string FullAddress => $"{AddressLine1}, {(!string.IsNullOrEmpty(AddressLine2) ? AddressLine2 + ", " : "")}{City}, {State} {PostalCode}, {Country}";
    }

    public class OrderItemVM
    {
        public int OrderItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string FormattedUnitPrice => UnitPrice.ToString("N0") + " VND";
        public string FormattedTotalPrice => TotalPrice.ToString("N0") + " VND";
    }

    public class ShipmentInfoVM
    {
        public int ShipmentId { get; set; }
        public int ShipperId { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string FormattedShippedDate => ShippedDate?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa vận chuyển";
        public string FormattedDeliveredDate => DeliveredDate?.ToString("dd/MM/yyyy HH:mm") ?? "Chưa giao";
        public string FormattedCreatedDate => CreatedDate?.ToString("dd/MM/yyyy HH:mm") ?? "";
    }

    public class AssignedOrderVM
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime OrderDate { get; set; }
        public string BuyerName { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string CurrentStatus { get; set; } = string.Empty;
        public int OrderStatusId { get; set; }
        public string FormattedOrderDate => OrderDate.ToString("dd/MM/yyyy");
        public string FormattedTotalAmount => TotalAmount.ToString("N0") + " VND";
        public string StatusBadgeColor => OrderStatusId switch
        {
            4 => "primary",    // Shipped
            5 => "info",       // In Transit  
            6 => "warning",    // Out for Delivery
            7 => "success",    // Delivered
            9 => "error",      // Returned
            _ => "default"
        };
    }

    public class OrderStatusOptionVM
    {
        public int StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    // Request Models
    public class UpdateShipmentStatusRequest
    {
        [Required(ErrorMessage = "Vui lòng chọn trạng thái mới")]
        public int NewStatusId { get; set; }
        
        public string? Note { get; set; }
        
        public DateTime UpdatedDate { get; set; } = DateTime.Now;
    }

    public class AssignShipmentRequest
    {
        [Required(ErrorMessage = "OrderId là bắt buộc")]
        public int OrderId { get; set; }
        
        [Required(ErrorMessage = "ShipperId là bắt buộc")]
        public int ShipperId { get; set; }
        
        public string? TrackingNumber { get; set; }
    }

    public class ShipmentValidationResponse
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? OrderStatusId { get; set; }
    }

    // Dashboard Summary Models
    public class ShipperDashboardSummaryVM
    {
        public int TotalAssignedOrders { get; set; }
        public int OrdersInProgress { get; set; }
        public int OrdersCompleted { get; set; }
        public int OrdersReturned { get; set; }
        public decimal TotalEarnings { get; set; }
        public double SuccessRate { get; set; }
        public string FormattedTotalEarnings => TotalEarnings.ToString("N0") + " VND";
        public string FormattedSuccessRate => SuccessRate.ToString("F1") + "%";
    }
}