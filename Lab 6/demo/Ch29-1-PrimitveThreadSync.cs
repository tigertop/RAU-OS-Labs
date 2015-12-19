using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

public static class PrimitveThreadSync {
   public static void Main() {
      //OptimizedAway();
      //StrangeBehavior.Go();
      //AsyncCoordinatorDemo.Go();
      SingleInstance.Go();
      //LockComparison.Go();
      //RegisteredWaitHandleDemo.Go();
   }

   private static void OptimizedAway() {
      // An expression of constants is computed at compile time then put into a local variable that is never used
      Int32 value = 1 * 100 + 0 / 1 % 2;
      if (value >= 0) Console.WriteLine("Jeff");

      for (Int32 x = 0; x < 1000; x++) ;   // A loop that does nothing
   }
}

#region LinkedList Synchronization
// This class is used by the LinkedList class
public class Node { internal Node m_next; }

public sealed class LinkedList {
   private Node m_head;

   public void Add(Node newNode) {
      // The two lines below perform very fast reference assignments
      newNode.m_next = m_head;
      m_head = newNode;
   }
}
#endregion

internal static class StrangeBehavior {
   // Compile with "/platform:x86 /o" and run it NOT under the debugger (Ctrl+F5)
   private static Boolean s_stopWorker = false;

   public static void Go() {
      Console.WriteLine("Main: letting worker run for 5 seconds");
      Thread t = new Thread(Worker);
      t.Start();
      Thread.Sleep(5000);
      s_stopWorker = true;
      Console.WriteLine("Main: waiting for worker to stop");
      t.Join();
      Environment.Exit(0);
   }

   private static void Worker(Object o) {
      Int32 x = 0;
      while (!s_stopWorker) x++;
      Console.WriteLine("Worker: stopped when x={0}", x);
   }
}

internal static class ThreadsSharingData {
   internal sealed class ThreadsSharingDataV1 {
      private Int32 m_flag = 0;
      private Int32 m_value = 0;

      // This method is executed by one thread 
      public void Thread1() {
         // Note: These could execute in reverse order
         m_value = 5;
         m_flag = 1;
      }

      // This method is executed by another thread 
      public void Thread2() {
         // Note: m_value could be read before m_flag
         if (m_flag == 1) Console.WriteLine(m_value);
      }
   }

   internal sealed class ThreadsSharingDataV2 {
      private Int32 m_flag = 0;
      private Int32 m_value = 0;

      // This method is executed by one thread 
      public void Thread1() {
         // Note: 5 must be written to m_value before 1 is written to m_flag
         m_value = 5;
         Volatile.Write(ref m_flag, 1);
           
      }

      // This method is executed by another thread 
      public void Thread2() {
         // Note: m_value must be read after m_flag is read 
         if (Volatile.Read(ref m_flag) == 1)
            Console.WriteLine(m_value);
      }
   }

   internal sealed class ThreadsSharingDataV3 {
      private volatile Int32 m_flag = 0;
      private Int32 m_value = 0;

      // This method is executed by one thread 
      public void Thread1() {
         // Note: 5 must be written to m_value before 1 is written to m_flag
         m_value = 5;
         m_flag = 1;
      }

      // This method is executed by another thread 
      public void Thread2() {
         // Note: m_value must be read after m_flag is read 
         if (m_flag == 1)
            Console.WriteLine(m_value);
      }
   }
}

internal static class SingleInstance
{
    public static void Go()
    {
        Boolean createdNew;
        // Try to create a kernel object with the specified name
        using (new Semaphore(0, 1, "SomeUniqueStringIdentifyingMyApp", out createdNew))
        {
            if (createdNew)
            {
                // This thread created the kernel object so no other instance of this
                // application must be running. Run the rest of the application here... 
                Console.WriteLine("I am number one :)");
                Console.ReadLine();
            }
            else
            {
                // This thread opened an existing kernel object with the same string name;   
                // another instance of this application must be running now.             
                // There is nothing to do in here, let's just return from Main to terminate  
                // this second instance of the application.
                Console.WriteLine("An another instance of the app is currently running :(");
                Console.ReadLine();
            }
        }
    }
}       

internal static class AsyncCoordinatorDemo {
   public static void Go() {
      const Int32 timeout = 50000;   // Change to desired timeout
      MultiWebRequests act = new MultiWebRequests(timeout);
      Console.WriteLine("All operations initiated (Timeout={0}). Hit <Enter> to cancel.",
         (timeout == Timeout.Infinite) ? "Infinite" : (timeout.ToString() + "ms"));
      Console.ReadLine();
      act.Cancel();

      Console.WriteLine();
      Console.WriteLine("Hit enter to terminate.");
      Console.ReadLine();
   }

   private sealed class MultiWebRequests {
      // This helper class coordinates all the asynchronous operations
      private AsyncCoordinator m_ac = new AsyncCoordinator();

      // Set of Web servers we want to query & their responses (Exception or Int32)
      private Dictionary<String, Object> m_servers = new Dictionary<String, Object> {
         { "http://Wintellect.com/", null },
         { "http://Microsoft.com/",  null },
         { "http://1.1.1.1/",        null } 
      };

      public MultiWebRequests(Int32 timeout = Timeout.Infinite) {
         // Asynchronously initiate all the requests all at once
         var httpClient = new HttpClient();
         foreach (var server in m_servers.Keys) {
            m_ac.AboutToBegin(1);
            httpClient.GetByteArrayAsync(server).ContinueWith(task => ComputeResult(server, task));
         }

         // Tell AsyncCoordinator that all operations have been initiated and to call
         // AllDone when all operations complete, Cancel is called, or the timeout occurs
         m_ac.AllBegun(AllDone, timeout);
      }

      private void ComputeResult(String server, Task<Byte[]> task) {
         Object result;
         if (task.Exception != null) {
            result = task.Exception.InnerException;
         } else {
            // Process I/O completion here on thread pool thread(s)
            // Put your own compute-intensive algorithm here...
            result = task.Result.Length;   // This example just returns the length
         }

         // Save result (exception/sum) and indicate that 1 operation completed
         m_servers[server] = result;
         m_ac.JustEnded();
      }

      // Calling this method indicates that the results don't matter anymore
      public void Cancel() { m_ac.Cancel(); }

      // This method is called after all Web servers respond, 
      // Cancel is called, or the timeout occurs
      private void AllDone(CoordinationStatus status) {
         switch (status) {
            case CoordinationStatus.Cancel:
               Console.WriteLine("Operation canceled.");
               break;

            case CoordinationStatus.Timeout:
               Console.WriteLine("Operation timed-out.");
               break;

            case CoordinationStatus.AllDone:
               Console.WriteLine("Operation completed; results below:");
               foreach (var server in m_servers) {
                  Console.Write("{0} ", server.Key);
                  Object result = server.Value;
                  if (result is Exception) {
                     Console.WriteLine("failed due to {0}.", result.GetType().Name);
                  } else {
                     Console.WriteLine("returned {0:N0} bytes.", result);
                  }
               }
               break;
         }
      }
   }

   private enum CoordinationStatus {
      AllDone,
      Timeout,
      Cancel
   };

   private sealed class AsyncCoordinator {
      private Int32 m_opCount = 1;        // Decremented when AllBegun calls JustEnded
      private Int32 m_statusReported = 0; // 0=false, 1=true
      private Action<CoordinationStatus> m_callback;
      private Timer m_timer;

      // This method MUST be called BEFORE initiating an operation
      public void AboutToBegin(Int32 opsToAdd = 1) {
         Interlocked.Add(ref m_opCount, opsToAdd);
      }

      // This method MUST be called AFTER an operations result has been processed
      public void JustEnded() {
         if (Interlocked.Decrement(ref m_opCount) == 0)
            ReportStatus(CoordinationStatus.AllDone);
      }

      // This method MUST be called AFTER initiating ALL operations
      public void AllBegun(Action<CoordinationStatus> callback, Int32 timeout = Timeout.Infinite) {
         m_callback = callback;
         if (timeout != Timeout.Infinite) {
            m_timer = new Timer(TimeExpired, null, timeout, Timeout.Infinite);
         }
         JustEnded();
      }

      private void TimeExpired(Object o) { ReportStatus(CoordinationStatus.Timeout); }

      public void Cancel() {
         if (m_callback == null)
            throw new InvalidOperationException("Cancel cannot be called before AllBegun");
         ReportStatus(CoordinationStatus.Cancel);
      }

      private void ReportStatus(CoordinationStatus status) {
         if (m_timer != null) {  // If timer is still in play, kill it
            Timer timer = Interlocked.Exchange(ref m_timer, null);
            if (timer != null) timer.Dispose();
         }

         // If status has never been reported, report it; else ignore it
         if (Interlocked.Exchange(ref m_statusReported, 1) == 0)
            m_callback(status);
      }
   }
}

internal static class LockComparison {
   public static void Go() {
      Int32 x = 0;
      const Int32 iterations = 10000000;  // 10 million

      // How long does it take to increment x 10 million times?
      Stopwatch sw = Stopwatch.StartNew();
      for (Int32 i = 0; i < iterations; i++) {
         x++;
      }
      Console.WriteLine("Incrementing x: {0:N0}", sw.ElapsedMilliseconds);

      // How long does it take to increment x 10 million times 
      // adding the overhead of calling a method that does nothing?
      sw.Restart();
      for (Int32 i = 0; i < iterations; i++) {
         M(); x++; M();
      }
      Console.WriteLine("Incrementing x in M: {0:N0}", sw.ElapsedMilliseconds);

      // How long does it take to increment x 10 million times 
      // adding the overhead of calling an uncontended SimpleSpinLock?
      SimpleSpinLock ssl = new SimpleSpinLock();
      sw.Restart();
      for (Int32 i = 0; i < iterations; i++) {
         ssl.Enter(); x++; ssl.Leave();
      }
      Console.WriteLine("Incrementing x in SimpleSpinLock: {0:N0}", sw.ElapsedMilliseconds);

      // How long does it take to increment x 10 million times 
      // adding the overhead of calling an uncontended SpinLock?
      SpinLock sl = new SpinLock(false);
      sw.Restart();
      for (Int32 i = 0; i < iterations; i++) {
         Boolean taken = false;
         sl.Enter(ref taken); x++; sl.Exit(false);
      }
      Console.WriteLine("Incrementing x in SpinLock: {0:N0}", sw.ElapsedMilliseconds);

      // How long does it take to increment x 10 million times 
      // adding the overhead of calling an uncontended SimpleWaitLock?
      using (SimpleWaitLock swl = new SimpleWaitLock()) {
         sw.Restart();
         for (Int32 i = 0; i < iterations; i++) {
            swl.Enter(); x++; swl.Leave();
         }
         Console.WriteLine("Incrementing x in SimpleWaitLock: {0:N0}", sw.ElapsedMilliseconds);
      }

        //using (SimpleSempahoreLock swl = new SimpleSempahoreLock(1, 1))
        //{
        //    sw.Restart();
        //    for (Int32 i = 0; i < iterations; i++)
        //    {
        //        swl.Enter(); x++; swl.Leave();
        //    }
        //    Console.WriteLine("Incrementing x in SimpleSempahoreLock: {0:N0}", sw.ElapsedMilliseconds);
        //}

        Console.ReadLine();
   }

   [MethodImpl(MethodImplOptions.NoInlining)]
   private static void M() { }

   internal struct SimpleSpinLock {
      private Int32 m_ResourceInUse; // 0=false (default), 1=true

      public void Enter() {
         while (true) {
            // Always set resource to in-use
            // When this thread changes it from not in-use, return
            if (Interlocked.Exchange(ref m_ResourceInUse, 1) == 0) return;
            // Black magic goes here...
         }
      }

      public void Leave() {
         // Set resource to not in-use
         Volatile.Write(ref m_ResourceInUse, 0);
      }
   }

   private sealed class SimpleWaitLock : IDisposable {
      private readonly AutoResetEvent m_available;
      public SimpleWaitLock() {
         m_available = new AutoResetEvent(true); // Initially free
      }

      public void Enter() {
         // Block in kernel until resource available
         m_available.WaitOne();
      }

      public void Leave() {
         // Let another thread access the resource
         m_available.Set();
      }

      public void Dispose() { m_available.Dispose(); }
   }

    private sealed class SimpleSempahoreLock : IDisposable
    {
        private readonly Semaphore m_available;
        public SimpleSempahoreLock(int initialCount, int maxCount)
        {
            m_available = new Semaphore(initialCount, maxCount); // Initially free1
        }

        public void Enter()
        {
            // Block in kernel until resource available
            m_available.WaitOne();
        }

        public void Leave()
        {
            // Let another thread access the resource
            m_available.Release(1);
        }

        public void Dispose() { m_available.Dispose(); }
    }
}

internal sealed class RecursiveAutoResetEvent : IDisposable {
   private AutoResetEvent m_lock = new AutoResetEvent(true);
   private Int32 m_owningThreadId = 0;
   private Int32 m_recursionCount = 0;

   public void Enter() {
      // Obtain the calling thread's unique Int32 ID
      Int32 currentThreadId = Thread.CurrentThread.ManagedThreadId;

      // If the calling thread owns the lock, increment the recursion count
      if (m_owningThreadId == currentThreadId) {
         m_recursionCount++;
         return;
      }

      // The calling thread doesn't own the lock, wait for it
      m_lock.WaitOne();

      // The calling now owns the lock, initialize the owning thread ID & recursion count
      m_owningThreadId = currentThreadId;
      m_recursionCount--;
   }

   public void Leave() {
      // If the calling thread doesn't own the lock, we have an error
      if (m_owningThreadId != Thread.CurrentThread.ManagedThreadId)
         throw new InvalidOperationException();

      // Subtract 1 from the recursion count
      if (--m_recursionCount == 0) {
         // If the recursion count is 0, then no thread owns the lock
         m_owningThreadId = 0;
         m_lock.Set();   // Wake up 1 waiting thread (if any)
      }
   }

   public void Dispose() { m_lock.Dispose(); }
}




