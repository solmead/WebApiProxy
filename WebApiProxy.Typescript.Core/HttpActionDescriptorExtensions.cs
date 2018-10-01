using System.Linq;

namespace WebApiProxy.Typescript.Core
{
    public static class HttpActionDescriptorExtensions
    {
        public static bool IsExcluded(this HttpActionDescriptor descriptor)
        {
            return descriptor.GetCustomAttributes<ExcludeProxy>(true).Any();
        }
    }
}