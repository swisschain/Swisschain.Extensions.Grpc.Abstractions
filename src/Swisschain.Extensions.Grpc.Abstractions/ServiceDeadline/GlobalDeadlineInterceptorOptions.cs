using System;

namespace Swisschain.Extensions.Grpc.Abstractions.ServiceDeadline
{
    public class GlobalDeadlineInterceptorOptions
    {
        public TimeSpan Timeout { get; set; }
    }
}