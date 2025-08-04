using BLADE.TCPFORTRESS.CoreNET;
using BLADE.TCPFORTRESS.TFSERVICE_Linux64_NET;

var builder = Host.CreateDefaultBuilder(args).UseSystemd();
builder.ConfigureServices(sers =>
{
    sers.AddHostedService<TfsCoreWork>();
}
    );

var host = builder.Build();
host.Run();
