
using ProductService.Models.dbProduct;

public interface IUnitOfWork : IAsyncDisposable
{
    public ICategoryRepository _categoryRepository { get; }
    public IProductRepository _prodRepository { get; }
    Task BeginTransaction();
    Task CommitTransaction();
    Task RollbackTransaction();
    
    Task<int> SaveChangesAsync();
}

public class UnitOfWork : IUnitOfWork
{
    private readonly ProductDBContext _context;
    public ICategoryRepository _categoryRepository { get; }
    public IProductRepository _prodRepository { get; }

    public UnitOfWork(ProductDBContext context, ICategoryRepository categoryRepository, IProductRepository prodRepository)
    {
        _context = context;
        _categoryRepository = categoryRepository;
        _prodRepository = prodRepository;
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




