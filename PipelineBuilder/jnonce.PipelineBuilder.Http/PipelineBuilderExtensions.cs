using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace jnonce.PipelineBuilder.Http
{
    public static class PipelineBuilderExtensions
    {
        public static void Run<TRequestContext>(
            this IPipelineBuilder<TRequestContext, Task<HttpResponseMessage>> pipe,
            HttpMessageInvoker client)
            where TRequestContext : RequestContext
        {
            pipe.Use(next => input => client.SendAsync(input.RequestMessage, input.CancellationToken));
        }

        public static void Use(
            this IPipelineBuilder<RequestContext, Task<HttpResponseMessage>> pipe,
            params DelegatingHandler[] handlers)
        {
            pipe.Use((Func<RequestContext, Task<HttpResponseMessage>> next) =>
            {
                HttpClient client = HttpClientFactory.Create(
                    new ChainingHttpMessageHandler(next),
                    handlers);

                return x => client.SendAsync(x.RequestMessage, x.CancellationToken);
            });
        }

        public static void Retry(
            this IPipelineBuilder<RequestContext, Task<HttpResponseMessage>> pipe,
            Func<RequestContext, HttpResponseMessage, int, Task<bool>> isAcceptable,
            Func<RequestContext, Exception, int, Task<Exception>> shouldRethrow
            )
        {
            pipe.Use(next => async context =>
            {
                int attempt = 1;
                while (true)
                {
                    ++attempt;

                    Task<Exception> throwMe;
                    try
                    {
                        HttpResponseMessage response = await next(context);
                        if (await isAcceptable(context, response, attempt))
                        {
                            return response;
                        }

                        throwMe = Task.FromResult<Exception>(null);
                    }
                    catch (Exception ex)
                    {
                        throwMe = shouldRethrow(context, ex, attempt);
                        if (throwMe.IsCompleted && !throwMe.IsFaulted && ReferenceEquals(ex, throwMe.Result))
                        {
                            throw;
                        }
                    }

                    Exception errorToThrow = await throwMe;
                    if (errorToThrow != null)
                    {
                        throw errorToThrow;
                    }
                }
            });
        }

        public static void Run(
            this IPipelineBuilder<RequestContext, Task<HttpResponseMessage>> pipe,
            HttpMessageHandler innerHandler,
            params DelegatingHandler[] handlers)
        {
            pipe.Use((Func<RequestContext, Task<HttpResponseMessage>> next) =>
            {
                HttpClient client = HttpClientFactory.Create(innerHandler, handlers);

                return x => client.SendAsync(x.RequestMessage, x.CancellationToken);
            });
        }

        private class ChainingHttpMessageHandler : HttpMessageHandler
        {
            private Func<RequestContext, Task<HttpResponseMessage>> handler;

            public ChainingHttpMessageHandler(Func<RequestContext, Task<HttpResponseMessage>> handler)
            {
                this.handler = handler;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return handler(new RequestContext(request, cancellationToken));
            }
        }
    }
}
