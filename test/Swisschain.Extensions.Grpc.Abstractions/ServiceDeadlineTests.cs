using System;
using System.Threading.Tasks;
using Greet;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Xunit;

namespace Swisschain.Extensions.Grpc.Abstractions.Test
{
    public class ServiceDeadlineTests
    {
        private static readonly int ResponseDelaySeconds = 3;
        
        [Fact]
        public void CanReadResponseWhenDeadlineNotExceeded()
        {
            var greeterClient = GetClient(TimeSpan.FromSeconds(ResponseDelaySeconds + 1));
            
            var resp = greeterClient.SayHello(new()
            {
                Name = "WTF"
            });
            
            resp.Message.ShouldBe("Hello WTF");
        }
        
        [Fact]
        public void StopExecutionWhenDeadlineExceeded()
        {
            var greeterClient = GetClient(TimeSpan.FromSeconds(ResponseDelaySeconds - 1));
            
            Should.Throw<RpcException>(() => greeterClient.SayHello(new()
            {
                Name = "WTF"
            }));
        }

        private static Greeter.GreeterClient GetClient(TimeSpan timeout)
        {
            var builder = new HostBuilder()
                .ConfigureWebHostDefaults(webhost =>
                {
                    webhost.UseTestServer()
                        .UseStartup<Startup>();
                });
            
            var host = builder.Start();
            
            var sc = new ServiceCollection();
            
            sc
                .AddGrpcClient<Greeter.GreeterClient>(o=> o.Address= new Uri("http://localhost"))
                .ConfigurePrimaryHttpMessageHandler(()=> host.GetTestServer().CreateHandler())
                .WithGrpcGlobalDeadline(new ()
                {
                    Timeout = timeout
                });

            return sc.BuildServiceProvider().GetRequiredService<Greeter.GreeterClient>();

        }

        private class Startup
        {
            public void ConfigureServices(IServiceCollection serviceCollection)
            {
                serviceCollection.AddGrpc();
                serviceCollection.AddSingleton<GreeterService>();
            }
            
            public void Configure(IApplicationBuilder app)
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGrpcService<GreeterService>();
                });
            }
        }
        
        private class GreeterService : Greeter.GreeterBase
        {
            public override async Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
            {
                await Task.Delay(TimeSpan.FromSeconds(ResponseDelaySeconds));
                return new()
                {
                    Message = "Hello " + request.Name
                };
            }
        }
    }
}