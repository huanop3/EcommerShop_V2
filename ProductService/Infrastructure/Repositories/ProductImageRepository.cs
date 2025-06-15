using ProductService.Models.dbProduct;

public interface IProductImageRepository : IRepository<ProductImage>
{
    // Add custom methods for ProductImage here if needed
}

public class ProductImageRepository : Repository<ProductImage>, IProductImageRepository
{
    public ProductImageRepository(ProductDBContext context) : base(context)
    {
    }
}