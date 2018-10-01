using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WebApiProxy.Core.Models;
using WebApiProxy.Tasks.Models;
using WebApiProxy.Tasks.Templates;

namespace WebApiProxy.Tasks.Infrastructure
{
    public class TypeScriptGenerator
    {
        private readonly Configuration config;
        public TypeScriptGenerator(Configuration config)
        {
            this.config = config;
        }

        public string Generate()
        {
            config.Metadata = GetProxy();
            var template = new TsProxyTemplate(config);
            var source = template.TransformText();
            return source;
        }


        private Metadata GetProxy()
        {
            var url = string.Empty;

            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-Proxy-Type", "metadata");
                    var response = client.GetAsync(config.Endpoint).Result;
                    response.EnsureSuccessStatusCode();
                    var metadata = response.Content.ReadAsAsync<Metadata>().Result;
                    return metadata;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


    }
}
