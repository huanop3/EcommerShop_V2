using Microsoft.AspNetCore.Mvc;
using ProductService.Infrastructure.Services;

namespace ProductService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImageController : ControllerBase
    {
        private readonly IS3Service _s3Service;

        public ImageController(IS3Service s3Service, ILogger<ImageController> logger)
        {
            _s3Service = s3Service;
        }

        [HttpGet("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var isConnected = await _s3Service.CheckBucketConnectionAsync();
                return Ok(new
                {
                    Success = isConnected,
                    Message = isConnected ? "S3 connection successful!" : "S3 connection failed!",
                    BucketName = "ecommerce231",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Error testing connection",
                    Error = ex.Message
                });
            }
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage(IFormFile file, [FromQuery] string? folder = "images")
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Success = false, Message = "No file uploaded" });

            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                return BadRequest(new { Success = false, Message = "Invalid file type. Only JPEG, PNG, GIF, WebP are allowed." });

            // Validate file size (10MB max)
            if (file.Length > 10 * 1024 * 1024)
                return BadRequest(new { Success = false, Message = "File size too large. Maximum 10MB allowed." });

            try
            {
                // Create unique key name
                var fileExtension = Path.GetExtension(file.FileName);
                var keyName = $"{folder}/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}{fileExtension}";

                var imageUrl = await _s3Service.UploadImageAsync(file, keyName);


                return Ok(new
                {
                    Success = true,
                    ImageUrl = imageUrl,
                    KeyName = keyName,
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
                    Message = "Error uploading image",
                    Error = ex.Message
                });
            }
        }

        [HttpPost("upload-multiple")]
        public async Task<IActionResult> UploadMultipleImages(List<IFormFile> files, [FromQuery] string? folder = "images")
        {
            if (files == null || !files.Any())
                return BadRequest(new { Success = false, Message = "No files uploaded" });

            // Validate number of files (max 10)
            if (files.Count > 10)
                return BadRequest(new { Success = false, Message = "Maximum 10 files allowed" });

            var results = new List<object>();
            var errors = new List<string>();

            foreach (var file in files)
            {
                try
                {
                    // Validate each file
                    var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
                    if (!allowedTypes.Contains(file.ContentType.ToLower()))
                    {
                        errors.Add($"{file.FileName}: Invalid file type");
                        continue;
                    }

                    if (file.Length > 10 * 1024 * 1024)
                    {
                        errors.Add($"{file.FileName}: File too large");
                        continue;
                    }

                    // Upload file
                    var fileExtension = Path.GetExtension(file.FileName);
                    var keyName = $"{folder}/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}{fileExtension}";
                    var imageUrl = await _s3Service.UploadImageAsync(file, keyName);

                    results.Add(new
                    {
                        FileName = file.FileName,
                        ImageUrl = imageUrl,
                        KeyName = keyName,
                        FileSize = file.Length,
                        Success = true
                    });
                }
                catch (Exception ex)
                {
                    errors.Add($"{file.FileName}: {ex.Message}");
                }
            }

            return Ok(new
            {
                Success = results.Any(),
                UploadedFiles = results,
                Errors = errors,
                TotalUploaded = results.Count,
                TotalErrors = errors.Count
            });
        }

        [HttpDelete("delete/{*keyName}")]
        public async Task<IActionResult> DeleteImage(string keyName)
        {
            if (string.IsNullOrEmpty(keyName))
                return BadRequest(new { Success = false, Message = "Key name is required" });

            try
            {
                var success = await _s3Service.DeleteImageAsync(keyName);

                if (success)
                {
                    return Ok(new { Success = true, Message = "Image deleted successfully", KeyName = keyName });
                }
                else
                {
                    return BadRequest(new { Success = false, Message = "Failed to delete image", KeyName = keyName });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Error deleting image",
                    Error = ex.Message,
                    KeyName = keyName
                });
            }
        }

        [HttpGet("presigned-url/{*keyName}")]
        public async Task<IActionResult> GetPresignedUrl(string keyName, [FromQuery] int expireHours = 1)
        {
            if (string.IsNullOrEmpty(keyName))
                return BadRequest(new { Success = false, Message = "Key name is required" });

            if (expireHours < 1 || expireHours > 168) // Max 7 days
                return BadRequest(new { Success = false, Message = "Expire hours must be between 1 and 168 (7 days)" });

            try
            {
                var expiry = TimeSpan.FromHours(expireHours);
                var presignedUrl = await _s3Service.GetPresignedUrlAsync(keyName, expiry);

                return Ok(new
                {
                    Success = true,
                    PresignedUrl = presignedUrl,
                    KeyName = keyName,
                    ExpiresIn = $"{expireHours} hours",
                    ExpiresAt = DateTime.UtcNow.AddHours(expireHours)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Error generating presigned URL",
                    Error = ex.Message,
                    KeyName = keyName
                });
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> ListImages([FromQuery] string? prefix = "images/", [FromQuery] int maxKeys = 100)
        {
            try
            {
                var response = await _s3Service.ListObjectsAsync(prefix, maxKeys);

                var images = response.S3Objects.Select(obj => new
                {
                    Key = obj.Key,
                    Size = obj.Size,
                    LastModified = obj.LastModified,
                    Url = $"https://ecommerce231.s3.amazonaws.com/{obj.Key}"
                }).ToList();

                return Ok(new
                {
                    Success = true,
                    Images = images,
                    Count = images.Count,
                    IsTruncated = response.IsTruncated
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Error listing images",
                    Error = ex.Message
                });
            }
        }
    }
}