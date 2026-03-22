using System.Security.Cryptography;
using System.Collections.Concurrent;
using System.Text;

namespace Kaz.Http.Caching
{
    internal static class CacheService
    {
        public static string SHA256Algorithm(string url)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(url));

            return Convert.ToHexString(bytes);
        }

        public static string HMACSHA256Algorithm(string data, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            using var hmac = new HMACSHA256(keyBytes);

            var dataBytes = Encoding.UTF8.GetBytes(data);
            var hashBytes = hmac.ComputeHash(dataBytes);

            return Convert.ToHexString(hashBytes);
        }
    }

    internal static class CacheProvider
    {
        private static readonly ConcurrentDictionary<string, string> cache = 
            new ConcurrentDictionary<string, string>();

        private static readonly ConcurrentDictionary<string, string> etags =
            new ConcurrentDictionary<string, string>();

        public static bool TryGetCache(string key, out string? value)
        {
            return cache.TryGetValue(key, out value);
        }

        public static void SetCache(string key, string value)
        {
            cache[key] = value;
        }

        public static void ClearCache()
        {
            cache.Clear();
        }

        public static bool TryGetETag(string key, out string? etag)
        {
            return etags.TryGetValue(key, out etag);
        }

        public static void SetETag(string key, string etag)
        {
            etags[key] = etag;  
        }
    }
}
