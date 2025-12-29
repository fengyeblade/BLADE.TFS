using BLADE;
using BLADE.TOOLS;
using BLADE.TOOLS.LOG;
using BLADE.TOOLS.WEB;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.DataProtection; 
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.Extensions.Caching.Memory; 
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using BladeTime = BLADE.TimeProvider;
using BLADE.TOOLS.BASE.ThreadSAFE;
using System.Runtime.InteropServices;


namespace BLADE.SERVICEWEB.RAZORBODY9
{

    /// <summary>
    /// 宿主应用程序 配置 WebApplicationBuilder 的委托定义。
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public delegate ValueTask WebAppConfigureBuilderDelegate(WebApplicationBuilder builder);

    /// <summary>
    /// 当 WEB app 是通过宿主应用程序 管理和启动，使用此类进行。
    /// </summary>
    public class SITEManager : IDisposable
    {
        public WorkSetting WebRunSet { get; private set; }
        private WebApplication? _app;
        public Loger LOG { get; private set; }
        public ILogger<BaseService>? Syslog { get; private set; }
        public string AppStartFullPath { get; private set; } = "";
        public string WwwRootFullPath { get; private set; } = "";
        public string AppDbConnstring { get; private set; } = "";
        public BaseService BS { get; private set; }

        #region SITEManager  
        /// <summary>
        /// 中间层命令服务代理器。  可以通过此属性 设置中间层委托方法。当前台页面调用相关命令时，会触发注册的委托方法，与注册者进行交互。
        /// </summary>
        public MiddleCommandService MiddleDelegate { get { return BS.MCS; } }
        public SITEManager(WorkSetting inset, ILogger<BaseService>? _syslog, Loger _Bladelog, string appstartFullpath, string wwwrootFullpath,string appdbconnstring="")
        {
             
            WebRunSet = inset; LOG = _Bladelog;
            Syslog = _syslog;
            AppStartFullPath = appstartFullpath.Trim();
            WwwRootFullPath = wwwrootFullpath.Trim();
            if (AppStartFullPath.EndsWith("\\") || AppStartFullPath.EndsWith("/")) { }
            else   {  AppStartFullPath = AppStartFullPath + "/";     }
            if (WwwRootFullPath.EndsWith("\\") || WwwRootFullPath.EndsWith("/")) { }
            else { WwwRootFullPath = WwwRootFullPath + "/"; }
            AppDbConnstring= appdbconnstring.Trim();
            BS = new BaseService(AppStartFullPath, WwwRootFullPath, Syslog, LOG, WebRunSet,null, AppDbConnstring);
            BS.RunMode = BaseService.RunMODE.Embedded;
            BS.LogSaveto = WebRunSet.Logsaveto;
            BS.AddLog(100, "SITEManager Init");
        }

        public bool Running { get; private set; } = false;
        /// <summary>
        /// 一个 配置 WEB SERVICE 的委托。  可以设置此委托以自定义 WebApplicationBuilder 的配置过程。
        /// 如果此委托未设置，则使用默认的配置过程。
        /// </summary>
        public WebAppConfigureBuilderDelegate? OnConfigureBuilder { get; set; }
      


        /// <summary>
        /// 启动站点工作。
        /// </summary>
        /// <returns></returns>
        public async ValueTask<ResultMsg> StartSiteWork()
        {
            string rc = "StartSiteWork  Utc: "+BladeTime.UtcNow.ToString("yyyyMMdd HH:mm:ss");

            try
            {
                // TODO :  启动站点
                await MakeWebApp();
                Task.Run(async () => { await _app.RunAsync(); });
                await Task.Delay(300);
                await BS.StartAsync(new CancellationToken());
            }
            catch (Exception ex) {
                rc = rc + " | " + ex.Message;
                await BS.AddLogAsync(104, "StartSiteWork Ex = " + rc);
                BS?.PauseResumTimer(false);
                return new ResultMsg(false,rc,ex);
            }
            Running = true;
            await BS.AddLogAsync(100, "StartSiteWork = "+rc);
            BS.PauseResumTimer(true);
            return new(true, rc);
        }

        /// <summary>
        /// 关闭文本程序。
        /// </summary>
        /// <returns></returns>
        public async Task StopSiteWork()
        {

            await BS.AddLogAsync(100, "StopSiteWork");
            _app?.StopAsync();
            BS.PauseResumTimer(false);
            Running = false;
            return;
        }
        /// <summary>
        /// 释放资源
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void Dispose()
        {
            Running = false;
            _app?.DisposeAsync();
            BS.Dispose();
            throw new NotImplementedException();
        }
        #endregion 

        #region  web app make
        /// <summary>
        /// 配置 WebApplicationBuilder  的服务项目
        /// 
        /// 尝试使用 OnConfigureBuilder 委托进行自定义配置， 如果未设置委托则使用默认配置过程。
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        private async ValueTask ConfigureServices(WebApplicationBuilder builder)
        {
            if (OnConfigureBuilder != null)
            {
                await OnConfigureBuilder.Invoke(builder);
            }
            else
            {
                await ConfigureServices_DEFAULT(builder);
            }
        }

        /// <summary>
        /// 默认的服务配置过程。
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        private async ValueTask ConfigureServices_DEFAULT(WebApplicationBuilder builder)
        {
            ConfigureKestrelAndMiddleware(builder);
            builder.Services.AddDistributedMemoryCache(); // 使用内存缓存作为会话存储（适用于单服务器开发环境）
             
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // 设置 Session 过期时间
                options.Cookie.HttpOnly = true; // 设置 Cookie 为 HttpOnly
                options.Cookie.IsEssential = true; // 标记 Cookie 为必要
                options.Cookie.Name = WebRunSet.AppName + ".AspNetCore.Session";
            });
            builder.Services.AddHostedService(serviceProvider => {
                if (BaseService.Instance != null && BaseService.Instance.Disposed != true) { return BaseService.Instance; }
                else
                {
                    // var logger = serviceProvider.GetRequiredService<ILogger<BaseService>>();
                    var bs = new BaseService(AppStartFullPath, WwwRootFullPath, Syslog, LOG, WebRunSet, null, AppDbConnstring);
                    //BS = bs;
                    return bs;
                }
            });
            builder.Services.AddSingleton(serviceProvider => {
                // var logger = serviceProvider.GetRequiredService<ILogger<BaseService>>();
                if (BaseService.Instance != null && BaseService.Instance.Disposed != true) { return BaseService.Instance; }
                else
                {
                    var bs = new BaseService(AppStartFullPath, WwwRootFullPath, Syslog, LOG, WebRunSet, null, AppDbConnstring);
                    //BS = bs;
                    return bs;
                }
            });

            // Add services to the container.
            builder.Services.AddRazorPages();

            builder.Services.AddMemoryCache();
        }

        /// <summary>
        /// 初始化 WebApplication 站点的工作。
        /// </summary>
        /// <returns></returns>
        private async ValueTask MakeWebApp()
        {
            var builder = WebApplication.CreateBuilder();
            //   builder.Configuration.Sources.Clear();
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole().AddDebug().AddEventLog();

            // 设置内容根路径和Web根路径
            builder.Environment.ContentRootPath = AppStartFullPath;
            builder.Environment.WebRootPath = WwwRootFullPath;
            // 配置数据保护，使用文件系统存储密钥
            var secretpath = Path.Combine(builder.Environment.ContentRootPath, "secret");
            if (!Directory.Exists(secretpath))
            {
                try
                {
                    var di = Directory.CreateDirectory(secretpath);
                    builder.Services.AddDataProtection()
                    .PersistKeysToFileSystem(di).SetApplicationName(WebRunSet.AppName);
                }
                catch (Exception ze2) { BaseService.ProgramStepError = "make dir secret error = " + ze2.Message; }
            }
            else
            {
                builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(secretpath)).SetApplicationName(WebRunSet.AppName);
            }
            await Task.Delay(200);
            // 配置其他服务 （使用委托调用自定义配置过程，或使用默认配置过程）
            await ConfigureServices(builder);

            // 构建 WebApplication 实例
            _app = builder.Build();

            // 配置中间件管道
         


            // 配置其他中间件
            _app.UseForwardedHeaders();
            if (WebRunSet.UseHttps)
            {
                _app.UseHttpsRedirection();
            }

            _app.UseRouting();

            _app.UseAuthorization();
            _app.MapStaticAssets();
            _app.UseSession();


            if (_app.Environment.IsDevelopment())
            {
                // 配置开发环境的异常页面
                _app.UseDeveloperExceptionPage();
            }
            else
            {
                // 配置全局异常处理中间件
                _app.UseExceptionHandler(errorApp =>
                {
                    errorApp.Run(async context =>
                    {
                        var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
                        try
                        {
                            var exception = exceptionHandlerPathFeature.Error;

                            // 获取 LogService 实例
                            //  var logService = context.RequestServices.GetRequiredService<BackService>();

                            // 记录异常信息
                            if (BaseService.Instance != null) { await BaseService.Instance.AddLogAsync(444, $"【 Exception: {exception.Message}】  , StackTrace: {exception.StackTrace}"); }
                            context.Session.SetString("ExceptionMessage", exception.Message);
                            context.Session.SetString("ExceptionStackTrace", exception.StackTrace);
                        }
                        catch (Exception z)
                        {
                            context.Session.SetString("ExceptionStackTrace", z.ToString());
                        }
                        // 重定向到 Error 页面
                        context.Response.Redirect(WebRunSet.ErrorPage);
                    });
                });
            }
            _app.UseDefaultFiles(new DefaultFilesOptions
            {
                DefaultFileNames = new List<string> { WebRunSet.IndexPage }
            });
            _app.MapRazorPages()
               .WithStaticAssets();

            // 完成基本配置， 准备运行应用程序
        }


        /// <summary>
        /// 根据  运行模式 和 运行设置 配置 Kestrel 服务器和相关中间件。
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="_cbs"></param>
        /// <param name="_runset"></param>
        private  void ConfigureKestrelAndMiddleware(WebApplicationBuilder builder)
        {
            // 配置Kestrel服务器选项[1,4](@ref)
            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                if (BS.RunMode == BaseService.RunMODE.WebBack)
                {
                    // 存在IIS 或Nginx 等反向代理模式：遵从默认配置

                }
                else
                {
                   //  独立运行模式或嵌入模式，需要自行处理HTTP/HTTPS端口和证书
                    // HTTP端点配置
                    serverOptions.Listen(IPAddress.Any, WebRunSet.Port_Http);

                    // HTTPS端点配置
                    if (WebRunSet.UseHttps)
                    {
                        serverOptions.Listen(IPAddress.Any, WebRunSet.Port_Https, listenOptions =>
                        {
                            ConfigureHttps(listenOptions );
                        });
                    }
                }
            });

            // 配置HTTPS重定向和HSTS（仅在需要直接处理HTTPS时）[4](@ref)
            if (BS.RunMode != BaseService.RunMODE.WebBack && WebRunSet.UseHttps)
            {
                builder.Services.AddHttpsRedirection(options =>
                {
                    options.HttpsPort = WebRunSet.Port_Https;
                });

                // 在生产环境中启用HSTS
                if (builder.Environment.IsProduction())
                {
                    builder.Services.AddHsts(options =>
                    {
                        options.Preload = true;
                        options.IncludeSubDomains = true;
                        options.MaxAge = TimeSpan.FromDays(60);
                    });
                }
            }

            // 如果运行在反向代理后，需要配置转接头中间件[4](@ref)
            //if (_cbs.RunMode == BaseService.RunMODE.WebBack && _runset.UseHttps)
            //{
            //    builder.Services.Configure<ForwardedHeadersOptions>(options =>
            //    {
            //        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
            //                                  ForwardedHeaders.XForwardedProto;
            //        // 根据您的反向代理配置调整已知网络
            //        options.KnownNetworks.Clear();
            //        options.KnownProxies.Clear();
            //    });
            //}
        }

        /// <summary>
        /// 配置https 的工作
        /// </summary>
        /// <param name="listenOptions"></param>
        /// <param name="runSet"></param>
        private  void ConfigureHttps(ListenOptions listenOptions)
        {
            
            try
            {
                if (!string.IsNullOrEmpty(WebRunSet.CertFileFull) && File.Exists(WebRunSet.CertFileFull))
                {
                    string tpp = WebRunSet.GetCertPass();

                    // 从文件加载证书
                    var certificate = X509CertificateLoader.LoadPkcs12FromFile(WebRunSet.CertFileFull, tpp);
                    listenOptions.UseHttps(certificate);
                    return;
                    // Console.WriteLine("使用证书文件配置HTTPS");
                }
                else
                {
                    LOG?.AddLog(333, "ConfigureHttps Cert Error = nullstring");
                }
            }
            catch (Exception ze)
            {
                // 记录日志或处理异常
                LOG?.AddLog(333, "ConfigureHttps Cert Error = " + ze.Message);
            }
            try
            {
                var certificate = X509CertificateLoader.LoadPkcs12FromFile(AppStartFullPath + "defcert.pfx", WebRunSet.GetCertPass());
                listenOptions.UseHttps(certificate);
                return;
            }
            catch (Exception ze2)
            {
                LOG?.AddLog(333, "ConfigureHttps DefCert.pfx Error = " + ze2.Message);
            }


            // 使用默认证书
            listenOptions.UseHttps();

        }

        #endregion
    }

    /// <summary>
    /// 当 WEB APP 作为独立应用程序运行时，  program.cs 会使用此类进行配置和启动。
    /// </summary>
    public class WebAppCreater
    {
        private static async Task<string> LoadRunset()
        {
            string st = "";
            WorkSetting ws = new WorkSetting();
            string cfg = appfullpath + "RunSet.cfg";
            if (File.Exists(cfg))
            {
                try
                {
                    using (var fs = File.OpenText(cfg))
                    {
                        string wsj = await fs.ReadToEndAsync();
                        var tws = BLADE.TOOLS.BASE.Json.JsonOptions.Deserialize<WorkSetting>(wsj);
                        if (tws != null)
                        {
                            ws = tws;
                            st = st + "Load File OK : "+cfg;
                        }
                    }
                }
                catch (Exception ze)
                { 
                    st = "Load RunSet.cfg error = " + ze.Message +"   Try to RemakeFile :"+cfg;
                    try {
                        using (var fs = File.CreateText(cfg))
                        {
                            string wsj = BLADE.TOOLS.BASE.Json.JsonOptions.Serialize<WorkSetting>(ws);
                            await fs.WriteAsync(wsj);
                        }
                        st = st + "   | RemakeFile Done.";
                    }
                    catch (Exception ze2)
                    { 
                       st = st + "   | RemakeFile error = " + ze2.Message;
                    }
                }
            }
            else {
                try
                {
                    using (var fs = File.CreateText(cfg))
                    {
                        string wsj = BLADE.TOOLS.BASE.Json.JsonOptions.Serialize<WorkSetting>(ws);
                        await fs.WriteAsync(wsj);
                    }
                }
                catch (Exception ze3)
                { 
                   st =st +"   | Create New File error = " + ze3.Message;
                }
            }
            RunSET = ws;
            return st;
        }
        private static string appfullpath = "";
        private static string wwwrootfullpath = "";
        private static string loadTmp = "";
        private static string appdbstr = "";
          
        private static string createLoger()
        {
            try
            {
                string lgsph = wwwrootfullpath + RunSET.logsubdir + "/";
                try { if (!Directory.Exists(lgsph)) { Directory.CreateDirectory(lgsph); } } catch { }
                Loger _LG = new Loger(lgsph, "WL", true, 500, RunSET.BackWorkTimerMins * 61);
                _LG.Debug = RunSET.EnableDeBug;
                RunLOG = _LG;
            }
            catch (Exception ze)
            {
                return "Create Loger Error = " + ze.Message;
            }
            return "";
        }
        public static WorkSetting RunSET { get; set; }
        public static Loger RunLOG { get; set; }
        private static void ConfigureHttps(ListenOptions listenOptions)
        {
            try
            {
                if (!string.IsNullOrEmpty(RunSET.CertFileFull) && File.Exists(RunSET.CertFileFull))
                {
                    string tpp = RunSET.GetCertPass();

                    // 从文件加载证书
                    var certificate = X509CertificateLoader.LoadPkcs12FromFile(RunSET.CertFileFull, tpp);
                    listenOptions.UseHttps(certificate);
                    return;
                    // Console.WriteLine("使用证书文件配置HTTPS");
                }
                else {
                    RunLOG?.AddLog(333, "ConfigureHttps Cert Error = nullstring");
                }
            }catch (Exception ze)
            {
                // 记录日志或处理异常
                RunLOG?.AddLog(333, "ConfigureHttps Cert Error = " + ze.Message);
            }
            try {
                var certificate = X509CertificateLoader.LoadPkcs12FromFile(appfullpath+"defcert.pfx", RunSET.GetCertPass());
                listenOptions.UseHttps(certificate);
                return;
            }
            catch (Exception ze2)
            {
                RunLOG?.AddLog(333, "ConfigureHttps DefCert.pfx Error = " + ze2.Message);
            }


            // 使用默认证书
            listenOptions.UseHttps();
        }
        private static void ConfigureKestrelAndMiddleware(WebApplicationBuilder builder)
        {
            // 配置Kestrel服务器选项[1,4](@ref)
            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                if (runmode == BaseService.RunMODE.WebBack)
                {
                    // 存在IIS 或Nginx 等反向代理模式：遵从默认配置

                }
                else
                {
                    //  独立运行模式或嵌入模式，需要自行处理HTTP/HTTPS端口和证书
                    // HTTP端点配置
                    serverOptions.Listen(IPAddress.Any, RunSET.Port_Http);

                    // HTTPS端点配置
                    if (RunSET.UseHttps)
                    {
                        serverOptions.Listen(IPAddress.Any, RunSET.Port_Https, listenOptions =>
                        {
                            ConfigureHttps(listenOptions);
                        });
                    }
                }
            });

            // 配置HTTPS重定向和HSTS（仅在需要直接处理HTTPS时）[4](@ref)
            if (runmode != BaseService.RunMODE.WebBack && RunSET.UseHttps)
            {
                builder.Services.AddHttpsRedirection(options =>
                {
                    options.HttpsPort = RunSET.Port_Https;
                });

                // 在生产环境中启用HSTS
                if (builder.Environment.IsProduction())
                {
                    builder.Services.AddHsts(options =>
                    {
                        options.Preload = true;
                        options.IncludeSubDomains = true;
                        options.MaxAge = TimeSpan.FromDays(60);
                    });
                }
            }

        }
        public static async ValueTask ConfigureBuilder(WebApplicationBuilder builder,string dbconstr="",string appname="")
        {
            // 设置内容根路径和Web根路径
            //builder.Environment.ContentRootPath = AppStartFullPath;
            //builder.Environment.WebRootPath = WwwRootFullPath;
            // 配置数据保护，使用文件系统存储密钥
            dbconstr= dbconstr.Trim();
            appdbstr = dbconstr;
            appname =appname.Trim();
            if (string.IsNullOrEmpty(appname))
            { appname = "DefAppName"; }
            appfullpath= builder.Environment.ContentRootPath;
            wwwrootfullpath= builder.Environment.WebRootPath;
            if (appfullpath.EndsWith("\\") || appfullpath.EndsWith("/")) { } else { appfullpath = appfullpath + "/"; }
            if (wwwrootfullpath.EndsWith("\\") || wwwrootfullpath.EndsWith("/")) { } else { wwwrootfullpath = wwwrootfullpath + "/"; }
            var secretpath = Path.Combine(builder.Environment.ContentRootPath, "secret");
            if (!Directory.Exists(secretpath))
            {
                try
                {
                    var di = Directory.CreateDirectory(secretpath);
                    builder.Services.AddDataProtection()
                    .PersistKeysToFileSystem(di).SetApplicationName(appname);
                }
                catch (Exception ze2) { BaseService.ProgramStepError = "make dir secret error = " + ze2.Message; }
            }
            else
            {
                builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(secretpath)).SetApplicationName(appname);
            }
            await Task.Delay(300);
            loadTmp = await LoadRunset() + "  | " + createLoger();


            ConfigureKestrelAndMiddleware(builder);

            builder.Services.AddDistributedMemoryCache(); // 使用内存缓存作为会话存储（适用于单服务器开发环境）

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // 设置 Session 过期时间
                options.Cookie.HttpOnly = true; // 设置 Cookie 为 HttpOnly
                options.Cookie.IsEssential = true; // 标记 Cookie 为必要
                options.Cookie.Name = appname + ".AspNetCore.Session";
            });
            
            builder.Services.AddHostedService(serviceProvider => {
                if (BaseService.Instance != null && BaseService.Instance.Disposed != true) { return BaseService.Instance; }
                else
                {
                     var logger = serviceProvider.GetRequiredService<ILogger<BaseService>>();
                    var bs = new BaseService(builder.Environment.ContentRootPath, builder.Environment.WebRootPath, logger, RunLOG, RunSET, null, dbconstr);
                    bs.RunMode = runmode;
                    //BS = bs;
                    return bs;
                }
            });
            builder.Services.AddSingleton(serviceProvider => {
                if (BaseService.Instance != null && BaseService.Instance.Disposed != true) { return BaseService.Instance; }
                else
                {
                     var logger = serviceProvider.GetRequiredService<ILogger<BaseService>>();
                    var bs = new BaseService(builder.Environment.ContentRootPath, builder.Environment.WebRootPath, logger, RunLOG, RunSET, null, dbconstr);
                    bs.RunMode = runmode;
                    //BS = bs;
                    return bs;
                }
            });

            // Add services to the container.
            builder.Services.AddRazorPages();

            builder.Services.AddMemoryCache();
        }
        public static BaseService.RunMODE runmode = BaseService.RunMODE.Standalone;
        public static async Task<BaseService> ConfigureApp(WebApplication _app)
        {
            // 配置中间件管道
           
            var _log = _app.Services.GetRequiredService<ILogger<BaseService>>();
            var bsp = new BaseService(appfullpath, wwwrootfullpath, _log, RunLOG, RunSET, null, appdbstr);
            bsp.LogSaveto = RunSET.Logsaveto;
            

            // 配置其他中间件
            _app.UseForwardedHeaders();
            if (RunSET.UseHttps)
            {
                _app.UseHttpsRedirection();
            }
            _app.UseRouting();
           
            _app.UseAuthorization();
            _app.MapStaticAssets();
            _app.UseSession();
            if (_app.Environment.IsDevelopment())
            {
                // 配置开发环境的异常页面
                _app.UseDeveloperExceptionPage();
            }
            else
            {
                // 配置全局异常处理中间件
                _app.UseExceptionHandler(errorApp =>
                {
                    errorApp.Run(async context =>
                    {
                        var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
                        try
                        {
                            var exception = exceptionHandlerPathFeature.Error;


                            // 记录异常信息
                            if (BaseService.Instance != null) { await BaseService.Instance.AddLogAsync(444, $"【 Exception: {exception.Message}】  , StackTrace: {exception.StackTrace}"); }
                            context.Session.SetString("ExceptionMessage", exception.Message);
                            context.Session.SetString("ExceptionStackTrace", exception.StackTrace);
                        }
                        catch (Exception z)
                        {
                            context.Session.SetString("ExceptionStackTrace", z.ToString());
                        }
                        // 重定向到 Error 页面
                        context.Response.Redirect(RunSET.ErrorPage);
                    });
                });
            }
            _app.UseDefaultFiles(new DefaultFilesOptions
            {
                DefaultFileNames = new List<string> { RunSET.IndexPage }
            });
            _app.MapRazorPages()
               .WithStaticAssets();
            return bsp;
        }
    }

    public static class HostingEnvironmentDetector
    {
        private const string EmbeddedModeFlag = "EMBEDDED_HOST_MODE";

        public static BaseService.RunMODE DetectCurrentMode()
        {
            // 首先检查是否由SITEManager启动的嵌入式模式
            if (IsEmbeddedInHost())
            {
                return BaseService.RunMODE.Embedded;
            }

            // 由program.cs启动时，判断是反向代理托管还是独立运行
            return IsRunningBehindReverseProxy() ? BaseService.RunMODE.WebBack : BaseService.RunMODE.Standalone;
        }

        // 检查是否由SITEManager启动的嵌入式模式
        private static bool IsEmbeddedInHost()
        {
            // 方法1：检查环境变量标志（由SITEManager设置）
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(EmbeddedModeFlag)))
            {
                return true;
            }

            // 方法2：检查程序启动参数
            var args = Environment.GetCommandLineArgs();
            if (args.Any(arg => arg.Equals("--embedded-host", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // 方法3：检查特定的宿主程序进程或程序集特征
            if (IsRunningUnderSpecificHost())
            {
                return true;
            }

            return false;
        }

        // 检查是否运行在反向代理后面
        private static bool IsRunningBehindReverseProxy()
        {
            // 方法1：检查标准ASP.NET Core环境变量[5](@ref)
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_HOSTING")))
            {
                return true;
            }

            // 方法2：检查IIS相关环境变量[5](@ref)
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APP_POOL_ID")) ||
                !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IIS_URLS")))
            {
                return true;
            }

            // 方法3：检查是否在IIS Express下运行（开发环境）
            if (IsRunningUnderIISExpress())
            {
                return true;
            }

            // 方法4：检查是否有反向代理特有的头文件或模块加载
            if (HasReverseProxyIndicators())
            {
                return true;
            }

            return false;
        }

        private static bool IsRunningUnderSpecificHost()
        {
            // 实现您的特定宿主程序检测逻辑
            // 例如：检查进程名、加载的程序集、特定的环境变量等
            var processName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;

            // 示例：检查是否存在宿主程序特有的环境变量
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MY_HOST_APPLICATION")))
            {
                return true;
            }

            // 示例：检查程序集加载情况
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            if (assemblies.Any(asm => asm.FullName != null &&
                asm.FullName.Contains("MyHostApplication")))
            {
                return true;
            }

            return false;
        }

        private static bool IsRunningUnderIISExpress()
        {
            // 检查是否在IIS Express下运行[5](@ref)
            var processName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
            return processName.Contains("iisexpress", StringComparison.OrdinalIgnoreCase) ||
                   !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IIS_EXPRESS"));
        }

        private static bool HasReverseProxyIndicators()
        {
            // 检查其他反向代理的指示器
            // 例如：特定的环境变量、文件存在性、注册表项等

            // 检查常见的反向代理环境变量
            var proxyVars = new[] { "NGINX_VERSION", "TRAEFIK_VERSION", "APACHE_PID_FILE" };
            return proxyVars.Any(var => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(var)));
        }
    }

   
}
