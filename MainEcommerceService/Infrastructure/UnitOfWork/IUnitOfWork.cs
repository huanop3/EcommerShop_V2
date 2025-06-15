
using MainEcommerceService.Models.dbMainEcommer;

public interface IUnitOfWork : IAsyncDisposable
{
    public IUserRepository _userRepository { get; }
    public IUserRoleRepository _userRoleRepository { get; }
    public IRoleRepository _roleRepository { get; }
    public IClientRepository _clientRepository { get; }
    public ILoginLogRepository _loginLogRepository { get; }
    public ICouponRepository _couponRepository { get; }
    public IAddressRepository _addressRepository { get; }
    public IPaymentRepository _paymentRepository { get; }
    public ISellerProfileRepository _sellerProfileRepository { get; }
    public IRefreshTokenRepository _refreshTokenRepository { get; }
    public IOrderStatusRepository _orderStatusRepository { get; }
    public IOrderItemRepository _orderItemRepository { get; }
    public IOrderRepository _orderRepository { get; }
    public IShipperProfileRepository _shipperProfileRepository { get; }
    public IShipmentRepository _shipmentRepository { get; }
    Task BeginTransaction();
    Task CommitTransaction();
    Task RollbackTransaction();
    
    Task<int> SaveChangesAsync();
}

public class UnitOfWork : IUnitOfWork
{
    public IUserRepository _userRepository { get;}
    public IUserRoleRepository _userRoleRepository { get; }
    public IRoleRepository _roleRepository { get; }
    public IClientRepository _clientRepository { get; }
    public ILoginLogRepository _loginLogRepository { get; }
    public ICouponRepository _couponRepository { get; }
    // public ICategoryRepository _categoryRepository { get; }
    public IRefreshTokenRepository _refreshTokenRepository { get; }
    public IAddressRepository _addressRepository { get; }
    public ISellerProfileRepository _sellerProfileRepository { get; }
    public IPaymentRepository _paymentRepository { get; }
    public IOrderStatusRepository _orderStatusRepository { get; }
    public IOrderItemRepository _orderItemRepository { get; }
    public IOrderRepository _orderRepository { get; }
    public IShipperProfileRepository _shipperProfileRepository { get; }
    public IShipmentRepository _shipmentRepository { get; }

    private readonly MainEcommerDBContext _context;

    public UnitOfWork(MainEcommerDBContext context, IUserRepository userRepository, IUserRoleRepository userRoleRepository, IRoleRepository roleRepository, IClientRepository clientRepository, ILoginLogRepository loginLogRepository, ICouponRepository couponRepository, IRefreshTokenRepository refreshTokenRepository, IAddressRepository addressRepository, ISellerProfileRepository sellerProfileRepository, IPaymentRepository paymentRepository, IOrderStatusRepository orderStatusRepository, IOrderItemRepository orderItemRepository, IOrderRepository orderRepository, IShipperProfileRepository shipperProfileRepository, IShipmentRepository shipmentRepository)
    {
        _context = context;
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _clientRepository = clientRepository;
        _loginLogRepository = loginLogRepository;
        _couponRepository = couponRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _addressRepository = addressRepository;
        _sellerProfileRepository = sellerProfileRepository;
        _paymentRepository = paymentRepository;
        _orderStatusRepository = orderStatusRepository;
        _orderItemRepository = orderItemRepository;
        _orderRepository = orderRepository;
        _shipperProfileRepository = shipperProfileRepository;
        _shipmentRepository = shipmentRepository;
    }
    public async Task BeginTransaction()
    {
        await _context.Database.BeginTransactionAsync();
    }
    public async Task CommitTransaction()
    {
        await _context.Database.CommitTransactionAsync();
    }
    public async Task RollbackTransaction()
    {
        await _context.Database.RollbackTransactionAsync();
    }
    public Task<int> SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }
}




