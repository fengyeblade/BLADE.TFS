using BLADE.TFS.HOMEGATE.WIN;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService();
builder.Services.AddHostedService<HomeGateWinWorker>();
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var host = builder.Build();
host.Run();
