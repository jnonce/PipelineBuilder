using System;

namespace jnonce.PipelineBuilder
{
    public interface IPipelineBuilder<TInput, TOutput>
    {
        /// <summary>
        /// Create a new, separate pipeline.  The resultant method can then be invoked directly or added as a unit into another pipeline.
        /// 
        /// This is useful to create distinct, branching paths through pipelines.
        /// </summary>
        /// <typeparam name="TInput2"></typeparam>
        /// <typeparam name="TOutput2"></typeparam>
        /// <param name="configuration"></param>
        /// <returns></returns>
        Func<Func<TInput2, TOutput2>, Func<TInput2, TOutput2>> New<TInput2, TOutput2>(Action<IPipelineBuilder<TInput2, TOutput2>> configuration);

        /// <summary>
        /// Add a handler method to the pipeline
        /// </summary>
        /// <param name="handler">Method which handles the request and may (or may not) call the next handler</param>
        void Use(Func<Func<TInput, TOutput>, Func<TInput, TOutput>> handler);
    }

}
