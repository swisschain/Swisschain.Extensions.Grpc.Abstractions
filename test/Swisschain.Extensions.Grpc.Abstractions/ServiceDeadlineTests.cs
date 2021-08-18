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
        public async Task CanReadResponseWhenDeadlineNotExceeded()
        {
            var greeterClient = GetClient(TimeSpan.FromSeconds(ResponseDelaySeconds + 1));
            
            var respSync = greeterClient.SayHello(new()
            {
                Name = "WTF"
            });
            
            respSync.Message.ShouldBe("Hello WTF");
            
            var respAsync =await greeterClient.SayHelloAsync(new()
            {
                Name = "WTF"
            });
            
            respAsync.Message.ShouldBe("Hello WTF");
        }

        [Fact]
        public async Task ShouldFailIfDeadlineExceededOnServiceCallLevel()
        {
            var greeterClient = GetClient(TimeSpan.FromSeconds(ResponseDelaySeconds + 1));
            
            Should.Throw<RpcException>(() => greeterClient.SayHello(new()
            {
                Name = "WTF"
            }, deadline: DateTime.UtcNow.AddSeconds(ResponseDelaySeconds -1)));

            await Should.ThrowAsync<RpcException>(async () => await greeterClient.SayHelloAsync(new()
            {
                Name = "WTF"
            }, deadline: DateTime.UtcNow.AddSeconds(ResponseDelaySeconds -1)));
            
        }
        
        [Fact]
        public async Task ShouldStopExecutionWhenDeadlineExceeded()
        {
            var greeterClient = GetClient(TimeSpan.FromSeconds(ResponseDelaySeconds - 1));
            
            Should.Throw<RpcException>(() => greeterClient.SayHello(new()
            {
                Name = "WTF"
            }));

            await Should.ThrowAsync<RpcException>(async () => await greeterClient.SayHelloAsync(new()
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