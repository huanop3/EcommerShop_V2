namespace MainEcommerceService.Models.Kafka
{
    public class OrderCreatedMessage
    {
        public string RequestId { get; set; }
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public List<OrderItemData> OrderItems { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public class OrderItemData
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class ProductUpdateMessage
    {
        public string RequestId { get; set; }
        public int OrderId { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<ProductUpdateResult> UpdatedProducts { get; set; } = new();
    }

    public class ProductUpdateResult
    {
        public int ProductId { get; set; }
        public int UpdatedQuantity { get; set; }
        public int RemainingStock { get; set; }
    }
}