using BLADE.TOOLS.WEB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;

namespace BLADE.SERVICEWEB.RAZORBODY9.Pages
{
   
    public class CaptChaModel : BLADE.TOOLS.WEB.Razor.BasePageModel
    {
        private readonly IMemoryCache _cache;
        public CaptChaModel(IMemoryCache cache,BaseService _bs):base(_bs)
        {
            _cache = cache;
        }
        public async Task<IActionResult> OnGetAsync()
        {
           
            GetClientIpAddress();
            var cacheKey = $"CaptchaAttempt_{uuip}";

            if (_cache.TryGetValue(cacheKey, out int attemptCount) && attemptCount > 7)
            {
                return Content("ÇëÇó¹ýÓÚÆµ·±");
            }
            _cache.Set(cacheKey, attemptCount + 1, TimeSpan.FromMinutes(1));
            string filename = Request.GetValueFromRequest("ofn").Trim();
            var sessionId = HttpContext.Session.Id;
            Console.WriteLine($"CaptCha Session ID: {HttpContext.Session.Id}");
            if (filename.Length < 1)
            {
                filename = "TmpCapcha"+sessionId+".png";
            }
            var cap  = await BLADE.TOOLS.WEB.WebTools.MakeCaptchaPIC_CrossPlatform(60);
           
            _cache.Set<string>($"Captcha_{sessionId}", cap.code, TimeSpan.FromMinutes(5));
            return File(cap.pic, "image/png",filename);
        }
    }
}
