using BLADE.TFS.HOMEGATE.LINUX;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSystemd();
builder.Services.AddHostedService<HomeGateLinuxWorker>();

var host = builder.Build();
host.Run();
