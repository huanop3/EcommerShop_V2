namespace ProductService.DTOs
{
    public class ImageUploadResultDto
    {
        public bool Success { get; set; }
        public string ImageUrl { get; set; }
        public string KeyName { get; set; }
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; }
        public DateTime UploadedAt { get; set; }
        public string? Error { get; set; }
    }

    public class MultipleImageUploadResultDto
    {
        public bool Success { get; set; }
        public List<ImageUploadResultDto> UploadedFiles { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public int TotalUploaded { get; set; }
        public int TotalErrors { get; set; }
    }
}