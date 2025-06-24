using MainEcommerceService.Models.dbMainEcommer;

public interface IAddressRepository : IRepository<Address>
{
    // Add custom methods for Address here if needed
}

public class AddressRepository : Repository<Address>, IAddressRepository
{
    public AddressRepository(MainEcommerDBContext context) : base(context)
    {
    }
}
