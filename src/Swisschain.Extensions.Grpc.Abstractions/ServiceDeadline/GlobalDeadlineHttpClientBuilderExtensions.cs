using Grpc.Net.ClientFactory;
using Microsoft.Extensions.Options;
using Swisschain.Extensions.Grpc.Abstractions.ServiceDeadline;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class GlobalDeadlineHttpClientBuilderExtensions
    {
        public static IHttpClientBuilder WithGrpcGlobalDeadline(this IHttpClientBuilder builder, GlobalDeadlineInterceptorOptions globalDeadlineOptions)
        {
            builder.Services.AddTransient<IConfigureOptions<GrpcClientFactoryOptions>>(services =>
            {
                return new ConfigureNamedOptions<GrpcClientFactoryOptions>(builder.Name, options =>
                {
                    options.Interceptors.Add(new GlobalDeadlineInterceptor(globalDeadlineOptions));
                });
            });

            return builder;
        }
    }
}