using System.Text.Json;
using StackExchange.Redis;

public class RedisHelper
{
    private IDatabase _db { get; set; }
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IConnectionMultiplexer _redis;

    public RedisHelper(IConnectionMultiplexer redis)
    {
        _redis = redis; // ✅ SỬA: Lưu reference để dùng cho GetServer()
        _db = redis.GetDatabase(0);
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public void setDatabaseRedis(int db)
    {
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
    // ✅ SỬA: Delete single key (giữ nguyên cho backward compatibility)
    public async Task<bool> DeleteAsync(string key) => await _db.KeyDeleteAsync(key);

    // ✅ THÊM: Delete by pattern (wildcard support)
    public async Task<long> DeleteByPatternAsync(string pattern)
    {

            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: pattern).ToArray();
            
            if (keys.Length > 0)
            {
                var deletedCount = await _db.KeyDeleteAsync(keys);
                return deletedCount;
            }
            
            return 0;

    }

    // ✅ THÊM: Delete multiple patterns
    public async Task<long> DeleteByPatternsAsync(params string[] patterns)
    {

            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var allKeys = new List<RedisKey>();

            foreach (var pattern in patterns)
            {
                var keys = server.Keys(pattern: pattern);
                allKeys.AddRange(keys);
            }

            if (allKeys.Count > 0)
            {
                // Remove duplicates
                var uniqueKeys = allKeys.Distinct().ToArray();
                var deletedCount = await _db.KeyDeleteAsync(uniqueKeys);
                
                return deletedCount;
            }

            return 0;
        

    }

    // ✅ THÊM: Get keys by pattern (useful for debugging)
    public async Task<string[]> GetKeysByPatternAsync(string pattern)
    {

            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: pattern).Select(k => k.ToString()).ToArray();
            
            return keys;
        
    }

    // ✅ THÊM: Delete multiple specific keys
    public async Task<long> DeleteKeysAsync(params string[] keys)
    {
        if (keys == null || keys.Length == 0) return 0;

 
            var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
            var deletedCount = await _db.KeyDeleteAsync(redisKeys);
            
            return deletedCount;

    }

    // ✅ THÊM: Check if pattern has any matching keys
    public async Task<bool> ExistsByPatternAsync(string pattern)
    {

            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: pattern).Take(1);
            return keys.Any();

    }

    public async Task<bool> ExistsAsync(string key) => await _db.KeyExistsAsync(key);

    // ✅ THÊM: Clear ALL cache (Nuclear option)
    public async Task<long> ClearAllAsync()
    {

            return await DeleteByPatternAsync("*");

    }
}