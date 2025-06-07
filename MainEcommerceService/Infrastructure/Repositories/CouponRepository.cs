using MainEcommerceService.Models.dbMainEcommer;

public interface ICouponRepository : IRepository<Coupon>
{
    // Add custom methods for Coupon here if needed
}

public class CouponRepository : Repository<Coupon>, ICouponRepository
{
    public CouponRepository(MainEcommerDBContext context) : base(context)
    {
    }
}