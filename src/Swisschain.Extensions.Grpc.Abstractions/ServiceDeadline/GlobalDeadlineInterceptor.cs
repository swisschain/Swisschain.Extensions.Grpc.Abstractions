using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Swisschain.Extensions.Grpc.Abstractions.ServiceDeadline
{
    internal class GlobalDeadlineInterceptor : Interceptor
    {
        private readonly GlobalDeadlineInterceptorOptions _options;

        public GlobalDeadlineInterceptor(GlobalDeadlineInterceptorOptions options)
        {
            _options = options;
        }
        
        public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context, AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            var call = continuation(ConfigureContext(context, out var cts));
            if (cts == null)
            {
                return call;
            }
            else
            {
                return new AsyncClientStreamingCall<TRequest, TResponse>(
                    requestStream: call.RequestStream,
                    responseAsync: call.ResponseAsync,
                    responseHeadersAsync: ClientStreamingCallbacks<TRequest, TResponse>.GetResponseHeadersAsync,
                    getStatusFunc: ClientStreamingCallbacks<TRequest, TResponse>.GetStatus,
                    getTrailersFunc: ClientStreamingCallbacks<TRequest, TResponse>.GetTrailers,
                    disposeAction: ClientStreamingCallbacks<TRequest, TResponse>.Dispose,
                    CreateContextState(call, cts));
            }
        }

        public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context, AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            var call = continuation(ConfigureContext(context, out var cts));
            if (cts == null)
            {
                return call;
            }

            return new AsyncDuplexStreamingCall<TRequest, TResponse>(
                requestStream: call.RequestStream,
                responseStream: call.ResponseStream,
                responseHeadersAsync: DuplexStreamingCallbacks<TRequest, TResponse>.GetResponseHeadersAsync,
                getStatusFunc: DuplexStreamingCallbacks<TRequest, TResponse>.GetStatus,
                getTrailersFunc: DuplexStreamingCallbacks<TRequest, TResponse>.GetTrailers,
                disposeAction: DuplexStreamingCallbacks<TRequest, TResponse>.Dispose,
                CreateContextState(call, cts));
        }

        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            var call = continuation(request, ConfigureContext(context, out var cts));
            if (cts == null)
            {
                return call;
            }
            else
            {
                return new AsyncServerStreamingCall<TResponse>(
                    responseStream: call.ResponseStream,
                    responseHeadersAsync: ServerStreamingCallbacks<TResponse>.GetResponseHeadersAsync,
                    getStatusFunc: ServerStreamingCallbacks<TResponse>.GetStatus,
                    getTrailersFunc: ServerStreamingCallbacks<TResponse>.GetTrailers,
                    disposeAction: ServerStreamingCallbacks<TResponse>.Dispose,
                    CreateContextState(call, cts));
            }
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            var call = continuation(request, ConfigureContext(context, out var cts));
            if (cts == null)
            {
                return call;
            }
            else
            {
                return new AsyncUnaryCall<TResponse>(
                    responseAsync: call.ResponseAsync,
                    responseHeadersAsync: UnaryCallbacks<TResponse>.GetResponseHeadersAsync,
                    getStatusFunc: UnaryCallbacks<TResponse>.GetStatus,
                    getTrailersFunc: UnaryCallbacks<TResponse>.GetTrailers,
                    disposeAction: UnaryCallbacks<TResponse>.Dispose,
                    CreateContextState(call, cts));
            }
        }

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            var response = continuation(request, ConfigureContext(context, out var cts));
            cts?.Dispose();
            return response;
        }

        private ClientInterceptorContext<TRequest, TResponse> ConfigureContext<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context, out CancellationTokenSource linkedCts)
            where TRequest : class
            where TResponse : class
        {
            linkedCts = null;

            var options = context.Options;
            var globalDeadline = DateTime.UtcNow.Add(_options.Timeout);
            if (globalDeadline < context.Options.Deadline.GetValueOrDefault(DateTime.MaxValue))
            {
                options = options.WithDeadline(globalDeadline);
            }

            if (options.CancellationToken.CanBeCanceled)
            {
                var globalCancellation = new CancellationTokenSource(_options.Timeout).Token;
                linkedCts = CancellationTokenSource.CreateLinkedTokenSource(globalCancellation, options.CancellationToken);
                options = options.WithCancellationToken(linkedCts.Token);
            }

            return new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host, options);
        }

        private ContextState<TCall> CreateContextState<TCall>(TCall call, CancellationTokenSource cancellationTokenSource) where TCall : IDisposable =>
            new ContextState<TCall>(call, cancellationTokenSource);

        private class ContextState<TCall> : IDisposable where TCall : IDisposable
        {
            public ContextState(TCall call, CancellationTokenSource cancellationTokenSource)
            {
                Call = call;
                CancellationTokenSource = cancellationTokenSource;
            }

            public TCall Call { get; }
            public CancellationTokenSource CancellationTokenSource { get; }

            public void Dispose()
            {
                Call.Dispose();
                CancellationTokenSource.Dispose();
            }
        }

        // Store static callbacks so delegates are allocated once
        private static class UnaryCallbacks<TResponse>
            where TResponse : class
        {
            internal static readonly Func<object, Task<Metadata>> GetResponseHeadersAsync = state => ((ContextState<AsyncUnaryCall<TResponse>>)state).Call.ResponseHeadersAsync;
            internal static readonly Func<object, Status> GetStatus = state => ((ContextState<AsyncUnaryCall<TResponse>>)state).Call.GetStatus();
            internal static readonly Func<object, Metadata> GetTrailers = state => ((ContextState<AsyncUnaryCall<TResponse>>)state).Call.GetTrailers();
            internal static readonly Action<object> Dispose = state => ((ContextState<AsyncUnaryCall<TResponse>>)state).Dispose();
        }

        private static class ServerStreamingCallbacks<TResponse>
            where TResponse : class
        {
            internal static readonly Func<object, Task<Metadata>> GetResponseHeadersAsync = state => ((ContextState<AsyncServerStreamingCall<TResponse>>)state).Call.ResponseHeadersAsync;
            internal static readonly Func<object, Status> GetStatus = state => ((ContextState<AsyncServerStreamingCall<TResponse>>)state).Call.GetStatus();
            internal static readonly Func<object, Metadata> GetTrailers = state => ((ContextState<AsyncServerStreamingCall<TResponse>>)state).Call.GetTrailers();
            internal static readonly Action<object> Dispose = state => ((ContextState<AsyncServerStreamingCall<TResponse>>)state).Dispose();
        }

        private static class DuplexStreamingCallbacks<TRequest, TResponse>
            where TRequest : class
            where TResponse : class
        {
            internal static readonly Func<object, Task<Metadata>> GetResponseHeadersAsync = state => ((ContextState<AsyncDuplexStreamingCall<TRequest, TResponse>>)state).Call.ResponseHeadersAsync;
            internal static readonly Func<object, Status> GetStatus = state => ((ContextState<AsyncDuplexStreamingCall<TRequest, TResponse>>)state).Call.GetStatus();
            internal static readonly Func<object, Metadata> GetTrailers = state => ((ContextState<AsyncDuplexStreamingCall<TRequest, TResponse>>)state).Call.GetTrailers();
            internal static readonly Action<object> Dispose = state => ((ContextState<AsyncDuplexStreamingCall<TRequest, TResponse>>)state).Dispose();
        }

        private static class ClientStreamingCallbacks<TRequest, TResponse>
            where TRequest : class
            where TResponse : class
        {
            internal static readonly Func<object, Task<Metadata>> GetResponseHeadersAsync = state => ((ContextState<AsyncClientStreamingCall<TRequest, TResponse>>)state).Call.ResponseHeadersAsync;
            internal static readonly Func<object, Status> GetStatus = state => ((ContextState<AsyncClientStreamingCall<TRequest, TResponse>>)state).Call.GetStatus();
            internal static readonly Func<object, Metadata> GetTrailers = state => ((ContextState<AsyncClientStreamingCall<TRequest, TResponse>>)state).Call.GetTrailers();
            internal static readonly Action<object> Dispose = state => ((ContextState<AsyncClientStreamingCall<TRequest, TResponse>>)state).Dispose();
        }
    }
}