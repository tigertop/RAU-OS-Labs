using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Threading.Tasks;

namespace Chapter15
{
    [TestClass]
    public class MagicOrderingTest
    {
        [TestMethod]
        public void TasksCompleteInOrder()
        {
            var tardis = new TimeMachine();
            var task1 = tardis.ScheduleSuccess(1, "t1");
            var task2 = tardis.ScheduleSuccess(2, "t2");
            var task3 = tardis.ScheduleSuccess(3, "t3");

            var tasksOutOfOrder = new[] { task2, task3, task1 };

            tardis.ExecuteInContext(advancer =>
            {
                var inOrder = tasksOutOfOrder.InCompletionOrder().ToList();
                advancer.AdvanceTo(3);
                // Could use CollectionAssert here, but this is simpler...
                Assert.AreEqual("t1", inOrder[0].Result);
                Assert.AreEqual("t2", inOrder[1].Result);
                Assert.AreEqual("t3", inOrder[2].Result);
            });
        }

        [TestMethod]
        public void TasksCompleteWhenInputsComplete()
        {
            var tardis = new TimeMachine();
            var task1 = tardis.ScheduleSuccess(1, "t1");
            var task2 = tardis.ScheduleSuccess(2, "t2");
            var task3 = tardis.ScheduleSuccess(3, "t3");

            var tasksOutOfOrder = new[] { task2, task3, task1 };

            tardis.ExecuteInContext(advancer =>
            {
                var inOrder = tasksOutOfOrder.InCompletionOrder().ToList();

                Assert.AreEqual(TaskStatus.WaitingForActivation, inOrder[0].Status);
                Assert.AreEqual(TaskStatus.WaitingForActivation, inOrder[1].Status);
                Assert.AreEqual(TaskStatus.WaitingForActivation, inOrder[2].Status);

                advancer.Advance();
                Assert.AreEqual(TaskStatus.RanToCompletion, inOrder[0].Status);
                Assert.AreEqual(TaskStatus.WaitingForActivation, inOrder[1].Status);
                Assert.AreEqual(TaskStatus.WaitingForActivation, inOrder[2].Status);

                advancer.Advance();
                Assert.AreEqual(TaskStatus.RanToCompletion, inOrder[1].Status);
                Assert.AreEqual(TaskStatus.WaitingForActivation, inOrder[2].Status);

                advancer.Advance();
                Assert.AreEqual(TaskStatus.RanToCompletion, inOrder[2].Status);
            });
        }

        [TestMethod]
        public void MixedResults()
        {
            string message = "message";

            var tardis = new TimeMachine();
            var task1 = tardis.ScheduleSuccess(1, "t1");
            var task2 = tardis.ScheduleFault<string>(2, new Exception(message));
            var task3 = tardis.ScheduleCancellation<string>(3);

            var tasksOutOfOrder = new[] { task2, task3, task1 };

            tardis.ExecuteInContext(advancer =>
            {
                var inOrder = tasksOutOfOrder.InCompletionOrder().ToList();
                advancer.AdvanceTo(3);
                // Could use CollectionAssert here, but this is simpler...
                Assert.AreEqual(TaskStatus.RanToCompletion, inOrder[0].Status);
                Assert.AreEqual(TaskStatus.Faulted, inOrder[1].Status);
                Assert.AreEqual(message, inOrder[1].Exception.InnerExceptions[0].Message);

                Assert.AreEqual(TaskStatus.Canceled, inOrder[2].Status);
            });
        }
    }
}
