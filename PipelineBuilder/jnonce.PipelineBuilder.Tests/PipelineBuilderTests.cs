using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

