using BLADE.TOOLS.WEB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.IO;
using System.Text;

namespace BLADE.SERVICEWEB.RAZORBODY9.Pages
{
    public class LOGINModel : BLADE.TOOLS.WEB.Razor.BasePageModel
    {
        private readonly IMemoryCache _cache;
        public LOGINModel(IMemoryCache cache, BaseService _bs) : base(_bs)
        {
            _cache = cache;
        }
        [BindProperty]
        public string LoginInfo { get; set; } = "";

        [BindProperty]
        public string CaptchaCode { get; set; } = "";

        public string rndstr { get { var p = BLADE.TimeProvider.LocalNow; return p.Millisecond.ToString() + p.Second.ToString() + p.DayOfYear.ToString(); } } 
        public void OnGet()
        {
            HttpContext.Session.SetString("NewInit", "true");
            Console.WriteLine($"Login Session ID: {HttpContext.Session.Id}");
        }

        public async Task<IActionResult> OnPostAsync(string username, string password, string captcha)
        {
            GetClientIpAddress();
            var sessionId = HttpContext.Session.Id;
            string? aa = "";

            aa = _cache.Get<string>($"Captcha_{sessionId}");
            CaptchaCode = (aa!=null) ? aa : "-"+sessionId;
             
            // 验证码校验
            if (captcha.Trim().ToLower() != CaptchaCode.Trim().ToLower())
            {
                LoginInfo = "验证码错误 |" + CaptchaCode+"|"+captcha;
                return Page();
            }

            // 用户名密码验证逻辑  这里只是一个实例，实际验证请替换真实逻辑
            if (username.Length>3 && username.Length<16 && password == "888999")
            {
                TempData["inputaccount"] = username;
                TempData["inputpassword"] = password;
                TempData["SessionId"] = sessionId;
                var ur = MakeNewUserSession();
                ur.UserName = username;
                ur.SetCryptPass ( password);
                ur.TokenKey = Convert.ToHexString(Encoding.UTF8.GetBytes(username + password));
                
                ur.LastAddress = uuip;
                ur.LastLogin_UTC = BLADE.TimeProvider.UtcNowOffSet;


                await ur.MakeTokenInToSelf();
                // 将用户信息存储在Session中
                HttpContext.Session.SetString("USERDATA",BLADE.TOOLS.BASE.Json.JsonOptions.Serialize(ur));
                await BS.AddLogAsync(160, "Login OK: " + username + " | " + password);
                // 应该跳转到正常的工作页面地址    这里跳转到一个测试页面，展示运行时的一些信息。
                return RTPage("/DevTest", new { ti = BLADE.TimeProvider.LocalNow.Ticks});
            }

            LoginInfo = "用户名或密码错误";
            return Page();
        }
    }
}
