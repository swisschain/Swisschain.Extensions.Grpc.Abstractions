using System;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Core.Utils;

namespace Swisschain.Extensions.Grpc.Abstractions
{
    //copypasted from https://github.com/grpc/grpc/blob/master/src/csharp/Grpc.Core.Api/Interceptors/CallInvokerExtensions.cs#L97
    //because in original repo it's private
    //this class is useful to intercept request within ClientFactory (when using Grpc core with Service Collection)

    public class MetadataInterceptor : Interceptor
    {
        readonly Func<Metadata, Metadata> interceptor;

        /// <summary>
        /// Creates a new instance of MetadataInterceptor given the specified interceptor function.
        /// </summary>
        public MetadataInterceptor(Func<Metadata, Metadata> interceptor)
        {
            this.interceptor = GrpcPreconditions.CheckNotNull(interceptor, nameof(interceptor));
        }

        private ClientInterceptorContext<TRequest, TResponse> GetNewContext<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context)
            where TRequest : class
            where TResponse : class
        {
            var metadata = context.Options.Headers ?? new Metadata();
            return new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, context.Options.WithHeaders(interceptor(metadata)));
        }

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            return continuation(request, GetNewContext(context));
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            return continuation(request, GetNewContext(context));
        }

        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            return continuation(request, GetNewContext(context));
        }

        public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context, AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            return continuation(GetNewContext(context));
        }

        public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context, AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            return continuation(GetNewContext(context));
        }
    }
}