using System.Net.Http;
using System.Threading;

namespace jnonce.PipelineBuilder.Http
{
    public class RequestContext
    {
        public RequestContext(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.RequestMessage = request;
            this.CancellationToken = cancellationToken;
        }

        public RequestContext(HttpRequestMessage request)
        {
            this.RequestMessage = request;
            this.CancellationToken = CancellationToken.None;
        }

        public HttpRequestMessage RequestMessage { get; private set; }

        public CancellationToken CancellationToken { get; private set; }
    }
}
