using System.Linq.Expressions;

public interface IServiceBase<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByIdAsync(int id);
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
    Task<T?> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task<IEnumerable<T>> WhereAsync(Expression<Func<T, bool>> predicate);
}



public class ServiceBase<T> : IServiceBase<T> where T : class
{
    protected readonly IUnitOfWork _uow;
    protected readonly IRepository<T> _repository;

    public ServiceBase(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public virtual async Task AddAsync(T entity)
    {
        await _repository.AddAsync(entity);
        await _uow.SaveChangesAsync();
    }

    public virtual async Task UpdateAsync(T entity)
    {
        _repository.Update(entity);
        await _uow.SaveChangesAsync();
    }

    public virtual async Task DeleteAsync(int id)
    {
        await _repository.DeleteAsync(id);
        await _uow.SaveChangesAsync();
    }

    public async Task<T?> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        return await _repository.SingleOrDefaultAsync(predicate);
    }

    public async Task<IEnumerable<T>> WhereAsync(Expression<Func<T, bool>> predicate)
    {
         return await _repository.WhereAsync(predicate);
    }
}
