using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Chapter15
{
    public static partial class TaskExtensions
    {
        [Description("Listing 15.12")]
        public static IEnumerable<Task<T>> InCompletionOrder<T>(this IEnumerable<Task<T>> source)
        {
            var inputs = source.ToList();
            var boxes = inputs.Select(x => new TaskCompletionSource<T>()).ToList();

            int currentIndex = -1;
            foreach (var task in inputs)
            {
                task.ContinueWith(completed =>
                {
                    var nextBox = boxes[Interlocked.Increment(ref currentIndex)];
                    PropagateResult(completed, nextBox);
                }, TaskContinuationOptions.ExecuteSynchronously);
            }
            return boxes.Select(box => box.Task);
        }

        /// <summary> 
        /// Propagates the status of the given task (which must be completed) to a task completion source 
        /// (which should not be). 
        /// </summary> 
        private static void PropagateResult<T>(Task<T> completedTask,
            TaskCompletionSource<T> completionSource)
        {
            switch (completedTask.Status)
            {
                case TaskStatus.Canceled:
                    completionSource.TrySetCanceled();
                    break;
                case TaskStatus.Faulted:
                    completionSource.TrySetException(completedTask.Exception.InnerExceptions);
                    break;
                case TaskStatus.RanToCompletion:
                    completionSource.TrySetResult(completedTask.Result);
                    break;
                default:
                    // TODO: Work out whether this is really appropriate. Could set 
                    // an exception in the completion source, of course... 
                    throw new ArgumentException("Task was not completed");
            }
        }
    }

    [Description("Listing 15.13")]
    static class MagicOrdering
    {
        static async Task<int> ShowPageLengthsAsync(params string[] urls)
        {
            var tasks = urls.Select(async url =>
            { 
                using (var client = new HttpClient())
                {
                    return await client.GetStringAsync(url);
                }
            }).ToList();

            int total = 0;
            foreach (var task in tasks.InCompletionOrder())
            {
                string page = await task;
                Console.WriteLine("Got page length {0}", page.Length);
                total += page.Length;
            }
            return total;
        }

        static void Main()
        {
            var task = ShowPageLengthsAsync("http://stackoverflow.com", "http://www.google.com", "http://csharpindepth.com");
            Console.WriteLine("Total length: {0}", task.Result);
        }
    }
}
