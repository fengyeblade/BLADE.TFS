using BLADE.TFS.HOMEGATE.WIN;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<HomeGateWinWorker>();

var host = builder.Build();
host.Run();
