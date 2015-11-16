using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace jnonce.PipelineBuilder.Http
{
    public static class Appender
    {
        /// <summary>
        /// Creates a file appender that writes to a file
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Func<Func<Stream, Task>, Task> File(string path)
        {
            var sync = new SemaphoreSlim(1);
            return func => InSemaphore(sync, async () =>
            {
                using (FileStream stream = System.IO.File.OpenWrite(path))
                {
                    await func(stream);
                }
            });
        }

        /// <summary>
        /// Creates a file appender that writes to a file
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Func<Func<Stream, Task>, Task> Stream(Stream stream)
        {
            var sync = new SemaphoreSlim(1);

            return func => InSemaphore(sync, () => func(stream));
        }

        private static async Task InSemaphore(SemaphoreSlim sync, Func<Task> func)
        {
            await sync.WaitAsync();
            try
            {
                await func();
            }
            finally
            {
                sync.Release();
            }
        }
    }
}
