using Microsoft.AspNetCore.Mvc;
using RateLimiter.Service;

namespace RateLimiterAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GateKeeperController : ControllerBase
    {

        private readonly IRateLimiterService _rateLimiterService;

        public GateKeeperController(IRateLimiterService rateLimiterService)
        {
            _rateLimiterService = rateLimiterService;
        }

        [HttpPost("can-send")]
        public async Task<IActionResult> CanSend([FromBody] MessageRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.BusinessPhoneNumber))
            {
                return BadRequest(new { error = "BusinessPhoneNumber is required." });
            }
            bool canSend = await _rateLimiterService.CanSendMessageAsync(request.BusinessPhoneNumber);
            return Ok(new { canSend });
        }
    }

    public class MessageRequest
    {
        public string? BusinessPhoneNumber { get; set; }
    }
}
