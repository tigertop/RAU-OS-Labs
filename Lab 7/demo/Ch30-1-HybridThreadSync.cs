using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Globalization;

public static class HybridThreadSync {
   public static void Main() {
      HybridLocks.Go();
      //Singletons.Go();
      //AsyncSynchronization.Go();
      //BlockingCollectionDemo.Go();
      Console.ReadLine();
   }
}

internal static class HybridLocks {
   public static void Go() {
      Int32 x = 0;
      const Int32 iterations = 10000000;  // 10 million

      // How long does it take to increment x 10 million times 
      // adding the overhead of calling an uncontended SimpleHybridLock?
      var shl = new SimpleHybridLock();
      shl.Enter(); x++; shl.Leave();
      Stopwatch sw = Stopwatch.StartNew();
      for (Int32 i = 0; i < iterations; i++) {
         shl.Enter(); x++; shl.Leave();
      }
      Console.WriteLine("Incrementing x in SimpleHybridLock: {0:N0}", sw.ElapsedMilliseconds);

      // How long does it take to increment x 10 million times 
      // adding the overhead of calling an uncontended ANotherHybridLock?
      using (var ahl = new AnotherHybridLock()) {
         ahl.Enter(); x++; ahl.Leave();
         sw.Restart();
         for (Int32 i = 0; i < iterations; i++) {
            ahl.Enter(); x++; ahl.Leave();
         }
         Console.WriteLine("Incrementing x in AnotherHybridLock: {0:N0}", sw.ElapsedMilliseconds);
      }

      //using (var oml = new OneManyLock()) {
      //   oml.Enter(true); x++; oml.Leave();
      //   sw.Restart();
      //   for (Int32 i = 0; i < iterations; i++) {
      //      oml.Enter(true); x++; oml.Leave();
      //   }
      //   Console.WriteLine("Incrementing x in OneManyLock: {0:N0}", sw.ElapsedMilliseconds);
      //}

      using (var rwls = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion)) {
         rwls.EnterReadLock(); x++; rwls.ExitReadLock();
         sw.Restart();
         for (Int32 i = 0; i < iterations; i++) {
            rwls.EnterReadLock(); x++; rwls.ExitReadLock();
         }
         Console.WriteLine("Incrementing x in ReaderWriterLockSlim: {0:N0}", sw.ElapsedMilliseconds);
      }

      var rwl = new ReaderWriterLock();
      rwl.AcquireReaderLock(Timeout.Infinite); x++; rwl.ReleaseReaderLock();
      sw.Restart();
      for (Int32 i = 0; i < iterations; i++) {
         rwl.AcquireReaderLock(Timeout.Infinite); x++; rwl.ReleaseReaderLock();
      }
      Console.WriteLine("Incrementing x in ReaderWriterLock: {0:N0}", sw.ElapsedMilliseconds);

      Object l = new Object();
      Monitor.Enter(l); x++; Monitor.Exit(l);
      sw.Restart();
      for (Int32 i = 0; i < iterations; i++) {
         Monitor.Enter(l); x++; Monitor.Exit(l);
      }
      Console.WriteLine("Incrementing x in Monitor: {0:N0}", sw.ElapsedMilliseconds);

      sw.Restart();
      for (Int32 i = 0; i < iterations; i++) {
         lock (l) { x++; }
      }
      Console.WriteLine("Incrementing x in lock: {0:N0}", sw.ElapsedMilliseconds);
      Console.ReadLine();
   }

   public sealed class SimpleHybridLock : IDisposable {
      // The Int32 is used by the primitive user-mode constructs (Interlocked mehtods)
      private Int32 m_waiters = 0;

      // The AutoResetEvent is the primitive kernel-mode construct
      private readonly AutoResetEvent m_waiterLock = new AutoResetEvent(false);

      public void Enter() {
         // Indicate that this thread wants the lock
         if (Interlocked.Increment(ref m_waiters) == 1)
            return; // Lock was free, no contention, just return

         // There is contention, block this thread
         m_waiterLock.WaitOne();  // Bad performance hit here
         // When WaitOne returns, this thread now has the lock
      }

      public void Leave() {
         // This thread is releasing the lock
         if (Interlocked.Decrement(ref m_waiters) == 0)
            return; // No other threads are blocked, just return

         // Other threads are blocked, wake 1 of them
         m_waiterLock.Set();  // Bad performance hit here
      }

      public void Dispose() { m_waiterLock.Dispose(); }
   }

   public sealed class AnotherHybridLock : IDisposable {
      // The Int32 is used by the primitive user-mode constructs (Interlocked methods)
      private Int32 m_waiters = 0;

      // The AutoResetEvent is the primitive kernel-mode construct
      private AutoResetEvent m_waiterLock = new AutoResetEvent(false);

      // This field controls spinning in an effort to improve performance
      private Int32 m_spincount = 4000;   // Arbitrarily chosen count

      // These fields indicate which thread owns the lock and how many times it owns it
      private Int32 m_owningThreadId = 0, m_recursion = 0;

      public void Enter() {
         // If the calling thread already owns this lock, increment the recursion count and return
         Int32 threadId = Thread.CurrentThread.ManagedThreadId;
         if (threadId == m_owningThreadId) { m_recursion++; return; }

            // The calling thread doesn't own the lock, try to get it
            //on single-core computers, SpinWait yields the time slice of the thread immediately because spinning blocks forward progress on all threads.
            //SpinWait also yields even on multi-core machines to prevent the waiting thread from blocking higher-priority threads or the garbage collector.
            //Therefore, if you are using a SpinWait in a two-phase wait operation, we recommend that you invoke the kernel wait before the SpinWait 
            //itself initiates a context switch.SpinWait provides the NextSpinWillYield property, which you can check before every call to SpinOnce. 
            //When the property returns true, initiate your own Wait operation.
         SpinWait spinwait = new SpinWait();
         for (Int32 spinCount = 0; spinCount < m_spincount; spinCount++) {
            // If the lock was free, this thread got it; set some state and return
            if (Interlocked.CompareExchange(ref m_waiters, 1, 0) == 0) goto GotLock;

            // Black magic: give others threads a chance to run 
            // in hopes that the lock will be released
            spinwait.SpinOnce();
         }

         // Spinning is over and the lock was still not obtained, try one more time
         if (Interlocked.Increment(ref m_waiters) > 1) {
            // Other threads are blocked and this thread must block too
            m_waiterLock.WaitOne(); // Wait for the lock; performance hit
            // When this thread wakes, it owns the lock; set some state and return
         }

      GotLock:
         // When a thread gets the lock, we record its ID and 
         // indicate that the thread owns the lock once
         m_owningThreadId = threadId; m_recursion = 1;
      }

      public void Leave() {
         // If the calling thread doesn't own the lock, there is a bug
         Int32 threadId = Thread.CurrentThread.ManagedThreadId;
         if (threadId != m_owningThreadId)
            throw new SynchronizationLockException("Lock not owned by calling thread");

         // Decrement the recursion count. If this thread still owns the lock, just return
         if (--m_recursion > 0) return;

         m_owningThreadId = 0;   // No thread owns the lock now

         // If no other threads are blocked, just return
         if (Interlocked.Decrement(ref m_waiters) == 0)
            return;

         // Other threads are blocked, wake 1 of them
         m_waiterLock.Set();	// Bad performance hit here
      }

      public void Dispose() { m_waiterLock.Dispose(); }
   }

    internal sealed class Transaction
    {
        private DateTime m_timeOfLastTrans;
        public void PerformTransaction()
        {
            Monitor.Enter(this);
            // This code has exclusive access to the data...   
            m_timeOfLastTrans = DateTime.Now;
            Monitor.Exit(this);
        }  
        public DateTime LastTransaction
        {
            get
            {
                Monitor.Enter(this);
                // This code has exclusive access to the data... 
                DateTime temp = m_timeOfLastTrans;
                Monitor.Exit(this);
                return temp;
            }
        }
    }

    public static void SomeMethod()
    {
        var t = new Transaction();
        Monitor.Enter(t);
        // This thread takes the object's public lock  
        // Have a thread pool thread display the LastTransaction time  
        // NOTE: The thread pool thread blocks until SomeMethod calls Monitor.Exit! 
        ThreadPool.QueueUserWorkItem(o => Console.WriteLine(t.LastTransaction));
        // Execute some other code here...    
        Monitor.Exit(t);
    }

    private sealed class Transactions : IDisposable {
      private readonly ReaderWriterLockSlim m_lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
      private DateTime m_timeOfLastTrans;

      public void PerformTransaction() {
         m_lock.EnterWriteLock();
         // This code has exclusive access to the data...
         m_timeOfLastTrans = DateTime.Now;
         m_lock.ExitWriteLock();
      }

      public DateTime LastTransaction {
         get {
            m_lock.EnterReadLock();
            // This code has shared access to the data...
            DateTime temp = m_timeOfLastTrans;
            m_lock.ExitReadLock();
            return temp;
         }
      }
      public void Dispose() { m_lock.Dispose(); }
   }
}

internal static class Singletons {
   public static class V1 {
      public sealed class Singleton {
         // s_lock is required for thread safety and having this object assumes that creating  
         // the singleton object is more expensive than creating a System.Object object and that 
         // creating the singleton object may not be necessary at all. Otherwise, it is more  
         // efficient and easier to just create the singleton object in a class constructor
         private static readonly Object s_lock = new Object();

         // This field will refer to the one Singleton object
         private static Singleton s_value = null;

         // Private constructor prevents any code outside this class from creating an instance 
         private Singleton() { /* ... */ }

         // Public, static method that returns the Singleton object (creating it if necessary) 
         public static Singleton GetSingleton() {
            // If the Singleton was already created, just return it (this is fast)
            if (s_value != null) return s_value;

            Monitor.Enter(s_lock);  // Not created, let 1 thread create it
            if (s_value == null) {
               // Still not created, create it
               Singleton temp = new Singleton();

               // Save the reference in s_value (see discussion for details)
               Volatile.Write(ref s_value, temp);
            }
            Monitor.Exit(s_lock);

            // Return a reference to the one Singleton object 
            return s_value;
         }
      }
   }

   public static class V2 {
      public sealed class Singleton {
         private static Singleton s_value = new Singleton();

         // Private constructor prevents any code outside this class from creating an instance 
         private Singleton() { }

         // Public, static method that returns the Singleton object (creating it if necessary) 
         public static Singleton GetSingleton() { return s_value; }
      }
   }

   public static class V3 {
      public sealed class Singleton {
         private static Singleton s_value = null;

         // Private constructor prevents any code outside this class from creating an instance 
         private Singleton() { }

         // Public, static method that returns the Singleton object (creating it if necessary) 
         public static Singleton GetSingleton() {
            if (s_value != null) return s_value;

            // Create a new Singleton and root it if another thread didn’t do it first
            Singleton temp = new Singleton();
            Interlocked.CompareExchange(ref s_value, temp, null);

            // If this thread lost, then the second Singleton object gets GC’d

            return s_value; // Return reference to the single object
         }
      }
   }

   public static void Go() {
      Lazy<String> s = new Lazy<String>(() => DateTime.Now.ToLongTimeString(), true);
      Console.WriteLine(s.IsValueCreated);   // false
      Console.WriteLine(s.Value);                  // Lambda is invoked now
      Console.WriteLine(s.IsValueCreated);   // true
      Thread.Sleep(10000);
      Console.WriteLine(s.Value);                  // Lambda is NOT invoked now; same result

      String name = null;
      LazyInitializer.EnsureInitialized(ref name, () => "Jeff");
      Console.WriteLine(name);   // Jeff

      LazyInitializer.EnsureInitialized(ref name, () => "Richter");
      Console.WriteLine(name);   // Jeff
   }
}

internal static class ConditionVariables {
   public sealed class ConditionVariablePattern {
      private readonly Object m_lock = new Object();
      private Boolean m_condition = false;

      public void Thread1() {
         Monitor.Enter(m_lock);        // Acquire a mutual-exclusive lock

         // While under the lock, test the complex condition "atomically"
         while (!m_condition) {
            // If condition is not met, wait for another thread to change the condition
            Monitor.Wait(m_lock);	   // Temporarily release lock so other threads can get it
         }

         // The condition was met, process the data...

         Monitor.Exit(m_lock);         // Permanently release lock
      }

      public void Thread2() {
         Monitor.Enter(m_lock);        // Acquire a mutual-exclusive lock

         // Process data and modify the condition...
         m_condition = true;

         // Monitor.Pulse(m_lock);	   // Wakes one waiter AFTER lock is released
         Monitor.PulseAll(m_lock);	   // Wakes all waiters AFTER lock is released

         Monitor.Exit(m_lock);         // Release lock
      }
   }

   public sealed class SynchronizedQueue<T> {
      private readonly Object m_lock = new Object();
      private readonly Queue<T> m_queue = new Queue<T>();

      public void Enqueue(T item) {
         Monitor.Enter(m_lock);

         m_queue.Enqueue(item);
         Monitor.PulseAll(m_lock); // Wakeup any/all waiters

         Monitor.Exit(m_lock);
      }

      public T Dequeue() {
         Monitor.Enter(m_lock);

         // Loop waiting for condition (queue not empty)
         while (m_queue.Count == 0)
            Monitor.Wait(m_queue);

         T item = m_queue.Dequeue();
         Monitor.Exit(m_lock);
         return item;
      }
   }
}

internal static class AsyncSynchronization {
   public static void Go() {
      //SemaphoreSlimDemo();
      ConcurrentExclusiveSchedulerDemo();
      OneManyDemo();
   }

   private static void SemaphoreSlimDemo() {
      SemaphoreSlim asyncLock = new SemaphoreSlim(1, 1);
      List<Task> tasks = new List<Task>();
      for (Int32 op = 0; op < 5; op++) {
         var capturedOp = op;
         tasks.Add(Task.Run(() => AccessResourceViaAsyncSynchronization(asyncLock, capturedOp)));
         Thread.Sleep(200);
      }
      Task.WaitAll(tasks.ToArray());
      Console.WriteLine("All operations done");
      Console.ReadLine();
   }

   private static async Task AccessResourceViaAsyncSynchronization(SemaphoreSlim asyncLock, Int32 operation) {
      // Execute whatever code you want here...
      Console.WriteLine("ThreadID={0}, OpID={1}, await for {2} access",
         Environment.CurrentManagedThreadId, operation, "exclusive");
      await asyncLock.WaitAsync();     // Request exclusive access to a resource via its lock
      // When we get here, we know that no other thread his accessing the resource
      // Access the resource (exclusively)...
      Console.WriteLine("ThreadID={0}, OpID={1}, got access at {2}",
         Environment.CurrentManagedThreadId, operation, DateTime.Now.ToLongTimeString());
      Thread.Sleep(5000);

      // When done accessing resource, relinquish lock so other code can access the resource
      asyncLock.Release();

      // Execute whatever code you want here...
   }

   private static async Task AccessResourceViaAsyncSynchronization(SemaphoreSlim asyncLock) {
      // Execute whatever code you want here...

      await asyncLock.WaitAsync();     // Request exclusive access to a resource via its lock
      // When we get here, we know that no other thread his accessing the resource
      // Access the resource (exclusively)...

      // When done accessing resource, relinquish lock so other code can access the resource
      asyncLock.Release();

      // Execute whatever code you want here...
   }

   private static async Task AccessResourceViaAsyncSynchronization(AsyncOneManyLock asyncLock) {
      // Execute whatever code you want here...

      // Pass OneManyMode.Exclusive or OneManyMode.Shared depending on the concurrent access you need
      await asyncLock.WaitAsync(OneManyMode.Shared); // Request shared access to a resource via its lock
      // When we get here, no threads are writing to the resource; other threads may be reading
      // Read from the resource...

      // When done accessing resource, relinquish lock so other code can access the resource
      asyncLock.Release();

      // Execute whatever code you want here...
   }

   private static void ConcurrentExclusiveSchedulerDemo() {
      var cesp = new ConcurrentExclusiveSchedulerPair();
      var tfExclusive = new TaskFactory(cesp.ExclusiveScheduler);
      var tfConcurrent = new TaskFactory(cesp.ConcurrentScheduler);

      List<Task> tasks = new List<Task>();
      for (Int32 operation = 0; operation < 5; operation++) {
         var capturedOp = operation;
         var exclusive = operation < 2; 
         Task t = (exclusive ? tfExclusive : tfConcurrent).StartNew(() => {
            Console.WriteLine("ThreadID={0}, OpID={1}, {2} access",
               Environment.CurrentManagedThreadId, capturedOp, exclusive ? "exclusive" : "concurrent");            
            Thread.Sleep(5000);
         });

         tasks.Add(t);
         Thread.Sleep(200);
      }
      Task.WaitAll(tasks.ToArray());
      Console.WriteLine("All operations done");
      Console.ReadLine();
}


   private static void OneManyDemo() {
      var asyncLock = new AsyncOneManyLock();
      List<Task> tasks = new List<Task>();
      for (Int32 x = 0; x < 5; x++) {
         var y = x;

         tasks.Add(Task.Run(async () => {
            var mode = (y < 3) ? OneManyMode.Shared : OneManyMode.Exclusive;
            Console.WriteLine("ThreadID={0}, OpID={1}, await for {2} access",
               Environment.CurrentManagedThreadId, y, mode);
            var t = asyncLock.WaitAsync(mode);
            await t;
            Console.WriteLine("ThreadID={0}, OpID={1}, got access at {2}",
               Environment.CurrentManagedThreadId, y, DateTime.Now.ToLongTimeString());
            Thread.Sleep(5000);
            asyncLock.Release();
         }));
         Thread.Sleep(200);
      }
      Task.WaitAll(tasks.ToArray());
      Console.WriteLine("All operations done");
      Console.ReadLine();
   }

   /// <summary>
   /// Indicates if the OneManyLock should be acquired for exclusive or shared access.
   /// </summary>
   public enum OneManyMode {
      /// <summary>
      /// Indicates that exclusive access is required.
      /// </summary>
      Exclusive,

      /// <summary>
      /// Indicates that shared access is required.
      /// </summary>
      Shared
   }


   ///////////////////////////////////////////////////////////////////////////////


   /// <summary>
   /// This class implements a reader/writer lock that never blocks any threads.
   /// To use, await the result of AccessAsync and, after manipulating shared state,
   /// call Release.
   /// </summary>
   public sealed class AsyncOneManyLock {
      #region Lock code
      private SpinLock m_lock = new SpinLock(true);   // Don't use readonly with a SpinLock
      private void Lock() { Boolean taken = false; m_lock.Enter(ref taken); }
      private void Unlock() { m_lock.Exit(); }
      #endregion

      #region Lock state and helper methods
      private Int32 m_state = 0;
      private Boolean IsFree { get { return m_state == 0; } }
      private Boolean IsOwnedByWriter { get { return m_state == -1; } }
      private Boolean IsOwnedByReaders { get { return m_state > 0; } }
      private Int32 AddReaders(Int32 count) { return m_state += count; }
      private Int32 SubtractReader() { return --m_state; }
      private void MakeWriter() { m_state = -1; }
      private void MakeFree() { m_state = 0; }
      #endregion

      // For the no-contention case to improve performance and reduce memory consumption
      private readonly Task m_noContentionAccessGranter;

      // Each waiting writers wakes up via their own TaskCompletionSource queued here
      private readonly Queue<TaskCompletionSource<Object>> m_qWaitingWriters =
         new Queue<TaskCompletionSource<Object>>();

      // All waiting readers wake up by signaling a single TaskCompletionSource
      private TaskCompletionSource<Object> m_waitingReadersSignal =
         new TaskCompletionSource<Object>();
      private Int32 m_numWaitingReaders = 0;

      /// <summary>Constructs an AsyncOneManyLock object.</summary>
      public AsyncOneManyLock() {
         m_noContentionAccessGranter = Task.FromResult<Object>(null);
      }

      /// <summary>
      /// Asynchronously requests access to the state protected by this AsyncOneManyLock.
      /// </summary>
      /// <param name="mode">Specifies whether you want exclusive (write) access or shared (read) access.</param>
      /// <returns>A Task to await.</returns>
      public Task WaitAsync(OneManyMode mode) {
         Task accressGranter = m_noContentionAccessGranter; // Assume no contention

         Lock();
         switch (mode) {
            case OneManyMode.Exclusive:
               if (IsFree) {
                  MakeWriter();  // No contention
               } else {
                  // Contention: Queue new writer task & return it so writer waits
                  var tcs = new TaskCompletionSource<Object>();
                  m_qWaitingWriters.Enqueue(tcs);
                  accressGranter = tcs.Task;
               }
               break;

            case OneManyMode.Shared:
               if (IsFree || (IsOwnedByReaders && m_qWaitingWriters.Count == 0)) {
                  AddReaders(1); // No contention
               } else { // Contention
                  // Contention: Increment waiting readers & return reader task so reader waits
                  m_numWaitingReaders++;
                  accressGranter = m_waitingReadersSignal.Task.ContinueWith(t => t.Result);
               }
               break;
         }
         Unlock();

         return accressGranter;
      }

      /// <summary>
      /// Releases the AsyncOneManyLock allowing other code to acquire it
      /// </summary>
      public void Release() {
         TaskCompletionSource<Object> accessGranter = null;   // Assume no code is released

         Lock();
         if (IsOwnedByWriter) MakeFree(); // The writer left
         else SubtractReader();           // A reader left

         if (IsFree) {
            // If free, wake 1 waiting writer or all waiting readers
            if (m_qWaitingWriters.Count > 0) {
               MakeWriter();
               accessGranter = m_qWaitingWriters.Dequeue();
            } else if (m_numWaitingReaders > 0) {
               AddReaders(m_numWaitingReaders);
               m_numWaitingReaders = 0;
               accessGranter = m_waitingReadersSignal;

               // Create a new TCS for future readers that need to wait
               m_waitingReadersSignal = new TaskCompletionSource<Object>();
            }
         }
         Unlock();

         // Wake the writer/reader outside the lock to reduce
         // chance of contention improving performance
         if (accessGranter != null) accessGranter.SetResult(null);
      }
   }
}

internal static class BlockingCollectionDemo {
   public static void Go() {
      var bl = new BlockingCollection<Int32>(new ConcurrentQueue<Int32>());

      // A thread pool thread will do the consuming
      ThreadPool.QueueUserWorkItem(ConsumeItems, bl);

      // Add 5 items to the collection
      for (Int32 item = 0; item < 5; item++) {
         Console.WriteLine("Producing: " + item);
         bl.Add(item);
      }

      // Tell the consuming thread(s) that no more items will be added to the collection
      bl.CompleteAdding();

      Console.ReadLine();  // For testing purposes
   }

   private static void ConsumeItems(Object o) {
      var bl = (BlockingCollection<Int32>)o;

      // Block until an item shows up, then process it
      foreach (var item in bl.GetConsumingEnumerable()) {
         Console.WriteLine("Consuming: " + item);
      }

      // The collection is empty and no more items are going into it
      Console.WriteLine("All items have been consumed");
   }
}
