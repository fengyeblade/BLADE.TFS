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
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 立即返回后台任务，不阻塞 Host 启动，避免 systemd start 卡住 SSH
            return Task.Run(async () =>
            {
                WC = new COMM.WorkCore();
                var j = await WC.StartUp(AppDomain.CurrentDomain.BaseDirectory);
                if (j.suc)
                {
                    _logger.LogInformation("HomeGateLinuxWorker StartUp OK: {msg}", j.msg);
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        await Task.Delay(9800, stoppingToken);
                    }
                }
                else
                {
                    _logger.LogError("HomeGateLinuxWorker StartUp Failed: {msg}", j.msg);
                }
                WC.Dispose();
                WC = null;
            });
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
