using System;
using System.Threading.Tasks;

namespace Chapter15
{
    class LogicalStack
    {
        static void Main()
        {
            Task task = DemonstrateStacks();
            task.Wait();
        }

        static async Task DemonstrateStacks()
        {
            int y = 10;
            Task<int> z = Task.FromResult(10);
            var x = y * await z;

            Task<int> task = Task.FromResult(20);
            Console.WriteLine("{0} {1}", x, await task);
        }
    }
}
