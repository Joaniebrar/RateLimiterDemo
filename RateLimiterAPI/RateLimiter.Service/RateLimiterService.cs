using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace RateLimiter.Service
{
    
    public class RateLimiterService: IRateLimiterService
    {
        private readonly IDistributedCache _distributedCache;
        private static readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly IOptions<RateLimitSettings> _rateLimitSettings;

        public RateLimiterService(IDistributedCache distributedCache, IOptions<RateLimitSettings> rateLimitSettings)
        {
            _distributedCache = distributedCache;
            _rateLimitSettings = rateLimitSettings;
        }

        public async Task<bool> CanSendMessageAsync(string phoneNumber) {

            var numberKey = $"rl:{phoneNumber}";
            var globalKey = "rl:global";

            await _semaphore.WaitAsync();
            try
            {
                var numberCount = await GetRateLimitAsync(numberKey);
                var globalCount = await GetRateLimitAsync(globalKey);
                if (numberCount >= _rateLimitSettings.Value.MaxPerNumberPerSecond ||
                    globalCount >= _rateLimitSettings.Value.MaxGlobalPerSecond)
                    return false;
                await IncrementRateLimitAsync(numberKey);
                await IncrementRateLimitAsync(globalKey);
                return true;
            }
            finally { 
                _semaphore.Release();
            }
        }

        private async Task IncrementRateLimitAsync(string key)
        {
            int newCount = await GetRateLimitAsync(key) + 1;
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(1)
            };

            await _distributedCache.SetStringAsync(key, newCount.ToString(), options);
        }

        private async Task<int> GetRateLimitAsync(string key)
        {
            var value = await _distributedCache.GetStringAsync(key);
            return value != null ? int.Parse(value) : 0;
        }
    }
}
