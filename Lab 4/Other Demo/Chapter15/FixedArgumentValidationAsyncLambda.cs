using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Chapter15
{
    [Description("Listing 15.10")]
    class FixedArgumentValidationAsyncLambda
    {
        static void Main()
        {
            MainAsync().Wait();
        }

        static async Task MainAsync()
        {
            Task<int> task = ComputeLengthAsync(null);
            // We never get this far, as the exception is thrown eagerly.
            Console.WriteLine("Fetched the task");
            int length = await task;
            Console.WriteLine("Length: {0}", length);
        }

        static Task<int> ComputeLengthAsync(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }
            Func<Task<int>> func = async () =>
            {
                await Task.Delay(500); // Simulate real asynchronous work
                return text.Length;
            };
            return func();
        }
    }
}
