using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Chapter15
{
    [Description("Listing 15.11")]
    class DecompilationSample
    {
        static void Main()
        {
            Task<int> task = SumCharactersAsync("test");
            Console.WriteLine(task.Result);
        }

        static async Task<int> SumCharactersAsync(IEnumerable<char> text)
        {
            int total = 0;
            foreach (char ch in text)
            {
                int unicode = ch;
                await Task.Delay(unicode);
                total += unicode;
            }

            await Task.Yield();

            return total;
        }
    }
}
