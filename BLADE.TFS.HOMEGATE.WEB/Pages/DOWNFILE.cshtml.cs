using BLADE.TOOLS.BASE;
using BLADE.TOOLS.WEB;
using BLADE.TOOLS.WEB.Razor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace BLADE.SERVICEWEB.RAZORBODY9.Pages
{
    public class DOWNFILEModel : BasePageModel
    {
        public string Message { get; private set; } = "Params Error !";
        public DOWNFILEModel(BaseService _bs): base(_bs)
        {
        }
        public async Task<IActionResult> OnGetAsync()
        {
            string ctt = "";
            GetClientIpAddress();
            try
            {
                string fileid = Request.GetValueFromRequest("fid").Trim() ;
                string filename = Request.GetValueFromRequest("ofn").Trim();  //如果包含扩展名，则oft可以为空，自动分割 
                string filetype = Request.GetValueFromRequest("oft").Trim();  //  如果指定扩展名，则会使用指定的扩展名处理 contextType 但不会影响 输出的文件名。
                string fileprm = Request.GetValueFromRequest("fpm").Trim();
                string dm = Request.GetValueFromRequest("dm").Trim().ToLower();
                bool download = false; 
                if ( dm.StartsWith("s")|| dm.StartsWith("d"))
                {   download = true;  }
                string extension = "";
                var f = StaticFunction.SplitFilename(filename);
                if (f.ftype.Length > 1 && filetype.Length<1) { 
                   filetype = "." + f.ftype;
                }
                //if ( filename.Length > 1 && filetype.Length<1)
                //{  
                //    int lastDotIndex = filename.LastIndexOf('.'); 
                //    if (lastDotIndex >= 0 && lastDotIndex < filename.Length - 1)
                //    {
                //        // 确保 '.' 不是最后一个字符，并且确实存在
                //        extension = filename.Substring(lastDotIndex + 1);
                //        filetype = "." + extension;
                //    } 
                //}
                var mt= WebTools.GetMimeType(f.ftype);
                var fcb = await MakeFileContext(fileid, f.pth, f.fname, f.ftype, fileprm, mt.ucode);
                if (fcb.filestream != null)
                { return File(fcb.filestream, mt.cntp, filename); }
                else
                {
                    if (fcb.Item1.Length < 1)
                    {
                        ctt =ctt + "FileNotFound: " + fileid + " / " + filename + " / " + filetype;
                    }
                    return File(fcb.Item1, mt.cntp, filename);
                }
            }catch(Exception ex)
            {
                 ctt= ctt +   "    -- Ex:" + ex.Message;
            }
            Message = "FileError: [ " + ctt  +" ] ";
            Response.StatusCode = 404;
            return Page();
        }
        /// <summary>
        /// 查找并读取文件内容。
        /// </summary>
        /// <param name="fileid">文件ID   可能空</param>
        /// <param name="filename">文件名称  可能空</param>
        /// <param name="filetype">文件类型  可能空</param>
        /// <param name="fileprm">其他参数  可能空</param>
        /// <returns></returns>
        public async Task<(byte[],Stream? filestream)> MakeFileContext(string fileid,string subpath, string filename, string filetype, string fileprm,bool ucode)
        {
            fileid = fileid.Trim().ToLower();
            filename = filename.Trim().ToLower();
            filetype = filetype.Trim().ToLower();
            fileprm = fileprm.Trim().ToLower();
            subpath= subpath.Trim();

            // 以下部分 根据需要处理   这里是实列代码
            if ( filetype.Length>0 && !filetype.StartsWith("."))
            { filetype = "."+ filetype; }
            string pth = BS.WwwRootPath;
            if ( subpath.Length>0)
            {
                pth = System.IO.Path.Combine(pth, subpath);
            }else
            {
                pth = System.IO.Path.Combine(pth, "FileUpDown");
            }
            pth = System.IO.Path.Combine(pth, filename + filetype);

            if (System.IO.File.Exists(pth))
            {
                System.IO.FileInfo fi = new FileInfo(pth);
                if (fi.Length < 0)
                { }
                else if (fi.Length < 1048576)
                {
                    return (await System.IO.File.ReadAllBytesAsync(pth), null);
                }
                else
                {
                    return (Array.Empty<byte>(), System.IO.File.OpenRead(pth));
                }
            }

          
            return (Array.Empty<byte>(),null);
        }
    }
}
