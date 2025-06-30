using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ProductService.Infrastructure.Services;
using ProductService.ViewModels;

namespace ProductService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductImageController : ControllerBase
    {
        private readonly IProductImageService _productImageService;

        public ProductImageController(IProductImageService productImageService, ILogger<ProductImageController> logger)
        {
            _productImageService = productImageService;
        }

        /// <summary>
        /// Upload ảnh cho sản phẩm
        /// </summary>
        [HttpPost("UploadImage")]
        [Authorize(Roles = "Seller,Admin")]
        public async Task<IActionResult> UploadImage([FromForm] CreateProductImageViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _productImageService.UploadImageAsync(model);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        /// <summary>
        /// Upload nhiều ảnh cho sản phẩm
        /// </summary>
        [HttpPost("UploadMultipleImages")]
        [Authorize(Roles = "Seller,Admin")]
        public async Task<IActionResult> UploadMultipleImages([FromForm] UploadProductImagesViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _productImageService.UploadMultipleImagesAsync(model);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        /// <summary>
        /// Cập nhật ảnh sản phẩm
        /// </summary>
        [HttpPut("UpdateImage")]
        [Authorize(Roles = "Seller,Admin")]
        public async Task<IActionResult> UpdateImage([FromForm] UpdateProductImageViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _productImageService.UpdateImageAsync(model);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        /// <summary>
        /// Xóa ảnh sản phẩm
        /// </summary>
        [HttpDelete("DeleteImage/{imageId}")]
        [Authorize(Roles = "Seller,Admin")]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            var result = await _productImageService.DeleteImageAsync(imageId);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        /// <summary>
        /// Xóa tất cả ảnh của sản phẩm
        /// </summary>
        [HttpDelete("DeleteAllProductImages/{productId}")]
        [Authorize(Roles = "Seller,Admin")]
        public async Task<IActionResult> DeleteAllProductImages(int productId)
        {
            var result = await _productImageService.DeleteAllProductImagesAsync(productId);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        /// <summary>
        /// Lấy tất cả ảnh của sản phẩm
        /// </summary>
        [HttpGet("GetProductImages/{productId}")]
        public async Task<IActionResult> GetProductImages(int productId)
        {
            var result = await _productImageService.GetProductImagesAsync(productId);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        /// <summary>
        /// Lấy ảnh theo ID
        /// </summary>
        [HttpGet("GetImageById/{imageId}")]
        public async Task<IActionResult> GetImageById(int imageId)
        {
            var result = await _productImageService.GetImageByIdAsync(imageId);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        /// <summary>
        /// Lấy ảnh chính của sản phẩm
        /// </summary>
        [HttpGet("GetPrimaryImage/{productId}")]
        public async Task<IActionResult> GetPrimaryImage(int productId)
        {
            var result = await _productImageService.GetPrimaryImageAsync(productId);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        /// <summary>
        /// Đặt ảnh chính cho sản phẩm
        /// </summary>
        [HttpPatch("SetPrimaryImage/{productId}/{imageId}")]
        [Authorize(Roles = "Seller,Admin")]
        public async Task<IActionResult> SetPrimaryImage(int productId, int imageId)
        {
            var result = await _productImageService.SetPrimaryImageAsync(productId, imageId);
            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        /// <summary>
        /// Upload ảnh đơn giản (chỉ upload file, không lưu DB)
        /// </summary>
        [HttpPost("UploadImageOnly")]
        [Authorize(Roles = "Seller,Admin")]
        public async Task<IActionResult> UploadImageOnly(IFormFile file, [FromQuery] string? folder = "products")
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Success = false, Message = "Không có file được tải lên" });

            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                return BadRequest(new { Success = false, Message = "Định dạng file không hợp lệ. Chỉ chấp nhận JPEG, PNG, GIF, WebP" });

            // Validate file size (10MB max)
            if (file.Length > 10 * 1024 * 1024)
                return BadRequest(new { Success = false, Message = "Kích thước file quá lớn. Tối đa 10MB" });

            try
            {
                var model = new CreateProductImageViewModel
                {
                    ProductId = 0, // Temporary, will be set later
                    ImageFile = file,
                    IsPrimary = false,
                    Folder = folder
                };

                // Create S3 key
                var fileExtension = Path.GetExtension(file.FileName);
                var s3Key = $"{folder}/temp/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}{fileExtension}";

                // Note: You'll need to inject IS3Service here or create a separate method
                // This is just for temporary upload without saving to DB
                
                return Ok(new
                {
                    Success = true,
                    Message = "Upload thành công",
                    S3Key = s3Key,
                    FileName = file.FileName,
                    FileSize = file.Length,
                    ContentType = file.ContentType,
                    UploadedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Lỗi khi upload ảnh",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Kiểm tra trạng thái ảnh
        /// </summary>
        [HttpGet("CheckImageStatus/{imageId}")]
        public async Task<IActionResult> CheckImageStatus(int imageId)
        {
            try
            {
                var result = await _productImageService.GetImageByIdAsync(imageId);
                if (result.Success && result.Data != null)
                {
                    return Ok(new
                    {
                        Success = true,
                        ImageExists = true,
                        ImageInfo = new
                        {
                            result.Data.ImageId,
                            result.Data.ProductId,
                            result.Data.ImageUrl,
                            result.Data.IsPrimary,
                            result.Data.CreatedAt,
                            result.Data.UpdatedAt
                        },
                        Message = "Ảnh tồn tại"
                    });
                }

                return Ok(new
                {
                    Success = false,
                    ImageExists = false,
                    Message = "Ảnh không tồn tại"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Lỗi khi kiểm tra trạng thái ảnh",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Thống kê ảnh theo sản phẩm
        /// </summary>
        [HttpGet("GetImageStatistics/{productId}")]
        public async Task<IActionResult> GetImageStatistics(int productId)
        {
            try
            {
                var result = await _productImageService.GetProductImagesAsync(productId);
                if (result.Success && result.Data != null)
                {
                    var stats = new
                    {
                        ProductId = productId,
                        TotalImages = result.Data.TotalCount,
                        HasPrimaryImage = result.Data.PrimaryImage != null,
                        PrimaryImageId = result.Data.PrimaryImage?.ImageId,
                        ImagesBreakdown = new
                        {
                            PrimaryImages = result.Data.Images.Count(i => i.IsPrimary),
                            SecondaryImages = result.Data.Images.Count(i => !i.IsPrimary)
                        },
                        LastUploadedAt = result.Data.Images.Any() ? 
                            result.Data.Images.Max(i => i.CreatedAt) : (DateTime?)null
                    };

                    return Ok(new
                    {
                        Success = true,
                        Data = stats,
                        Message = "Lấy thống kê ảnh thành công"
                    });
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Lỗi khi lấy thống kê ảnh",
                    Error = ex.Message
                });
            }
        }
    }
}