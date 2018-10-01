using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using WebApiProxy.Typescript.WebApi;

//using WebApiProxy.TypeScript.WebApi;


[assembly: WebActivatorEx.PostApplicationStartMethod(typeof(WebApiProxy.TypeScript.Startup.Generate), "CallMeAfterAppStart")]
namespace WebApiProxy.TypeScript.Startup
{
    public static class Generate
    {
        public static void CallMeAfterAppStart()
        {
            try
            {

                var context = HttpContext.Current;
                var server = context.Server;
                var fi = new FileInfo(server.MapPath("/Scripts/WebApi/Api.ts"));
                if (!fi.Directory.Exists)
                {
                    fi.Directory.Create();
                }
                if (fi.Exists)
                {
                    fi.Delete();
                }

                var proxyTemplate = new TsProxyTemplate();

                using (var fs = new StreamWriter(fi.OpenWrite()))
                {
                    fs.Write(proxyTemplate.TransformText());
                    fs.Close();
                }

            }
            catch (Exception ex)
            {
                
            }


        }

    }
}
