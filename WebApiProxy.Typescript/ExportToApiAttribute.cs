using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApiProxy.Typescript
{
    public class ExportToApiAttribute : Attribute
    {

        public string RouteToUse { get; set; }

    }
}
