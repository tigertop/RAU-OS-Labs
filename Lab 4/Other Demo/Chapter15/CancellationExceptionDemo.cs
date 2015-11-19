using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chapter15
{
    [Description("Listing 15.07")]
    class CancellationExceptionDemo
    {
        static void Main()
        {
            Task task = ThrowCancellationException();
            Console.WriteLine(task.Status);
        }

        static async Task ThrowCancellationException()
        {
            throw new OperationCanceledException();
        }
    }
}
