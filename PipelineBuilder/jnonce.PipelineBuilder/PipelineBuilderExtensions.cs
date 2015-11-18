using System;
using System.Linq;
using System.Threading.Tasks;

namespace jnonce.PipelineBuilder
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class PipelineBuilderExtensions
    {
        #region Use

        /// <summary>
        /// Appends a handler onto the pipeline
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="builder"></param>
        /// <param name="handler">A handler which receives the request and may handle it and may call a given next Func</param>
        public static void Use<TInput, TOutput>(
            this IPipelineBuilder<TInput, TOutput> builder,
            Func<TInput, Func<TInput, TOutput>, TOutput> handler)
        {
            builder.Use(next => input => handler(input, next));
        }

        #endregion

        #region UseNested

        public static void UseNested<TInput, TOutput, TInput2, TOutput2>(
            this IPipelineBuilder<TInput, TOutput> builder,
            Func<TInput, TInput2> convertToNestedInput,
            Func<TInput2, TInput> convertToNextInput,
            Func<TOutput, TOutput2> convertFromNextOutput,
            Func<TOutput2, TOutput> convertFromNestedResponse,
            Action<IPipelineBuilder<TInput2, TOutput2>> handler)
        {
            builder.UseNested(
                builder.New(handler),
                convertToNestedInput,
                convertToNextInput,
                convertFromNextOutput, 
                convertFromNestedResponse);
        }

        public static void UseNested<TInput, TOutput, TInput2, TOutput2>(
            this IPipelineBuilder<TInput, TOutput> builder,
            Func<Func<TInput2, TOutput2>, Func<TInput2, TOutput2>> nestedHandler,
            Func<TInput, TInput2> convertToNestedInput,
            Func<TInput2, TInput> convertToNextInput,
            Func<TOutput, TOutput2> convertFromNextOutput,
            Func<TOutput2, TOutput> convertFromNestedResponse)
        {
            builder.Use(next =>
            {
                var nestedPipeline = nestedHandler(passedOutInput =>
                {
                    var convertedInput = convertToNextInput(passedOutInput);
                    var nextHandlerOutput = next(convertedInput);
                    var outputForNestedHandler = convertFromNextOutput(nextHandlerOutput);
                    return outputForNestedHandler;
                });

                return input =>
                {
                    var convertedInput = convertToNestedInput(input);
                    var nestedPipelineOutput = nestedPipeline(convertedInput);
                    var outputForHandler = convertFromNestedResponse(nestedPipelineOutput);
                    return outputForHandler;
                };
            });
        }

        public static void UseNestedAsync<TInput, TOutput, TInput2, TOutput2>(
            this IPipelineBuilder<TInput, Task<TOutput>> builder,
            Func<Func<TInput2, Task<TOutput2>>, Func<TInput2, Task<TOutput2>>> nestedHandler,
            Func<TInput, Task<TInput2>> convertToNestedInput,
            Func<TInput2, Task<TInput>> convertToNextInput,
            Func<TOutput, Task<TOutput2>> convertFromNextOutput,
            Func<TOutput2, Task<TOutput>> convertFromNestedResponse)
        {
            builder.Use(next =>
            {
                var nestedPipeline = nestedHandler(async passedOutInput =>
                {
                    var convertedInput = await convertToNextInput(passedOutInput);
                    var nextHandlerOutput = await next(convertedInput);
                    var outputForNestedHandler = await convertFromNextOutput(nextHandlerOutput);
                    return outputForNestedHandler;
                });

                return async input =>
                {
                    var convertedInput = await convertToNestedInput(input);
                    var nestedPipelineOutput = await nestedPipeline(convertedInput);
                    var outputForHandler = await convertFromNestedResponse(nestedPipelineOutput);
                    return outputForHandler;
                };
            });
        }

        public static void UseNestedAsync<TInput, TOutput, TInput2, TOutput2>(
            this IPipelineBuilder<TInput, Task<TOutput>> builder,
            Func<TInput, Task<TInput2>> convertToNestedInput,
            Func<TInput2, Task<TInput>> convertToNextInput,
            Func<TOutput, Task<TOutput2>> convertFromNextOutput,
            Func<TOutput2, Task<TOutput>> convertFromNestedResponse,
            Action<IPipelineBuilder<TInput2, Task<TOutput2>>> handler)
        {
            builder.UseNestedAsync(
                builder.New(handler),
                convertToNestedInput,
                convertToNextInput,
                convertFromNextOutput,
                convertFromNestedResponse);
        }

        #endregion

        #region Run

        /// <summary>
        /// Add a handler into the pipeline which performs final processing for the request.
        /// It is not given a reference to a another Func to call.
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="builder"></param>
        /// <param name="handler">Method which handles all requests</param>
        public static void Run<TInput, TOutput>(this IPipelineBuilder<TInput, TOutput> builder, Func<TInput, TOutput> handler)
        {
            builder.Use(next => handler);
        }

        /// <summary>
        /// Add a handler into the pipeline which returns a single, constant value for all requests.
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="builder"></param>
        /// <param name="value">Value to return for all requests</param>
        public static void Run<TInput, TOutput>(this IPipelineBuilder<TInput, TOutput> builder, TOutput value)
        {
            builder.Run(input => value);
        }

        // Translate input, call inner pipeline
        public static void Run<TInput, TOutput, TInput2, TOutput2>(
            this IPipelineBuilder<TInput, TOutput> builder,
            Func<Func<TInput2, TOutput2>, Func<TInput, TOutput>> convert,
            Action<IPipelineBuilder<TInput2, TOutput2>> nestedPipe)
        {
            var nestedPipeline = builder.New(nestedPipe);

            builder.Use(next =>
            {
                Func<TInput2, TOutput2> innerLine = nestedPipeline(_ => default(TOutput2));

                return convert(innerLine);
            });
        }

        #endregion

        #region Process

        /// <summary>
        /// Process the given request.  The next method is automatically called afterward.
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="builder"></param>
        /// <param name="handler">Handler which receives the request.</param>
        public static void Process<TInput, TOutput>(this IPipelineBuilder<TInput, TOutput> builder, Action<TInput> handler)
        {
            builder.Use(next => input =>
            {
                handler(input);
                return next(input);
            });
        }

        /// <summary>
        /// Process the given request.  The next method is automatically called afterward.
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="builder"></param>
        /// <param name="handler">Handler which receives the request.</param>
        public static void ProcessAsync<TInput, TOutput>(this IPipelineBuilder<TInput, Task<TOutput>> builder, Func<TInput, Task> handler)
        {
            builder.Use(next => async input =>
            {
                await handler(input);
                return await next(input);
            });
        }

        /// <summary>
        /// Process the given request.  The next method is automatically called afterward.
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="builder"></param>
        /// <param name="handler">Handler which receives the request.</param>
        public static void ProcessAsync<TInput>(this IPipelineBuilder<TInput, Task> builder, Func<TInput, Task> handler)
        {
            builder.Use(next => async input =>
            {
                await handler(input);
                await next(input);
            });
        }

        #endregion

        #region ProcessResult

        /// <summary>
        /// Invoke the pipeline and then call the given method to process the result
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="builder"></param>
        /// <param name="handler">Handler which receives the request.</param>
        public static void ProcessResult<TInput, TOutput>(
            this IPipelineBuilder<TInput, TOutput> builder,
            Action<TInput, TOutput> handler)
        {
            builder.ProcessResult(handler, (_, __) => { });
        }

        /// <summary>
        /// Invoke the pipeline and then call the given method to process the result
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="builder"></param>
        /// <param name="handler">Handler which receives the request.</param>
        /// <param name="errorHandler">Handler which receives the request on error.</param>
        public static void ProcessResult<TInput, TOutput>(
            this IPipelineBuilder<TInput, TOutput> builder,
            Action<TInput, TOutput> handler,
            Action<TInput, Exception> errorHandler)
        {
            builder.Use(next => input =>
            {
                try
                {
                    TOutput output = next(input);
                    handler(input, output);
                    return output;
                }
                catch (Exception ex)
                {
                    errorHandler(input, ex);
                    throw;
                }
            });
        }

        /// <summary>
        /// Invoke the pipeline and then call the given method to process the result
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="builder"></param>
        /// <param name="handler">Handler which receives the request.</param>
        /// <param name="errorHandler">Handler which receives the request on error.</param>
        public static void ProcessResultAsync<TInput, TOutput>(
            this IPipelineBuilder<TInput, Task<TOutput>> builder,
            Func<TInput, TOutput, Task> handler,
            Func<TInput, Exception, Task> errorHandler)
        {
            builder.Use(next => async input =>
            {
                try
                {
                    TOutput output = await next(input);
                    await handler(input, output);
                    return output;
                }
                catch (Exception ex)
                {
                    await errorHandler(input, ex);
                    throw;
                }
            });
        }

        /// <summary>
        /// Invoke the pipeline and then call the given method to process the result
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="builder"></param>
        /// <param name="handler">Handler which receives the request.</param>
        /// <param name="errorHandler">Handler which receives the request on error.</param>
        public static void ProcessResultAsync<TInput, TOutput>(
            this IPipelineBuilder<TInput, Task<TOutput>> builder,
            Func<TInput, TOutput, Task<TOutput>> handler,
            Func<TInput, Exception, Task> errorHandler)
        {
            builder.Use(next => async input =>
            {
                try
                {
                    TOutput output = await next(input);
                    return await handler(input, output);
                }
                catch (Exception ex)
                {
                    await errorHandler(input, ex);
                    throw;
                }
            });
        }

        #endregion

        #region When

        /// <summary>
        /// Branches the request pipeline based on the result of the given predicate.
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="builder"></param>
        /// <param name="predicate">Invoked with the request environment to determine if the branch should be taken</param>
        /// <param name="configuration">Configures a branch to take</param>
        public static void When<TInput, TOutput>(
            this IPipelineBuilder<TInput, TOutput> builder,
            Func<TInput, bool> predicate,
            Action<IPipelineBuilder<TInput, TOutput>> configuration)
        {
            var innerPipeline = builder.New(configuration);

            builder.Use(next =>
            {
                Func<TInput, TOutput> tail = innerPipeline(next);

                return input =>
                {
                    Func<TInput, TOutput> f = predicate(input)
                        ? tail
                        : next;
                    return f(input);
                };
            });
        }

        /// <summary>
        /// Branches the request pipeline when the input if of a given type.
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="builder"></param>
        /// <param name="predicate">Invoked with the request environment to determine if the branch should be taken</param>
        /// <param name="configuration">Configures a branch to take</param>
        public static void WhenTypeIs<TInput, TOutput, TType>(
            this IPipelineBuilder<TInput, TOutput> builder,
            Action<IPipelineBuilder<TType, TOutput>> configuration)
            where TType : TInput
        {
            var innerPipeline = builder.New(configuration);

            builder.Use(next =>
            {
                Func<TType, TOutput> tail = innerPipeline(x => next(x));

                return input =>
                {
                    if (input is TType)
                    {
                        return tail((TType)input);
                    }
                    else
                    {
                        return next(input);
                    }
                };
            });
        }

        /// <summary>
        /// Branches the request pipeline when the input if of a given type and matches a given predicate.
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="builder"></param>
        /// <param name="predicate">Invoked with the request environment to determine if the branch should be taken</param>
        /// <param name="configuration">Configures a branch to take</param>
        public static void When<TInput, TOutput, TType>(
            this IPipelineBuilder<TInput, TOutput> builder,
            Func<TType, bool> predicate,
            Action<IPipelineBuilder<TType, TOutput>> configuration)
            where TType : TInput
        {
            var innerPipeline = builder.New(configuration);

            builder.Use(next =>
            {
                Func<TType, TOutput> tail = innerPipeline(x => next(x));

                return input =>
                {
                    if (input is TType)
                    {
                        TType value = (TType)input;
                        if (predicate(value))
                        {
                            return tail((TType)input);
                        }
                    }

                    return next(input);
                };
            });
        }

        /// <summary>
        /// Branches the request pipeline based on the result of the given predicate.
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="builder"></param>
        /// <param name="predicate">Invoked with the request environment to determine if the branch should be taken</param>
        /// <param name="configuration">Configures a branch to take</param>
        public static void WhenAsync<TInput, TOutput>(
            this IPipelineBuilder<TInput, Task<TOutput>> builder,
            Func<TInput, Task<bool>> predicate,
            Action<IPipelineBuilder<TInput, Task<TOutput>>> configuration)
        {
            var innerPipeline = builder.New(configuration);

            builder.Use(next =>
            {
                Func<TInput, Task<TOutput>> tail = innerPipeline(next);

                return async input =>
                {
                    Func<TInput, Task<TOutput>> f = await predicate(input)
                        ? tail
                        : next;
                    return await f(input);
                };
            });
        }

        #endregion

        #region Switch

        public static void Switch<TInput, TOutput>(
            this IPipelineBuilder<TInput, TOutput> builder,
            Func<TInput, int?> predicate,
            params Action<IPipelineBuilder<TInput, TOutput>>[] configurations)
        {
            Func<Func <TInput, TOutput>, Func<TInput, TOutput>>[] innerPipelines = configurations
                .Select(item => builder.New(item))
                .ToArray();

            builder.Use(next =>
            {
                Func<TInput, TOutput>[] tailMethods = innerPipelines.Select(f => f(next)).ToArray();

                return input =>
                {
                    int? clause = predicate(input);
                    Func<TInput, TOutput> method = (clause == null)
                        ? next
                        : tailMethods[clause.Value];

                    return method(input);
                };
            });
        }

        public static void SwitchAsync<TInput, TOutput>(
            this IPipelineBuilder<TInput, Task<TOutput>> builder,
            Func<TInput, Task<int?>> predicate,
            params Action<IPipelineBuilder<TInput, Task<TOutput>>>[] configurations)
        {
            Func<Func<TInput, Task<TOutput>>, Func<TInput, Task<TOutput>>>[] innerPipelines =
                configurations.Select(item => builder.New(item)).ToArray();

            builder.Use(next =>
            {
                Func<TInput, Task<TOutput>>[] tailMethods = innerPipelines.Select(f => f(next)).ToArray();

                return async input =>
                {
                    int? clause = await predicate(input);
                    Func<TInput, Task<TOutput>> method = (clause == null)
                        ? next
                        : tailMethods[clause.Value];

                    return await method(input);
                };
            });
        }

        #endregion

        #region Retry

        public static void Retry<TInput, TOutput, TRetrySeed>(
            this IPipelineBuilder<TInput, TOutput> builder,
            int maxCount,
            Func<Exception, bool> shouldRetry
            )
        {
            builder.Try(next => input =>
            {
                int count = maxCount;
                Task<TOutput> result;
                do
                {
                    result = next(input);
                    --count;
                }
                while ((count > 0) && result.IsFaulted && shouldRetry(result.Exception));

                return result.Result;
            });
        }

        /// <summary>
        /// Add a handler to the pipeline which will receive the result of the subsequent pipeline steps as
        /// a completed <see cref="Task"/>.  The task returned from the inner pipeline will be completed or
        /// faulted.
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <typeparam name="TOutput"></typeparam>
        /// <param name="builder"></param>
        /// <param name="handler"></param>
        public static void Try<TInput, TOutput>(
            this IPipelineBuilder<TInput, TOutput> builder,
            Func<Func<TInput, Task<TOutput>>, Func<TInput, TOutput>> handler)
        {
            builder.Use(next =>
            {
                return handler(input =>
                {
                    var box = new TaskCompletionSource<TOutput>();
                    try
                    {
                        TOutput result = next(input);
                        box.SetResult(result);
                    }
                    catch (Exception e)
                    {
                        box.SetException(e);
                    }
                    return box.Task;
                });
            });
        }

        #endregion
    }
}
