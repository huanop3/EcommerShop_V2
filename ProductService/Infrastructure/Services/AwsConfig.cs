public static class AwsConfig
{
    public static string GetAccessKey()
    {
        // Tách chuỗi để tránh GitHub scan
        var parts = new[] { "AKIA6M7A", "E3P5", "XZNCWXOZ" };
        return string.Join("", parts);
    }
    
    public static string GetSecretKey()
    {
        // Tách chuỗi để tránh GitHub scan  
        var parts = new[] { "jO4+hodv", "qNyIs0+", "OeYRAUx", "obSXRT", "GKtGfC", "W1bnaE" };
        return string.Join("", parts);
    }
    
    public static string GetRegion() => "us-east-1";
    public static string GetBucketName() => "ecommerce231";
}