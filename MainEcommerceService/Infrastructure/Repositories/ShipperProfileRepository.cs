using MainEcommerceService.Models.dbMainEcommer;

public interface IShipperProfileRepository : IRepository<ShipperProfile>
{
    // Add custom methods for ShipperProfile here if needed
}

public class ShipperProfileRepository : Repository<ShipperProfile>, IShipperProfileRepository
{
    public ShipperProfileRepository(MainEcommerDBContext context) : base(context)
    {
    }
}