using MainEcommerceService.Models.ViewModel;
using ProductService.Models.ViewModel;

namespace BlazorWebApp.Services
{
    public static class OrderStateService
    {
        private static OrderDataModel? _orderData;

        public static void SaveOrderData(OrderDataModel orderData)
        {
            _orderData = orderData;
        }

        public static OrderDataModel? GetOrderData()
        {
            return _orderData;
        }

        public static void ClearOrderData()
        {
            _orderData = null;
        }
    }

    public class OrderDataModel
    {
        public int? UserId { get; set; }
        public string OrderNumber { get; set; } = "";
        public DateTime OrderDate { get; set; }
        public List<CartVM> Items { get; set; } = new();
        public AddressVM? ShippingAddress { get; set; }
        public string DeliveryOption { get; set; } = "";
        public string PaymentMethod { get; set; } = "";
        public string? DeliveryInstructions { get; set; }
        public PricingBreakdown Pricing { get; set; } = new();
        public CouponVM? AppliedCoupon { get; set; }
        public string? OrderNotes { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
    // ✅ Supporting Models
public class OrderUpdateModel
{
    public string Type { get; set; } = ""; // success, info, warning, error
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime Timestamp { get; set; }
}
}