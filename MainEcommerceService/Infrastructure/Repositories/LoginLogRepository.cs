using MainEcommerceService.Models.dbMainEcommer;

public interface ILoginLogRepository : IRepository<LoginLog>
{
    // Add custom methods for LoginLog here if needed
}

public class LoginLogRepository : Repository<LoginLog>, ILoginLogRepository
{
    public LoginLogRepository(MainEcommerDBContext context) : base(context)
    {
    }
}