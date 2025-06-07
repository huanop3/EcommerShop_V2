using System.Text.Json;
using StackExchange.Redis;
public class RedisHelper
{
    private  IDatabase _db{get;set;}
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IConnectionMultiplexer _redis;
    // public int db = 0;
    public RedisHelper(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase(0);
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }
    public void setDatabaseRedis(int db){
        _db = _redis.GetDatabase(db);
    }
    // ===== Save object or any type as JSON string =====
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        string json = JsonSerializer.Serialize(value, _jsonOptions);
        await _db.StringSetAsync(key, json, expiry);
    }

    // ===== Load object or any type from JSON string =====
    public async Task<T?> GetAsync<T>(string key)
    {
        var json = await _db.StringGetAsync(key);
        return json.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>(json!, _jsonOptions);
    }

    // ===== Save plain string =====
    public async Task SetStringAsync(string key, string value, TimeSpan? expiry = null)
    {
        await _db.StringSetAsync(key, value, expiry);
    }

    public async Task<string?> GetStringAsync(string key)
    {
        var value = await _db.StringGetAsync(key);
        return value.IsNullOrEmpty ? null : value.ToString();
    }

    // ===== Delete & Exist =====
    public async Task<bool> DeleteAsync(string key) => await _db.KeyDeleteAsync(key);
    public async Task<bool> ExistsAsync(string key) => await _db.KeyExistsAsync(key);
}