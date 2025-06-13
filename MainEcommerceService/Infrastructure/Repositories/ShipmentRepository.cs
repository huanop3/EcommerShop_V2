using MainEcommerceService.Models.dbMainEcommer;

public interface IShipmentRepository : IRepository<Shipment>
{
    // Add custom methods for Shipment here if needed
}

public class ShipmentRepository : Repository<Shipment>, IShipmentRepository
{
    public ShipmentRepository(MainEcommerDBContext context) : base(context)
    {
    }
}