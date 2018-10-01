using System.Collections.Generic;

namespace WebApiProxy.TypeScript.Core.Models
{
    public class Metadata
    {
        public string Host { get; set; }

        public IEnumerable<ControllerDefinition> Definitions { get; set; }

        public IEnumerable<ModelDefinition> Models { get; set; }

    }
}
