using ProductService.Models.dbProduct;

public interface IProductRepository : IRepository<Product>
{
    // Add custom methods for Product here if needed
}

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(ProductDBContext context) : base(context)
    {
    }
}