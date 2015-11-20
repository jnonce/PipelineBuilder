using System;
using System.Collections.Generic;
using System.Linq;

namespace jnonce.PipelineBuilder
{
    internal class ParallelPipelineBuilder<TInput, TOutput> : IPipelineBuilder<TInput, TOutput>
    {
        private readonly Func<TInput, Func<TInput, TOutput[]>, TOutput> choose;
        private List<Func<Func<TInput, TOutput>, Func<TInput, TOutput>>> handlers =
            new List<Func<Func<TInput, TOutput>, Func<TInput, TOutput>>>();

        public ParallelPipelineBuilder(Func<TInput, Func<TInput, TOutput[]>, TOutput> choose)
        {
            this.choose = choose;
        }

        public Func<Func<TInput2, TOutput2>, Func<TInput2, TOutput2>> New<TInput2, TOutput2>(Action<IPipelineBuilder<TInput2, TOutput2>> configuration)
        {
            var line = new PipelineBuilder<TInput2, TOutput2>();
            configuration(line);
            return line.BindInnerPipeline;
        }

        public void Use(Func<Func<TInput, TOutput>, Func<TInput, TOutput>> handler)
        {
            handlers.Add(handler);
        }

        public Func<TInput, TOutput> BindInnerPipeline(Func<TInput, TOutput> tail)
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
