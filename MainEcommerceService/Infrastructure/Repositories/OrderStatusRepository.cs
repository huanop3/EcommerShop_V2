using MainEcommerceService.Models.dbMainEcommer;

public interface IOrderStatusRepository : IRepository<OrderStatus>
{
    // Add custom methods for OrderStatus here if needed
}

public class OrderStatusRepository : Repository<OrderStatus>, IOrderStatusRepository
{
    public OrderStatusRepository(MainEcommerDBContext context) : base(context)
    {
    }
}