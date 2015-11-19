using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Chapter15
{
    [Description("Listing 15.05")]
    class BrokenArgumentValidation
    {
        static void Main()
        {
            MainAsync().Wait();
        }

        static async Task MainAsync()
        {
            Task<int> task = ComputeLengthAsync(null);
            Console.WriteLine("Fetched the task");
            int length = await task;
            Console.WriteLine("Length: {0}", length);
        }

        static async Task<int> ComputeLengthAsync(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }
            await Task.Delay(500); // Simulate real asynchronous work
            return text.Length;
        }
    }
}
