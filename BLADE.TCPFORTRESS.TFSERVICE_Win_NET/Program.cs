using BLADE.TCPFORTRESS.TFSERVICE_Win_NET;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
