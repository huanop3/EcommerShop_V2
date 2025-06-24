using ProductService.Models.dbProduct;

public interface ICategoryRepository : IRepository<Category>
{
    // Add custom methods for Category here if needed
}

public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(ProductDBContext context) : base(context)
    {
    }
}