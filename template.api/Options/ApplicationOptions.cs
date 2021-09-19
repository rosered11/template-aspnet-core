using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace template.api
{
    public class ApplicationOptions
    {
        public CacheProfileOptions CacheProfiles { get; }
        public ForwardedHeadersOptions ForwardedHeaders { get; set; }
        public KestrelServerOptions Kestrel { get; set; }
    }
}