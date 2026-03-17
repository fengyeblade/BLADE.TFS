using Microsoft.Extensions.Logging;

namespace BLADE.TFS.HOMEGATE.LINUX
{
    public class HomeGateLinuxWorker  : BackgroundService
    {
        protected COMM.ServerCore SC;
        private bool running = false;
        protected string AppStartPath = "";
        protected int js = 0;
        //private readonly ILogger<HomeGateLinuxWorker> logger;

        public HomeGateLinuxWorker()
        {
            AppStartPath = AppContext.BaseDirectory;
            SC = new COMM.ServerCore(AppStartPath);
            COMM.RunCenter.AddLog("HomeGateLinuxWorker", "Create Worker", TOOLS.LOG.LogCodeEnum.App);
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var sttt = await SC.InitAndStart();
            await COMM.RunCenter.AddLogAsync("HomeGateLinuxWorker", "Service Start Execute", TOOLS.LOG.LogCodeEnum.App);
            if (sttt.suc)
            {
                running = true;
                while (!stoppingToken.IsCancellationRequested)
                {
                    js++;
                    if (js > 5)
                    {
                        js = 0;
                         
                         await COMM.RunCenter.AddLogAsync("HomeGateLinuxWorker","I am running");
                         
                    }
                    await Task.Delay(5000, stoppingToken);
                }

                await COMM.RunCenter.AddLogAsync("HomeGateLinuxWorker", " Out Execute while loop"); 
            }
            else
            {
                running = false;
                await COMM.RunCenter.AddLogAsync("HomeGateLinuxWorker", "Service start fail: " + sttt.info, TOOLS.LOG.LogCodeEnum.App);
            }
            await COMM.RunCenter.AddLogAsync("HomeGateLinuxWorker", "Service Out Execute", TOOLS.LOG.LogCodeEnum.App);
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
                _ = StopAsync(CancellationToken.None);
            }
            base.Dispose();
        }
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            SC?.Dispose();
            await COMM.RunCenter.AddLogAsync("HomeGateLinuxWorker", "Service StopAsync", TOOLS.LOG.LogCodeEnum.App);
            await base.StopAsync(cancellationToken);
        }
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await base.StartAsync(cancellationToken);
        }
    }
}
