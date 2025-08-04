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
        protected TfsCORE CORE;
        private bool running = false;
        protected string AppStartPath = "";
        protected int js = 0;
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            { 
               _logger.LogWarning("TfsCoreWork starting at: {time}", DateTimeOffset.Now);
            }

            AppStartPath = System.AppDomain.CurrentDomain.BaseDirectory;
            
            if (await ServiceRunCenter.Init_2(AppStartPath) == 999)
            {
                // Initialization failed, log the error
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError("Initialization failed with error code 999.  " + ServiceRunCenter.Error);
                }
                return;
            }

            CORE = new TfsCORE();

            string sr = await  CORE.StartWork();
            if (sr.Trim().Length > 0)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("CORE.StartWork():  " +sr);
                }
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                xjs++;
                if (xjs > 15)
                {
                    xjs = 0;
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation("TFS State Info:  " + CORE.State);
                    }
                }
                await Task.Delay(20000, stoppingToken);
            }
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning("Tfs Work Out ExecuteAsync(CancellationToken stoppingToken)" );
            }
        }

        private int xjs = 0;
        public override async   Task StopAsync(CancellationToken cancellationToken)
        {
            ServiceRunCenter.LOG.AddLog(false, 86, "StopAsync  Service...");
            CORE.Dispose();
            
            await base.StopAsync(cancellationToken);
        }
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await base.StartAsync(cancellationToken);
        }

        private bool _disposeded = false;
        public override void Dispose() { 
            running = false;
            if (_disposeded)
            { }
            else {
                _disposeded = true;
                StopAsync(CancellationToken.None);
            }
            base.Dispose();
        }
    }
}
