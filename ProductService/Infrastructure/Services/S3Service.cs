using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using ProductService.Infrastructure.Services;

namespace ProductService.Infrastructure.Services
{
    public interface IS3Service
    {
        Task<string> UploadImageAsync(IFormFile file, string keyName);
        Task<bool> DeleteImageAsync(string keyName);
        Task<string> GetPresignedUrlAsync(string keyName, TimeSpan expiry);
        Task<bool> CheckBucketConnectionAsync();
        Task<List<string>> UploadMultipleImagesAsync(List<IFormFile> files, string folderPrefix);
        Task<ListObjectsV2Response> ListObjectsAsync(string prefix, int maxKeys);

    }
    public class S3Service : IS3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private readonly ILogger<S3Service> _logger;

        public S3Service(IAmazonS3 s3Client, IConfiguration configuration, ILogger<S3Service> logger)
        {
            _s3Client = s3Client;
            _bucketName = configuration["AWS:S3:BucketName"];
            _logger = logger;
        }

        public async Task<string> UploadImageAsync(IFormFile file, string keyName)
        {
            try
            {
                using var stream = file.OpenReadStream();

                var request = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = keyName,
                    InputStream = stream,
                    ContentType = file.ContentType,
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
                    TagSet = new List<Tag>
                    {
                        new Tag { Key = "Environment", Value = "Production" },
                        new Tag { Key = "ContentType", Value = "Image" },
                        new Tag { Key = "UploadDate", Value = DateTime.UtcNow.ToString("yyyy-MM-dd") }
                    }
                };

                await _s3Client.PutObjectAsync(request);

                var imageUrl = $"https://{_bucketName}.s3.amazonaws.com/{keyName}";
                _logger.LogInformation($"Image uploaded successfully: {imageUrl}");

                return imageUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading image: {ex.Message}");
                throw new Exception($"Error uploading image: {ex.Message}");
            }
        }

        public async Task<bool> DeleteImageAsync(string keyName)
        {
            try
            {
                var request = new DeleteObjectRequest
                {
                    BucketName = _bucketName,
                    Key = keyName
                };

                await _s3Client.DeleteObjectAsync(request);
                _logger.LogInformation($"Image deleted successfully: {keyName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting image: {keyName}");
                return false;
            }
        }

        public async Task<string> GetPresignedUrlAsync(string keyName, TimeSpan expiry)
        {
            try
            {
                var request = new GetPreSignedUrlRequest
                {
                    BucketName = _bucketName,
                    Key = keyName,
                    Expires = DateTime.UtcNow.Add(expiry),
                    Verb = HttpVerb.GET
                };

                var presignedUrl = await _s3Client.GetPreSignedURLAsync(request);
                _logger.LogInformation($"Presigned URL generated for: {keyName}");

                return presignedUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating presigned URL for: {keyName}");
                throw new Exception($"Error generating presigned URL: {ex.Message}");
            }
        }

        public async Task<bool> CheckBucketConnectionAsync()
        {
            try
            {
                var request = new ListObjectsV2Request
                {
                    BucketName = _bucketName,
                    MaxKeys = 1
                };

                await _s3Client.ListObjectsV2Async(request);
                _logger.LogInformation("S3 bucket connection successful");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "S3 bucket connection failed");
                return false;
            }
        }

        public async Task<List<string>> UploadMultipleImagesAsync(List<IFormFile> files, string folderPrefix)
        {
            var uploadedUrls = new List<string>();

            foreach (var file in files)
            {
                var fileExtension = Path.GetExtension(file.FileName);
                var keyName = $"{folderPrefix}/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}{fileExtension}";

                var url = await UploadImageAsync(file, keyName);
                uploadedUrls.Add(url);
            }

            return uploadedUrls;
        }
        public async Task<ListObjectsV2Response> ListObjectsAsync(string prefix, int maxKeys)
        {
            try
            {
                var request = new ListObjectsV2Request
                {
                    BucketName = _bucketName,
                    Prefix = prefix,
                    MaxKeys = maxKeys
                };

                var response = await _s3Client.ListObjectsV2Async(request);
                _logger.LogInformation($"Listed {response.S3Objects.Count} objects with prefix: {prefix}");
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error listing objects with prefix: {prefix}");
                throw new Exception($"Error listing objects: {ex.Message}");
            }
        }
    }
}