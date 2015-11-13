using System;
using System.Collections.Generic;

namespace jnonce.PipelineBuilder
{
    public class PipelineBuilder<TInput, TOutput> : IPipelineBuilder<TInput, TOutput>
    {
        private readonly List<Func<Func<TInput, TOutput>, Func<TInput, TOutput>>> handlers
            = new List<Func<Func<TInput, TOutput>, Func<TInput, TOutput>>>();

        public Func<Func<TInput2, TOutput2>, Func<TInput2, TOutput2>> New<TInput2, TOutput2>(Action<IPipelineBuilder<TInput2, TOutput2>> configuration)
        {
            var result = new PipelineBuilder<TInput2, TOutput2>();
            configuration(result);
            return result.BindInnerPipeline;
        }

        public void Use(Func<Func<TInput, TOutput>, Func<TInput, TOutput>> handler)
        {
            handlers.Add(handler);
        }

        public Func<TInput, TOutput> BindInnerPipeline(Func<TInput, TOutput> tail)
        {
            Func<TInput, TOutput> next = tail;

            for (int i = handlers.Count - 1; i >= 0; i--)
            {
                next = handlers[i](next);
            }

            return next;
        }
    }

    public static class PipelineBuilder
    {
        public static Func<TInput, TOutput> Create<TInput, TOutput>(
            Func<TInput, TOutput> tailMethod,
            Action<IPipelineBuilder<TInput, TOutput>> action)
        {
            var result = new PipelineBuilder<TInput, TOutput>();
            action(result);
            return result.BindInnerPipeline(tailMethod);
        }
    }
}
