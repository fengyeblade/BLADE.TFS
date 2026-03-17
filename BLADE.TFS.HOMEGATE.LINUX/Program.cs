using BLADE.TFS.HOMEGATE.LINUX;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<HomeGateLinuxWorker>();

var host = builder.Build();
host.Run();
