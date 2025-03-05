namespace RateLimiter.Service
{
    public interface IRateLimiterService
    {
        Task<bool> CanSendMessageAsync(string phoneNumber);

    }
}
