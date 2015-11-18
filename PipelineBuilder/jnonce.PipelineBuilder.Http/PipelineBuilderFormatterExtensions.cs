using System;
using System.IO;
using System.Net.Http.Formatting;
using System.Threading.Tasks;

namespace jnonce.PipelineBuilder.Http
{
    public static class PipelineBuilderFormatterExtensions
    {
        public static void WriteTo<TInput, TOutput>(
            this IPipelineBuilder<TInput, Task<TOutput>> pipeline,
            Func<Func<Stream, Task>, Task> func,
            MediaTypeFormatter formatter)
        {
            Type type = typeof(TInput);
            if (!formatter.CanWriteType(type))
            {
                throw new InvalidOperationException();
            }

            pipeline.ProcessAsync(
                input => func(
                    stream => formatter.WriteToStreamAsync(type, input, stream, null, null)
                    )
                );
        }

        public static void WriteTo<TInput, TOutput>(
            this IPipelineBuilder<TInput, Task<TOutput>> pipeline,
            Stream stream,
            MediaTypeFormatter formatter)
        {
            Type type = typeof(TInput);
            if (!formatter.CanWriteType(type))
            {
                throw new InvalidOperationException();
            }

            pipeline.ProcessAsync(
                input => formatter.WriteToStreamAsync(type, input, stream, null, null)
                );
        }
    }
}
