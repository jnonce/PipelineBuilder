using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace jnonce.PipelineBuilder
{
    internal class ParallelPipelineBuilder<TInput, TOutput> : IPipelineBuilder<TInput, Task<TOutput>>
    {
        private readonly Func<TInput, Func<TInput, Task<TOutput>[]>, Task<TOutput>> choose;
        private List<Func<Func<TInput, Task<TOutput>>, Func<TInput, Task<TOutput>>>> handlers =
            new List<Func<Func<TInput, Task<TOutput>>, Func<TInput, Task<TOutput>>>>();

        public ParallelPipelineBuilder(Func<TInput, Func<TInput, Task<TOutput>[]>, Task<TOutput>> choose)
        {
            this.choose = choose;
        }

        public Func<Func<TInput2, TOutput2>, Func<TInput2, TOutput2>> New<TInput2, TOutput2>(Action<IPipelineBuilder<TInput2, TOutput2>> configuration)
        {
            var line = new PipelineBuilder<TInput2, TOutput2>();
            configuration(line);
            return line.BindInnerPipeline;
        }

        public void Use(Func<Func<TInput, Task<TOutput>>, Func<TInput, Task<TOutput>>> handler)
        {
            handlers.Add(handler);
        }

        public Func<TInput, Task<TOutput>> BindInnerPipeline(Func<TInput, Task<TOutput>> tail)
        {
            var handlerArray = handlers.Select(f => f(tail)).ToArray();
            return input => choose(
                input,
                input2 => handlerArray
                    .Select(f => f(input2))
                    .ToArray());
        }
    }

    internal class ParallelPipelineBuilder<TInput> : IPipelineBuilder<TInput, Task>
    {
        private readonly Func<TInput, Func<TInput, Task[]>, Task> choose;
        private List<Func<Func<TInput, Task>, Func<TInput, Task>>> handlers =
            new List<Func<Func<TInput, Task>, Func<TInput, Task>>>();

        public ParallelPipelineBuilder(Func<TInput, Func<TInput, Task[]>, Task> choose)
        {
            this.choose = choose;
        }

        public Func<Func<TInput2, TOutput2>, Func<TInput2, TOutput2>> New<TInput2, TOutput2>(Action<IPipelineBuilder<TInput2, TOutput2>> configuration)
        {
            var line = new PipelineBuilder<TInput2, TOutput2>();
            configuration(line);
            return line.BindInnerPipeline;
        }

        public void Use(Func<Func<TInput, Task>, Func<TInput, Task>> handler)
        {
            handlers.Add(handler);
        }

        public Func<TInput, Task> BindInnerPipeline(Func<TInput, Task> tail)
        {
            var handlerArray = handlers.Select(f => f(tail)).ToArray();
            return input => choose(
                input,
                input2 => handlerArray
                    .Select(f => f(input2))
                    .ToArray());
        }
    }
}
