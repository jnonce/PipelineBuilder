using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using jnonce.PipelineBuilder.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace jnonce.PipelineBuilder.Tests
{
    [TestClass]
    public class PipelineBuilderTests
    {

        [TestMethod]
        public void Process()
        {
            var handled = new bool[2];
            PipelineBuilder<LogMessage, Task> logger = new PipelineBuilder<LogMessage, Task>();
            logger.Process(msg =>
            {
                handled[0] = true;
            });
            logger.Process(msg =>
            {
                handled[1] = true;
            });

            Func<LogMessage, Task> logMethod = logger.BindInnerPipeline(_ => Task.FromResult(0));

            logMethod(new LogMessage { CorrelationVector = "e" });
            Assert.IsTrue(handled[0]);
            Assert.IsTrue(handled[1]);
        }

        [TestMethod]
        public void Use()
        {
            Func<double, int> pipeline = PipelineBuilder.Create(
                (double input) => (int)Math.Floor(input),
                builder =>
                {
                    builder.Use((i, n) => n(i + Math.PI));
                });

            int result = pipeline(30.0);
            Assert.AreEqual(33, result);
        }

        [TestMethod]
        public void TypeCheck()
        {
            Func<object, int> xxx = PipelineBuilder.Create(
                (object input) => 0,
                builder =>
                {
                    builder.When(
                        (string _) => true,
                        builderForStrings =>
                        {
                            builderForStrings.Process(Console.WriteLine);

                            builderForStrings.Run(text => text.Length);
                        });
                });

            int result1 = xxx("hi");
            int result2 = xxx(44);
            Assert.AreEqual(2, result1);
            Assert.AreEqual(0, result2);
        }

        [TestMethod]
        public void ProcessResult()
        {
            var list = new Stack<object>();
            int result = 5;

            Func<object, int> xxx = PipelineBuilder.Create(
                (object input) => result,
                builder =>
                {
                    builder.ProcessResult((input, output) =>
                    {
                        Assert.AreEqual(1, list.Count);
                        var top = list.Pop();
                        Assert.AreSame(top, input);
                    });
                    builder.Process(input => list.Push(input));
                });

            int result1 = xxx("hi");
            Assert.AreEqual(result, result1);
        }

        [TestMethod]
        public async Task Append()
        {
            var stream = new MemoryStream();
            var pipe = PipelineBuilder.Create(
                (LogMessage message) => Task.FromResult(message),
                builder =>
                {
                    builder.WriteTo(stream, new JsonMediaTypeFormatter());
                });

            await pipe(new ErrorMessage { CorrelationVector = "e", Code = 44 });

            stream.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(stream);
            string text = await reader.ReadToEndAsync();
        }

        [TestMethod]
        public async Task InParallel()
        {
            var s1 = new SemaphoreSlim(0);
            var s2 = new SemaphoreSlim(0);
            var processingComplete = new SemaphoreSlim(0);

            var pipe = PipelineBuilder.Create(
                (LogMessage message) => Task.FromResult(message),
                builder =>
                {
                    builder.InParallel(
                        (input, parallelOps) => parallelOps(input)[0],
                        forks =>
                        {
                            forks.Process(async _ =>
                            {
                                s2.Release();
                                await s1.WaitAsync();
                            });

                            forks.Process(async _ =>
                            {
                                s1.Release();
                                await s2.WaitAsync();
                            });
                        });

                    builder.Process(message =>
                    {
                        processingComplete.Release();
                    });
                });

            await pipe(new LogMessage());
            await processingComplete.WaitAsync();
            await processingComplete.WaitAsync();
        }
    }

    public class LogMessage
    {
        public string CorrelationVector { get; set; }

        public int MyProperty { get; set; }
    }

    public class ErrorMessage : LogMessage
    {
        public int Code { get; set; }
    }
}

