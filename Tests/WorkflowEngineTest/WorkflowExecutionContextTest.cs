using System;
using System.IO;
using System.Threading;
using Yaw.Workflow.ComponentModel;
using Yaw.Workflow.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Yaw.Core.Utils.Threading;

namespace Yaw.Tests.WorkflowEngineTest
{
    [TestClass]
    public class WorkflowExecutionContextTest
    {
        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void ConstructorTest()
        {
            var scheme = new WorkflowScheme();
            var target = new WorkflowExecutionContext(scheme);
            Assert.AreEqual(scheme, target.Scheme);
            Assert.IsTrue(target.Tracking);
            Assert.AreEqual(ActivityPriority.Default, target.Priority);
        }

        [TestMethod]
        public void SetWorkflowInstanceTest()
        {
            var target = new WorkflowExecutionContext_Accessor(new WorkflowScheme());
            var wi = new WorkflowInstance(
                Guid.NewGuid(), new WorkflowRuntime(), new WorkflowExecutionContext(new WorkflowScheme()));
            target.SetWorkflowInstance(wi);

            Assert.AreEqual(wi, target._workflowInstance);
        }

        #region Управление процессом выполнения

        [TestMethod]
        [ExpectedException(typeof(ActivityExecutionInterruptException))]
        public void ActivityExecutingTest()
        {
            var target = new WorkflowExecutionContext_Accessor(new WorkflowScheme());
            target.Taketh__toggleActivity(new Activity());
            target.ActivityExecuting(new Activity());
        }

        [TestMethod]
        public void ActivityExecutingTest2()
        {
            var target = new WorkflowExecutionContext_Accessor(new WorkflowScheme());
            target.ActivityExecuting(new Activity {Name = "a"});
            Assert.AreEqual(1, target._executingActivitiesStack.Count);
            Assert.AreEqual("a", target._executingActivitiesStack.Peek());
        }

        [TestMethod]
        public void ActivityExecutedTest()
        {
            var target = new WorkflowExecutionContext_Accessor(new WorkflowScheme()) {Tracking = false};
            target._executingActivitiesStack.Add("a");
            target.ActivityExecuted(new Activity {Name = "a"});
            Assert.AreEqual(0, target._executingActivitiesStack.Count);
        }

        [TestMethod]
        public void ToggleExecutionToActivityTest()
        {
            var target = new WorkflowExecutionContext_Accessor(new WorkflowScheme()) { Tracking = false };
            ThreadUtils.StartBackgroundThread(target.InterruptExecutionThread);
            target.ToggleExecutionToActivity(new Activity());

            Assert.IsTrue(target._interruptExecutionEvent.WaitOne(200));
        }

        [TestMethod]
        public void ToggleExecutionToActivityTest2()
        {
            var target = new WorkflowExecutionContext_Accessor(new WorkflowScheme()) { Tracking = false };
            target.ToggleExecutionToActivity(new Activity {Priority = ActivityPriority.Lowest});
            Assert.IsFalse(target._interruptExecutionEvent.WaitOne(0));
        }

        [TestMethod]
        public void ResetInterruptTest()
        {
            var target = new WorkflowExecutionContext_Accessor(new WorkflowScheme());
            target.ResetInterrupt();
            Assert.IsNull(target.Giveth__toggleActivity());
            Assert.IsFalse(target._interruptExecutionEvent.WaitOne(0));
        }

        /// <summary>
        /// Тест прерывания выполнения
        /// </summary>
        /// <remarks>Тест проверяет, что при прерывани выполнения стек действий будет
        /// правильно разобран: с вершины стека должны быть сняты все действия до тех пор, 
        /// пока на вершине не останется действие, которое является родителем для действия-прерывания,
        /// или пока стек не будет полностью разобран</remarks>
        [TestMethod]
        public void ActivityExecutionInterruptionTest()
        {
            var target = new WorkflowExecutionContext_Accessor(new WorkflowScheme());
            var t = new Activity { Name = "a", Parent = new Activity { Name = "1" } };

            ActivityExecutionInterruptionTestHelper(target, t);
            Assert.AreEqual(1, target._executingActivitiesStack.Count);
            Assert.AreEqual("1", target._executingActivitiesStack.Peek());
        }

        [TestMethod]
        public void ActivityExecutionInterruptionTest2()
        {
            var target = new WorkflowExecutionContext_Accessor(new WorkflowScheme());
            var t = new Activity {Name = "a"};

            ActivityExecutionInterruptionTestHelper(target, t);
            Assert.AreEqual(0, target._executingActivitiesStack.Count);
        }

        private static void ActivityExecutionInterruptionTestHelper(
            WorkflowExecutionContext_Accessor target, Activity toggledActivity)
        {
            target._executingActivitiesStack.Push("1");
            target._executingActivitiesStack.Push("2");
            target._executingActivitiesStack.Push("3");
            target._executingActivitiesStack.Push("4");
            target._executingActivitiesStack.Push("5");

            target.ToggleExecutionToActivity(toggledActivity);
            target.Taketh__toggleActivity(toggledActivity);

            var iex = new ActivityExecutionInterruptException(new Activity(), null);
            while (true)
            {
                try
                {
                    target.ActivityExecutionInterrupted(iex);
                }
                catch (ActivityExecutionInterruptException ex)
                {
                    Assert.AreEqual(iex, ex);
                    try
                    {
                        var a = target.GetToggledActivity(ex);
                        Assert.AreEqual(toggledActivity, a);
                        break;
                    }
                    catch (ActivityExecutionInterruptException ex2)
                    {
                        Assert.AreEqual(iex, ex2);
                    }
                }
            }
        }

        [TestMethod]
        public void GetTrackingBeforeExecuteActivityTest()
        {
            var a0 = new Activity { Tracking = true };
            var a1 = new Activity { Tracking = false, Parent = a0 };
            var a2 = new Activity { Tracking = true, Parent = a1 };
            var a3 = new Activity { Tracking = true, Parent = a2 };

            var res = WorkflowExecutionContext_Accessor.GetTrackingBeforeExecuteActivity(a3);
            Assert.IsFalse(res);
        }

        [TestMethod]
        public void GetTrackingBeforeExecuteActivityTest2()
        {
            var a0 = new Activity { Tracking = false };

            var res = WorkflowExecutionContext_Accessor.GetTrackingBeforeExecuteActivity(a0);
            Assert.IsTrue(res); // т.е. перед началом выполнения действия a0 Tracking будет = true у контекста
        }

        #endregion

        #region Отслеживание стека выполнения действий

        [TestMethod]
        public void StartActivityExecutionTrackingTest()
        {
            var target = new WorkflowExecutionContext_Accessor(new WorkflowScheme());
            var a = new Activity {Name = "a"};

            var activityExecutionStartingDone = false;
            target.add_ActivityExecutionStarting(
                (s, e) =>
                    {
                        if (!activityExecutionStartingDone && e.Activity == a)
                        {
                            activityExecutionStartingDone = true;
                            return;
                        }

                        Assert.Fail("Неожиданное событие ActivityExecutionStarting");
                    });

            target.Tracking = false;
            target.StartActivityExecutionTracking(a);
            Assert.AreEqual(1, target._executingActivitiesStack.Count);
            Assert.AreEqual("a", target._executingActivitiesStack.Peek());
        }

        [TestMethod]
        public void StartActivityExecutionTrackingTest2()
        {
            var target = new WorkflowExecutionContext_Accessor(new WorkflowScheme());
            var a = new Activity { Name = "a" };

            var executionContextChangedDone = false;
            target.add_ExecutionContextChanged(
                (s, e) =>
                {
                    if (!executionContextChangedDone && e.Activity == a)
                    {
                        executionContextChangedDone = true;
                        return;
                    }

                    Assert.Fail("Неожиданное событие ExecutionContextChanged");
                });

            target.Tracking = true;
            target._restoring = false;
            target.StartActivityExecutionTracking(a);
            Assert.AreEqual(1, target._trackingActivitiesStack.Count);
            Assert.AreEqual("a", target._trackingActivitiesStack.Peek());
        }

        [TestMethod]
        public void StartActivityExecutionTrackingTest3()
        {
            var target = new WorkflowExecutionContext_Accessor(new WorkflowScheme());
            var a1 = new Activity { Name = "a1" };
            var a2 = new Activity { Name = "a2" };

            target.Tracking = true;
            target.StartActivityExecutionTracking(a1);
            target.StartActivityExecutionTracking(a2);

            var waitEvent = false;
            target.add_ExecutionContextChanged(
                (s, e) =>
                {
                    if (!waitEvent)
                        Assert.Fail("Неожиданное событие ExecutionContextChanged");
                });
            target._restoring = true;

            target.StartActivityExecutionTracking(a1);
            Assert.AreEqual(1, target._restoredActivitiesCount);
            Assert.IsTrue(target._restoring);

            target.StartActivityExecutionTracking(a2);
            Assert.AreEqual(2, target._restoredActivitiesCount);
            Assert.IsFalse(target._restoring);

            waitEvent = true;
            target.StartActivityExecutionTracking(a1);
            Assert.AreEqual(3, target._trackingActivitiesStack.Count);
        }

        [TestMethod]
        public void EndActivityExecutionTrackingTest()
        {
            var target = new WorkflowExecutionContext_Accessor(new WorkflowScheme());
            var a = new Activity { Name = "a" };

            var eventDone = false;
            target.add_ActivityExecutionFinished(
                (s, e) =>
                {
                    if (!eventDone && e.Activity == a)
                    {
                        eventDone = true;
                        return;
                    }

                    Assert.Fail("Неожиданное событие ActivityExecutionFinished");
                });

            target.Tracking = false;
            target._restoring = false;
            target._executingActivitiesStack.Push("a");
            target.EndActivityExecutionTracking(a);
            Assert.IsTrue(eventDone);
            Assert.AreEqual(0, target._executingActivitiesStack.Count);
        }

        [TestMethod]
        public void EndActivityExecutionTrackingTest2()
        {
            var target = new WorkflowExecutionContext_Accessor(new WorkflowScheme());
            var a = new Activity { Name = "a" };

            var eventDone = false;
            target.add_ExecutionContextChanged(
                (s, e) =>
                {
                    if (!eventDone && e.Activity == a)
                    {
                        eventDone = true;
                        return;
                    }

                    Assert.Fail("Неожиданное событие ExecutionContextChanged");
                });

            target.Tracking = true;
            target._executingActivitiesStack.Push("a");
            target._trackingActivitiesStack.Push("a");
            target.EndActivityExecutionTracking(a);
            Assert.IsTrue(eventDone);
            Assert.AreEqual(0, target._trackingActivitiesStack.Count);
        }

        [TestMethod]
        public void EndActivityExecutionTrackingTest3()
        {
            var target = new WorkflowExecutionContext_Accessor(new WorkflowScheme());
            var a = new Activity { Name = "a" };

            target.add_ExecutionContextChanged((s, e) => Assert.Fail("Неожиданное событие ExecutionContextChanged"));

            target.Tracking = true;
            target._restoring = true;
            target._executingActivitiesStack.Push("a");
            target._trackingActivitiesStack.Push("a");
            target.EndActivityExecutionTracking(a);
            Assert.AreEqual(1, target._trackingActivitiesStack.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void SafePopFromStackTest()
        {
            var target = new WorkflowExecutionContext_Accessor(new WorkflowScheme());
            var a = new Activity { Name = "a" };
            WorkflowExecutionContext_Accessor.SafePopFromStack(
                target._executingActivitiesStack, a);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void SafePopFromStackTest2()
        {
            var target = new WorkflowExecutionContext_Accessor(new WorkflowScheme());
            var a = new Activity { Name = "a" };
            target._executingActivitiesStack.Add("?");
            WorkflowExecutionContext_Accessor.SafePopFromStack(
                target._executingActivitiesStack, a);
        }

        [TestMethod]
        public void SafePopFromStackTest3()
        {
            var target = new WorkflowExecutionContext_Accessor(new WorkflowScheme());
            var a = new Activity { Name = "a" };
            target._executingActivitiesStack.Add("a");
            WorkflowExecutionContext_Accessor.SafePopFromStack(
                target._executingActivitiesStack, a);

            Assert.AreEqual(0, target._executingActivitiesStack.Count);
        }

        [TestMethod]
        public void ExecutingActivitiesStackPopTest()
        {
            var target = new WorkflowExecutionContext_Accessor(new WorkflowScheme());
            
            target._executingActivitiesStack.Add("a");
            target._trackingActivitiesStack.Add("a");
            target.ExecutingActivitiesStackPop();

            Assert.AreEqual(0, target._executingActivitiesStack.Count);
            Assert.AreEqual(0, target._trackingActivitiesStack.Count);

            target._executingActivitiesStack.Add("a");
            target._trackingActivitiesStack.Add("b");
            target.ExecutingActivitiesStackPop();

            Assert.AreEqual(0, target._executingActivitiesStack.Count);
            Assert.AreEqual(1, target._trackingActivitiesStack.Count);
        }

        [TestMethod]
        public void CurrentExecutingActivityTest()
        {
            var scheme = new WorkflowScheme();
            var a = new Activity {Name = "a"};
            scheme.Activities.Add(a);
            var target = new WorkflowExecutionContext_Accessor(scheme);

            Assert.IsNull(target.CurrentExecutingActivity);

            target._executingActivitiesStack.Add("a");
            Assert.AreEqual(a, target.CurrentExecutingActivity);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void GetActivityNameToRestoreTest()
        {
            var target = new WorkflowExecutionContext_Accessor(new WorkflowScheme()) {_restoring = false};
            target.GetActivityNameToRestore();
        }

        [TestMethod]
        public void GetActivityNameToRestoreTest2()
        {
            var target = new WorkflowExecutionContext_Accessor(new WorkflowScheme())
                             {
                                 _restoring = true,
                                 _restoredActivitiesCount = 0
                             };
            target._trackingActivitiesStack.Add("a");
            Assert.AreEqual("a", target.GetActivityNameToRestore());
        }

        #endregion

        #region IWaitController Members

        [TestMethod]
        public void SleepTest()
        {
            var target = new WorkflowExecutionContext_Accessor(new WorkflowScheme());

            var interrupDone = false;
            ThreadPool.QueueUserWorkItem(
                s =>
                    {
                        try
                        {
                            target.Sleep(TimeSpan.FromSeconds(5));
                        }
                        catch (ActivityExecutionInterruptException)
                        {
                            interrupDone = true;
                        }
                    });

            target._interruptExecutionEvent.Set();
            Thread.Sleep(100);
            Assert.IsTrue(interrupDone);
        }


        [TestMethod]
        public void SleepTest2()
        {
            var target = new WorkflowExecutionContext_Accessor(new WorkflowScheme());

            var interrupDone = false;
            ThreadPool.QueueUserWorkItem(
                s =>
                {
                    try
                    {
                        target.Sleep(5000);
                    }
                    catch (ActivityExecutionInterruptException)
                    {
                        interrupDone = true;
                    }
                });

            target._interruptExecutionEvent.Set();
            Thread.Sleep(100);
            Assert.IsTrue(interrupDone);
        }

        [TestMethod]
        public void WaitAnyTest()
        {
            var target = new WorkflowExecutionContext_Accessor(new WorkflowScheme());

            var interrupDone = false;
            ThreadPool.QueueUserWorkItem(
                s =>
                {
                    try
                    {
                        var e = new AutoResetEvent(false);
                        target.WaitAny(new[] {e});
                    }
                    catch (ActivityExecutionInterruptException)
                    {
                        interrupDone = true;
                    }
                });

            target._interruptExecutionEvent.Set();
            Thread.Sleep(100);
            Assert.IsTrue(interrupDone);
        }


        [TestMethod]
        public void WaitAnyTest2()
        {
            var target = new WorkflowExecutionContext_Accessor(new WorkflowScheme());

            var interrupDone = false;
            ThreadPool.QueueUserWorkItem(
                s =>
                {
                    try
                    {
                        var e = new AutoResetEvent(false);
                        target.WaitAny(new[] { e }, TimeSpan.FromSeconds(5));
                    }
                    catch (ActivityExecutionInterruptException)
                    {
                        interrupDone = true;
                    }
                });

            target._interruptExecutionEvent.Set();
            Thread.Sleep(100);
            Assert.IsTrue(interrupDone);
        }

        [TestMethod]
        public void WaitAnyTest3()
        {
            var target = new WorkflowExecutionContext_Accessor(new WorkflowScheme());

            var interrupDone = false;
            ThreadPool.QueueUserWorkItem(
                s =>
                {
                    try
                    {
                        var e = new AutoResetEvent(false);
                        target.WaitAny(new[] { e }, 5000);
                    }
                    catch (ActivityExecutionInterruptException)
                    {
                        interrupDone = true;
                    }
                });

            target._interruptExecutionEvent.Set();
            Thread.Sleep(100);
            Assert.IsTrue(interrupDone);
        }

        #endregion

        #region Подписка на события

        public event EventHandler TestEvent;

        [TestMethod]
        public void SubscribeUnsubscribeToEventTest()
        {
            var context = new WorkflowExecutionContext(new WorkflowScheme());
            new WorkflowInstance_Accessor(Guid.NewGuid(), new WorkflowRuntime(), context);

            var target = new WorkflowExecutionContext_Accessor(new PrivateObject(context));

            var eh = new EventHolder(GetType().GetEvent("TestEvent"), this);
            var a = new Activity();
            target.SubscribeToEvent(eh, a, EventHandlingType.Sync);
            target.SubscribeToEvent(eh, a, EventHandlingType.Sync);

            Assert.IsNotNull(TestEvent);
            TestEvent(this, EventArgs.Empty);
            Activity res;
            target._toggleActivityQueue.TryDequeue(10, out res);
            Assert.AreEqual(a, res);

            target.UnsubscribeFromEvent(eh, a);
            Assert.IsNull(TestEvent);
        }

        #endregion

        #region Сериализация

        [TestMethod]
        public void GetStateTest()
        {
            var scheme = new WorkflowScheme();
            var target = new WorkflowExecutionContext_Accessor(scheme);

            var state = (object[])target.GetState();
            Assert.AreEqual(3, state.Length);
            Assert.AreEqual(scheme, state[0]);
            Assert.AreEqual(target._trackingActivitiesStack, state[1]);
            // элемент state[2] не проверяю, т.к. при обращении к target._eventHandlersDict
            // почему то возникает ошибка приведения типа...
        }

        [TestMethod]
        public void SaveLoadTest()
        {
            var context = new WorkflowExecutionContext(new WorkflowScheme());
            var target = new WorkflowExecutionContext_Accessor(new PrivateObject(context));
            target._trackingActivitiesStack.Push("1");

            using (var stream = new MemoryStream())
            {
                WorkflowExecutionContext.Save(context, stream);

                stream.Position = 0;

                var res = WorkflowExecutionContext.Load(stream);
                var target2 = new WorkflowExecutionContext_Accessor(new PrivateObject(res));

                Assert.AreEqual("1", target2._trackingActivitiesStack.Peek());
                Assert.IsTrue(target2._restoring);
            }
        }

        #endregion
    }
}
