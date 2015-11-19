using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Chapter15
{
    public static partial class TaskExtensions
    {
        [Description("Listing 15.03")]
        public static AggregatedExceptionAwaitable WithAggregatedExceptions(this Task task)
        {
            if (task == null)
            {
                throw new ArgumentNullException("task");
            }

            return new AggregatedExceptionAwaitable(task);
        }

        public struct AggregatedExceptionAwaitable
        {
            private readonly Task task;

            internal AggregatedExceptionAwaitable(Task task)
            {
                this.task = task;
            }

            public AggregatedExceptionAwaiter GetAwaiter()
            {
                return new AggregatedExceptionAwaiter(task);
            }
        }

        public struct AggregatedExceptionAwaiter : ICriticalNotifyCompletion
        {
            private readonly Task task;

            internal AggregatedExceptionAwaiter(Task task)
            {
                this.task = task;
            }

            // Delegate most members to the task's awaiter
            public bool IsCompleted { get { return task.GetAwaiter().IsCompleted; } }

            public void UnsafeOnCompleted(Action continuation)
            {
 	            task.GetAwaiter().UnsafeOnCompleted(continuation);
            }

            public void OnCompleted(Action continuation)
            {
                task.GetAwaiter().OnCompleted(continuation);
            }

            public void GetResult()
            {
                // This will throw AggregateException directly on failure,
                // unlike task.GetAwaiter().GetResult()
                task.Wait();
            }
        }
    }

    [Description("Listing 15.02")]
    class AggregatedExceptions
    {
        static void Main()
        {
            MainAsync().Wait();
        }

        private async static Task MainAsync()
        {
            Task task1 = Task.Run(() => { throw new Exception("Message 1"); });
            Task task2 = Task.Run(() => { throw new Exception("Message 2"); });

            try
            {
                await Task.WhenAll(task1, task2);
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught {0}", e.Message);
            }

            try
            {
                await Task.WhenAll(task1, task2).WithAggregatedExceptions();
            }
            catch (AggregateException e)
            {
                Console.WriteLine("Caught {0} exceptions: {1}", e.InnerExceptions.Count,
                                  string.Join(", ", e.InnerExceptions.Select(x => x.Message)));
            }
        }
    }
}
