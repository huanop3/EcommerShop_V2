using MainEcommerceService.Models.dbMainEcommer;

public interface IOrderRepository : IRepository<Order>
{
    // Add custom methods for Order here if needed
}

public class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(MainEcommerDBContext context) : base(context)
    {
    }
}