using BLADE.TCPFORTRESS.CoreNET;
using BLADE.TOOLS.BASE;
using BLADE.TOOLS.LOG;
namespace BLADE.TCPFORTRESS.TFSERVICE_Win_NET
{
    public class TfsCoreWork : BackgroundService
    {
        private readonly ILogger<TfsCoreWork> _logger;
        private readonly IHostApplicationLifetime _applicationLifetime;
        public TfsCoreWork(ILogger<TfsCoreWork> logger, IHostApplicationLifetime applicationLifetime)
        {
            _logger = logger; _applicationLifetime = applicationLifetime;
        }
        private bool running = false;
        protected string AppStartPath = "";
        protected int js = 0;
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
                await Task.Delay(1000, stoppingToken);
            }
        }
        public override void Dispose() { 
            running = false;
            base.Dispose();
        }
    }
}
