using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelProgramming.Playground.Extensions;
using ParallelProgramming.Playground.Objects;

namespace ParallelProgramming.Playground
{
    [TestClass]
    public class Chapter3
    {
        private const int SEARCHMAX = 1000000;

        [TestMethod]
        public void Invoke_Simple()
        {
            Parallel.Invoke(DoLeft, DoRight);
        }

        [TestMethod]
        public void Basic_CreateTasks()
        {
            var task1 = Task.Factory.StartNew(DoLeft);
            var task2 = Task.Factory.StartNew(DoRight);

            //waits for all task to be done
            Task.WaitAll(task1, task2);
        }

        [TestMethod]
        public void Cancellation()
        {
            var cancelTokenSource = new CancellationTokenSource(new TimeSpan(0, 0, 0, 0, 3));

            Task.WaitAll(Task.Factory.StartNew(() =>
                {
                    for (var i = 0; i < 1000000; i++)
                    {
                        Console.WriteLine("Some number: '{0}'", i);
                        Thread.Sleep(1);

                        if (cancelTokenSource.Token.IsCancellationRequested)
                        {
                            return;
                        }
                    }
                }, cancelTokenSource.Token));

            Assert.IsTrue(cancelTokenSource.IsCancellationRequested);
        }

        [TestMethod]
        public void Handle_Exceptions()
        {
            const string MSG = "Well this is mine!";
            try
            {
                var task = Task.Factory.StartNew(() =>
                    {
                        throw new RandomExceptionA(MSG);
                    });

                task.Wait();
            }
            catch (AggregateException ax)
            {
                ax.Handle(x =>
                    {
                        if (x is RandomExceptionA)
                        {
                            Console.WriteLine(x.Message);
                            Assert.AreEqual(MSG, x.Message);
                            return true;
                        }
                        Assert.Fail("Should have found my exception");
                        return false; //unhandled exception
                    });
            }
        }

        [TestMethod]
        public void Handle_Flatten_Exceptions()
        {
            const string MSG = "Inner Exception";
            try
            {
                var outterTask = Task.Factory.StartNew(() =>
                    {
                        var innerTask = Task.Factory.StartNew(() =>
                            {
                                throw new RandomExceptionA(MSG);
                            });

                        innerTask.Wait();
                    });

                outterTask.Wait();
            }
            catch (AggregateException ax)
            {
                ax.Flatten().Handle(exception =>
                    {
                        if (exception is RandomExceptionA)
                        {
                            Console.WriteLine(exception.Message);
                            Assert.AreEqual(MSG, exception.Message);
                            return true;
                        }
                        Assert.Fail("Should have found RandomExceptionA");
                        return false;
                    });
            }

        }

        [TestMethod]
        public void WaitForAGivenTask()
        {
            var tasks = new[]
                {
                    Task.Factory.StartNew(DoLeft),
                    Task.Factory.StartNew(DoRight),
                    Task.Factory.StartNew(DoCenter)
                };

            var allTasks = tasks;

            while (tasks.Length > 0)
            {
                var taskIndex = Task.WaitAny(tasks);
                tasks = tasks.Where(t => t != tasks[taskIndex]).ToArray();
            }

            //observe any exceptions that might have occured
            try
            {
                Task.WaitAll(allTasks);
            }
            catch (AggregateException ax)
            {
                ax.Flatten().Handle(x =>
                    {
                        if (x is RandomExceptionB)
                        {
                            Console.WriteLine(x.Message);
                            return true;
                        }
                        return false;
                    });
            }
        }

        [TestMethod, Ignore]
        public void SpeculativeExecution()
        {
            SpeculativeInvoke(42, SearchLeft, SearchRight, SearchCenter);
        }

        [TestMethod]
        [Description("Variables captured by closures, [args => body]")]
        public void VariablesCapturedByClosures()
        {
            const int TASKNUM = 4;

            IList<Task> buggyTasks = new List<Task>(TASKNUM);

            Console.WriteLine("This set of tasks has a bug due to scope because of closure capture");
            for (var i = 0; i < TASKNUM; i++)
            {
                buggyTasks.Add(Task.Factory.StartNew(() =>
                    {
                        Console.WriteLine(i); //scope was considered here and i will always be 4
                    }));
            }

            Task.WaitAll(buggyTasks.ToArray()); //to group the displays appropriatly

            Console.WriteLine("\n\n\n This used a temerary variable to fix the bug");

            for (var i = 0; i < TASKNUM; i++)
            {
                var temp = i;
                Task.Factory.StartNew(() => Console.WriteLine(temp));
            }  
        }
        
        #region Helper Methods

        private static void DoLeft()
        {
            Console.WriteLine("Left Task Executed. {0}", DateTime.Now);
        }

        private static void DoRight()
        {
            Console.WriteLine("Right Task Executed. {0}", DateTime.Now);
        }

        private static void DoCenter()
        {
            Console.WriteLine("Center Task Executed. {0}", DateTime.Now);
            throw new RandomExceptionB("\n\n\nCenter task blew up, EXPLODIFICATE!!!!!!!!\n\n\n");
        }

        private static int SearchLeft(int num, CancellationToken token)
        {
            var result = 0;

            var numbers = RandomNumbers();
            result = numbers.FirstOrDefault(n => n == num);

            return result;
        }

        private static int SearchRight(int num, CancellationToken token)
        {
            var result = 0;

            var numbers = Enumerable.Range(0, SEARCHMAX);
            result = numbers.FirstOrDefault(n => n == num);

            return result;
        }

        private static int SearchCenter(int num, CancellationToken token)
        {
            var result = 0;
            
            var numbers = RandomNumbers();
            result = numbers.FirstOrDefault(n => n == num);
            
            return result;
        }

        private static void SpeculativeInvoke(int searchNumber, params Func<int, CancellationToken, int>[] actions)
        {
            var cts = new CancellationTokenSource();
            var token = cts.Token;

            var tasks = actions.Select(a => Task.Factory.StartNew(() => a(searchNumber, token), token)).ToArray();

            Task.WaitAny(tasks);
            
            //cancel all of the slower tasks
            cts.Cancel();

            //wait for cancellations and observe exceptions
            try
            {
                Task.WaitAll(tasks);
            }
            catch (AggregateException ax)
            {
                //filter out cancelation exceptions
                ax.Flatten().Handle(x => x is OperationCanceledException);
            }
            finally
            {
                if (cts != null)
                {
                    cts.Dispose();
                }
            }
        }

        private static IEnumerable<int> RandomNumbers()
        {
            return Enumerable.Range(0, SEARCHMAX).Shuffle();
        } 

        #endregion Helper Methods
    }
}
