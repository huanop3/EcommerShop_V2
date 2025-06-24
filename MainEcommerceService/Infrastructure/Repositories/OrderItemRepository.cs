using MainEcommerceService.Models.dbMainEcommer;

public interface IOrderItemRepository : IRepository<OrderItem>
{
    // Add custom methods for OrderItem here if needed
}

public class OrderItemRepository : Repository<OrderItem>, IOrderItemRepository
{
    public OrderItemRepository(MainEcommerDBContext context) : base(context)
    {
    }
}