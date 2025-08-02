using BLADE.TCPFORTRESS.CoreNET;
using BLADE.TCPFORTRESS.TFSERVICE_Win_NET;

var builder = Host.CreateDefaultBuilder(args).UseWindowsService() ;
builder.ConfigureServices(   sers =>
    {
        sers.AddHostedService<TfsCoreWork>();
    }
    );

var host = builder.Build();
host.Run();
