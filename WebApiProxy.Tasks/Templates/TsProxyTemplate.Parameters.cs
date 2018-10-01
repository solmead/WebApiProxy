using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebApiProxy.Core.Models;
using WebApiProxy.Tasks.Models;

namespace WebApiProxy.Tasks.Templates
{
    /// <summary>
    /// A partial class implementation used to pass parameters to the proxy template.
    /// </summary>
    public partial class TsProxyTemplate
    {
        public TsProxyTemplate(Configuration config)
        {
            this.Configuration = config;
            this.Metadata = config.Metadata;
        }
        public Configuration Configuration { get; set; }
        public Metadata Metadata { get; set; }


        public string HandleType(string type)
        {
            var fType = "I" + type;

            switch (type)
            {
                case "Int16":
                case "Int32":
                case "Int64":
                case "UInt16":
                case "UInt32":
                case "UInt64":
                case "Single":
                case "Double":
                case "Decimal":
                case "Byte":
                case "SByte":
                    fType = "number";
                    break;

                case "String":
                case "Char":
                    fType = "string";
                    break;

                case "Boolean":
                    fType = "boolean";
                    break;

            }

            return fType;




        }
    }
}