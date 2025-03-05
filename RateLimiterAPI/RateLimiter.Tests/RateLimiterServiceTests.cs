using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Moq;
using RateLimiter.Service;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;

namespace RateLimiter.Tests
{
    public class RateLimiterServiceTests
    {
        private IDistributedCache _distributedCache;
        private Mock<IOptions<RateLimitSettings>> _rateLimitSettingsMock;
        private RateLimiterService _sut;
        private const string _phoneNumber = "123456789";
        private const string _numberKey = $"rl:{_phoneNumber}";
        private const string _globalKey = "rl:global";

        [SetUp]
        public void Setup()
        {
            _distributedCache = new MemoryDistributedCache(new OptionsWrapper<MemoryDistributedCacheOptions>(new MemoryDistributedCacheOptions()));
            _rateLimitSettingsMock = new Mock<IOptions<RateLimitSettings>>();

            _rateLimitSettingsMock.Setup(r => r.Value).Returns(new RateLimitSettings
            {
                MaxPerNumberPerSecond = 5,
                MaxGlobalPerSecond = 50
            });

            _sut = new RateLimiterService(_distributedCache, _rateLimitSettingsMock.Object);
        }
        private async Task SetCacheValue(string key, string value)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(1)
            };
            await _distributedCache.SetStringAsync(key, value,options);
        }

        [Test]
        public async Task CanSendMessageAsync_ShouldReturnFalse_WhenPhoneNumberExceedsLimit()
        {
            await SetCacheValue(_numberKey,"5");
            await SetCacheValue(_globalKey, "49");
            var result = await _sut.CanSendMessageAsync(_phoneNumber);
            result.Should().BeFalse();

        }
        
        [Test]
        public async Task CanSendMessageAsync_ShouldReturnFalse_WhenGlobalNumberExceedsLimit()
        {
            await SetCacheValue(_numberKey, "4");
            await SetCacheValue(_globalKey, "50");
            var result = await _sut.CanSendMessageAsync(_phoneNumber);
            result.Should().BeFalse();

        }
        
        [Test]
        public async Task CanSendMessageAsync_ShouldReturnTrue_WhenLimitsAreNotExceeded()
        {
            await SetCacheValue(_numberKey, "4");
            await SetCacheValue(_globalKey, "49");

            var result = await _sut.CanSendMessageAsync(_phoneNumber);
            result.Should().BeTrue();

        }
        
        [Test]
        public async Task CanSendMessageAsync_ShouldResetTriggeredRateLimit()
        {

            var tasks = new Task[7];
            var i = 0;
            bool anyFailed = false;

            do
            {
                var result = await _sut.CanSendMessageAsync(_phoneNumber);

                if (!result)
                {
                    anyFailed = true;
                    break;
                }
            }
            while (i < 10);
            anyFailed.Should().BeTrue();

            //wait one second for refresh
            await Task.Delay(1000);
            var resultAfterWait = await _sut.CanSendMessageAsync(_phoneNumber);
            resultAfterWait.Should().BeTrue();

        }
        
    }
}