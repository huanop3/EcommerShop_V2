using MainEcommerceService.Models.dbMainEcommer;

public interface IClientRepository : IRepository<Client>
{
    // Add custom methods for Client here if needed
}

public class ClientRepository : Repository<Client>, IClientRepository
{
    public ClientRepository(MainEcommerDBContext context) : base(context)
    {
    }
}