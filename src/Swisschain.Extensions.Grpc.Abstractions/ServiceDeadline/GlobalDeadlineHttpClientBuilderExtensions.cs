using Grpc.Net.ClientFactory;
using Microsoft.Extensions.Options;
using Swisschain.Extensions.Grpc.Abstractions.ServiceDeadline;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class GlobalDeadlineHttpClientBuilderExtensions
    {
        /// <summary>
        /// Adds deadline (timeout) for all calls for registered GRPC service. This actions is similar to providing "deadline" parameter for all service calls
        /// For more information about GRPC deadlines read https://docs.microsoft.com/en-Us/aspnet/core/grpc/deadlines-cancellation?view=aspnetcore-5.0#deadlines
        /// </summary>
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