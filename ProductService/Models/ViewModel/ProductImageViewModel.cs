namespace ProductService.ViewModels
{
    public class ProductImageViewModel
    {
        public int ImageId { get; set; }
        public int ProductId { get; set; }
        public string ImageUrl { get; set; } = null!;
        public bool IsPrimary { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string S3Key { get; set; } = string.Empty;
    }

    public class CreateProductImageViewModel
    {
        public int ProductId { get; set; }
        public IFormFile ImageFile { get; set; } = null!;
        public bool IsPrimary { get; set; } = false;
        public string? Folder { get; set; } = "products";
    }

    public class UpdateProductImageViewModel
    {
        public int ImageId { get; set; }
        public bool? IsPrimary { get; set; }
        public IFormFile? NewImageFile { get; set; }
        public string? Folder { get; set; } = "products";
    }

    public class ProductImageListViewModel
    {
        public List<ProductImageViewModel> Images { get; set; } = new();
        public int TotalCount { get; set; }
        public int ProductId { get; set; }
        public ProductImageViewModel? PrimaryImage { get; set; }
    }

    public class UploadProductImagesViewModel
    {
        public int ProductId { get; set; }
        public List<IFormFile> ImageFiles { get; set; } = new();
        public int? PrimaryImageIndex { get; set; }
        public string? Folder { get; set; } = "products";
    }
}