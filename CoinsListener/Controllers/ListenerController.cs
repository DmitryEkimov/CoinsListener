using CoinsListener.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CoinsListener.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ListenerController : ControllerBase
    {
        private readonly ILogger<ListenerController> logger;
        private readonly SessionHolderService cancellationTokenHolderService;
        public ListenerController(ILogger<ListenerController> logger, SessionHolderService cancellationTokenHolderService) => (this.logger, this.cancellationTokenHolderService) = (logger, cancellationTokenHolderService);

        // POST: api/<ListenerController>
        [HttpPost("Restart")]
        public void Restart()
        => logger.LogWarning(cancellationTokenHolderService.CancellationTokenSource is not null && cancellationTokenHolderService.CancellationTokenSource.Token.CanBeCanceled ?
                RequestManualCancellation() : "ListenerController: cannot request manual cancellation");

        private string RequestManualCancellation()
        {
            cancellationTokenHolderService.CancellationTokenSource.Cancel();
            return "ListenerController: request manual cancellation";
        }
    }
}
