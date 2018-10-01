using System;

namespace WebApiProxy.Typescript.Core
{
    public class ExportToApiAttribute : Attribute
    {

        public string RouteToUse { get; set; }

    }
}
