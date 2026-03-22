using Kaz.Http.Caching;

namespace Kaz.Http.Security
{
    internal static class HmacSha256
    {
        public static string ComputeSignature(string data, string key)
        {
            return CacheService.HMACSHA256Algorithm(data, key);
        }
    }
}
