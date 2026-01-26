namespace BLADE.TFS.HOMEGATE.WIN
{
    public class HomeGateWinWorker : BackgroundService
    {
        protected COMM.ServerCore SC;
        private bool running = false;
        protected string AppStartPath = "";
        protected int js = 0;
        private readonly ILogger<HomeGateWinWorker> logger;

        public HomeGateWinWorker(ILogger<HomeGateWinWorker> _logger)
        {
            this.logger = _logger;
            AppStartPath= AppContext.BaseDirectory;
            SC = new COMM.ServerCore(AppStartPath);
              COMM.RunCenter.AddLog("HomeGateWinWorker", "Create Worker", TOOLS.LOG.LogCodeEnum.App);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var sttt = await SC.InitAndStart();
            await COMM.RunCenter.AddLogAsync("HomeGateWinWorker", "Service Start Execute", TOOLS.LOG.LogCodeEnum.App);
            while (!stoppingToken.IsCancellationRequested)
            {
                js++;
                if (js > 5)
                {
                    js = 0;
                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation("HomeGateWinWorker running at: {time}", DateTimeOffset.Now);
                    }
                }
                await Task.Delay(5000, stoppingToken);
            }
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("HomeGateWinWorker Out Execute  at: {time}", DateTimeOffset.Now); 
            }
            await COMM.RunCenter.AddLogAsync("HomeGateWinWorker", "Service Out Execute", TOOLS.LOG.LogCodeEnum.App);
        }

        private bool _disposeded = false;
        public override void Dispose()
        {
            running = false;
            if (_disposeded)
            { }
            else
            {
                _disposeded = true;
                SC?.Dispose();
                StopAsync(CancellationToken.None);
            }
            base.Dispose();
        }
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await COMM.RunCenter.AddLogAsync("HomeGateWinWorker", "Service StopAsync", TOOLS.LOG.LogCodeEnum.App);
            await base.StopAsync(cancellationToken);
        }
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await base.StartAsync(cancellationToken);
        }
    }
}
