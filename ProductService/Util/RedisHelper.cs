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
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: pattern).ToArray();
            
            if (keys.Length > 0)
            {
                var deletedCount = await _db.KeyDeleteAsync(keys);
                Console.WriteLine($"🗑️ Deleted {deletedCount} keys matching pattern: {pattern}");
                return deletedCount;
            }
            
            Console.WriteLine($"🔍 No keys found matching pattern: {pattern}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error deleting keys by pattern '{pattern}': {ex.Message}");
            throw;
        }
    }

    // ✅ THÊM: Delete multiple patterns
    public async Task<long> DeleteByPatternsAsync(params string[] patterns)
    {
        try
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
                
                Console.WriteLine($"🗑️ Deleted {deletedCount} unique keys from {patterns.Length} patterns");
                return deletedCount;
            }

            Console.WriteLine($"🔍 No keys found for any of the {patterns.Length} patterns");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error deleting keys by patterns: {ex.Message}");
            throw;
        }
    }

    // ✅ THÊM: Get keys by pattern (useful for debugging)
    public async Task<string[]> GetKeysByPatternAsync(string pattern)
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: pattern).Select(k => k.ToString()).ToArray();
            
            Console.WriteLine($"🔍 Found {keys.Length} keys matching pattern: {pattern}");
            return keys;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error getting keys by pattern '{pattern}': {ex.Message}");
            return Array.Empty<string>();
        }
    }

    // ✅ THÊM: Delete multiple specific keys
    public async Task<long> DeleteKeysAsync(params string[] keys)
    {
        if (keys == null || keys.Length == 0) return 0;

        try
        {
            var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
            var deletedCount = await _db.KeyDeleteAsync(redisKeys);
            
            Console.WriteLine($"🗑️ Deleted {deletedCount} out of {keys.Length} specified keys");
            return deletedCount;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error deleting specified keys: {ex.Message}");
            throw;
        }
    }

    // ✅ THÊM: Check if pattern has any matching keys
    public async Task<bool> ExistsByPatternAsync(string pattern)
    {
        try
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var keys = server.Keys(pattern: pattern).Take(1);
            return keys.Any();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error checking existence by pattern '{pattern}': {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string key) => await _db.KeyExistsAsync(key);

    // ✅ THÊM: Clear ALL cache (Nuclear option)
    public async Task<long> ClearAllAsync()
    {
        try
        {
            Console.WriteLine("💥 WARNING: Clearing ALL Redis cache!");
            return await DeleteByPatternAsync("*");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error clearing all cache: {ex.Message}");
            throw;
        }
    }
}