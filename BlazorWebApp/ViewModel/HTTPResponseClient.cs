public class HTTPResponseClient<T>
{
    public int? StatusCode { get; set; } = 200;
    public string? Message { get; set; } = "";
    public DateTime? DateTime { get; set; }
    public T? Data { get; set; }
    public bool Success { get; set; }
}