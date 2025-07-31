using BLADE.TCPFORTRESS.TFSERVICE_Linux64_NET;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
