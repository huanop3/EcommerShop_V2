using MainEcommerceService.Models.dbMainEcommer;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    // Add custom methods for RefreshToken here if needed
}

public class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(MainEcommerDBContext context) : base(context)
    {
    }
}