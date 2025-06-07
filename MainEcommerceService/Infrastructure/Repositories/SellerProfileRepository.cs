using MainEcommerceService.Models.dbMainEcommer;

public interface ISellerProfileRepository : IRepository<SellerProfile>
{
    // Add custom methods for SellerProfile here if needed
}

public class SellerProfileRepository : Repository<SellerProfile>, ISellerProfileRepository
{
    public SellerProfileRepository(MainEcommerDBContext context) : base(context)
    {
    }
}