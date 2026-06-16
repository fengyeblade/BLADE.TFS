using Microsoft.Extensions.Logging;

namespace BLADE.TFS.HOMEGATE.LINUX
{
    public class HomeGateLinuxWorker  : BackgroundService
    {
        public HomeGateLinuxWorker(ILogger<HomeGateLinuxWorker> logger)
        {
            _logger = logger;
        }
        private readonly ILogger<HomeGateLinuxWorker> _logger;
        private BLADE.TFS.HOMEGATE.COMM.WorkCore? WC = null;
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            WC = new COMM.WorkCore();
            var j = await WC.StartUp(AppDomain.CurrentDomain.BaseDirectory);
            if (j.suc)
            {
                while (!stoppingToken.IsCancellationRequested)
                { 
                    await Task.Delay(5000, stoppingToken);
                }
            }
            else
            {
                _logger.LogError("HomeGateWinWorker StartUp Failed: {msg}", j.msg);
            }
            WC.Dispose();
            WC = null;
        }
        public override void Dispose()
        {
           WC?.Dispose();
            base.Dispose();
        }
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (WC != null)
            {
                await WC.Stop();
            }
        }
      
    }
}
