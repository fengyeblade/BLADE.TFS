using BLADE.TFS.HOMEGATE.WIN;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService();
builder.Services.AddHostedService<HomeGateWinWorker>();

var host = builder.Build();
host.Run();
