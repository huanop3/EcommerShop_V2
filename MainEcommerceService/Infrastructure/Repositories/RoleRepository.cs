using MainEcommerceService.Models.dbMainEcommer;

public interface IRoleRepository : IRepository<Role>
{
    // Add custom methods for Role here if needed
}

public class RoleRepository : Repository<Role>, IRoleRepository
{
    public RoleRepository(MainEcommerDBContext context) : base(context)
    {
    }
}