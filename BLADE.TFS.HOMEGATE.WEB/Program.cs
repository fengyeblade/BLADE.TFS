using BLADE.SERVICEWEB.RAZORBODY9;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;


var builder = WebApplication.CreateBuilder(args);
 WebAppCreater.runmode =  HostingEnvironmentDetector.DetectCurrentMode();
 await WebAppCreater.ConfigureBuilder(builder);

var app = builder.Build();

var bsp = await WebAppCreater.ConfigureApp(app);

app.Run();
