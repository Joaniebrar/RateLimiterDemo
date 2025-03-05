
namespace RateLimiter.Service
{
    public class RateLimitSettings
    {
        public int MaxPerNumberPerSecond { get; set; }
        public int MaxGlobalPerSecond { get; set; }
    }
}
