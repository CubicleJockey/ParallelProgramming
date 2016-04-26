using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelProgramming.Playground.Objects;

namespace ParallelProgramming.Playground
{
    [TestClass]
    public class Chapter2
    {
        private readonly Random rGenerator = new Random(DateTime.Now.Millisecond);
        private IEnumerable<Worker> _workers;

        #region Setup

        [TestInitialize]
        public void TestInitialize()
        {
            const int MAXWORKERS = 9999999;

            IList<Worker> workers = new List<Worker>(MAXWORKERS);
            var workerIds = Enumerable.Range(0, MAXWORKERS);
            foreach (var id in workerIds)
            {
                workers.Add(new Worker
                    {
                        Id = id,
                        Name = Guid.NewGuid(),
                        Age = rGenerator.Next(18, 150)
                    });
            }

            _workers = workers;
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _workers = null;
        }

        #endregion Setup
        [TestMethod]
        public void SimpleForLoop()
        {
            const int MAX = 1000;
            
            Parallel.For(0, MAX, (i, state) => Console.WriteLine("Number {0} on Thread {1}", i, Thread.CurrentThread.ManagedThreadId));
        }

        [TestMethod]
        public void SimpleForeachLoop()
        {
            const int MAX = 10000;

            var numbers = Enumerable.Range(0, MAX);

            Parallel.ForEach(numbers, (i, state) => Console.WriteLine("Number {0} on Thread {1}", i, Thread.CurrentThread.ManagedThreadId));
        }

        [TestMethod]
        public void ForeachLoop()
        {
            var workers = _workers.Take(1000);
            Parallel.ForEach(workers, worker =>
                {
                    Console.WriteLine("Before {0}", worker.Age);
                    worker.Age++;
                    Console.WriteLine("After {0}", worker.Age);
                });
        }

        [TestMethod]
        public void DiffLinqVsPLinq()
        {
            const long MAX_SALARY = 10000;

            //Setup Non Parallel Version of Query
            var query1 = from worker in _workers
                         where worker.CalculateSalaryBasedOnName() > MAX_SALARY
                         select worker;

            //Setup Parallel Version of Query
            var query2 = from worker in _workers.AsParallel()
                         where worker.CalculateSalaryBasedOnName() > MAX_SALARY
                         select worker;


            #region Execute Non Parallel Query

            var start1 = DateTime.Now;

            query1.ToArray(); //Execute

            var end1 = DateTime.Now;
            
            Console.WriteLine("Non Parallel Time: {0}", end1 - start1);

            #endregion Execute Non Parallel Query
            
            #region Execute Parallel Query

            var start2 = DateTime.Now;

            query2.ToArray(); //Execute

            var end2 = DateTime.Now;

            Console.WriteLine("Parallel Time: {0}", end2 - start2);

            #endregion Execute Parallel Query
        }

        [TestMethod]
        public void ForAll_Extension()
        {
            var start1 = DateTime.Now;
            foreach (var worker in _workers)
            {
                worker.Age++;
            }
            var end1 = DateTime.Now;

            Console.WriteLine("Non Parallel age update took {0}", end1 - start1);

            var start2 = DateTime.Now;

            _workers.AsParallel().ForAll(worker => worker.Age++);

            var end2 = DateTime.Now;

            Console.WriteLine("Parallel Age update took: {0}", end2 - start2); 
        }

        [TestMethod]
        [Description("When the body of a loop writes to a shared variable, you have a loop body dependency")]
        public void LoopBodyDependency()
        {

            const int MAX = 1000;

            var result = 0; //This is the loop body dependency

            Parallel.For(0, MAX, (i, state) =>
                {
                    result += rGenerator.Next(0, MAX);
                });

            Console.WriteLine("Loop Body Dependency [Result = {0}]", result);
        }

        [TestMethod]
        [Description("Break begins an orderly shutdown of the loop processing. Any steps that are running as of the call to BREAK will run to completion.")]
        public void BreakingOutOfLoop()
        {
            var numbers = Enumerable.Range(0, 1000).ToArray();

            #region Regular For loop way

            Console.WriteLine("Regular For loop and break");
            for (var i = 0; i < numbers.Length; i++)
            {
                Console.WriteLine(i);
                if (i == 3)
                {
                    Console.WriteLine("BREAK");
                    break;
                }
            }

            #endregion Regular For loop way


            Console.WriteLine("\n\nParallel for loop with a break");

            Parallel.For(0, numbers.Count(), (i, state) =>
                {
                    Console.WriteLine(i);
                    if (i == 3)
                    {
                        Console.WriteLine("BREAK");
                        state.Break(); //This may have more items run before it fully stops
                    }
                });
        }

        [TestMethod]
        public void BreakingOutOfLoop_LoopResult()
        {
            var numbers = Enumerable.Range(0, 10000).ToArray();

            var loopResult = Parallel.For(0, numbers.Length, (i, state) =>
                {
                    Console.WriteLine(i);
                    if (i == 3)
                    {
                        Console.WriteLine("BREAK");
                        state.Break(); //This may have more items run before it fully stops
                    }
                });

            if (!loopResult.IsCompleted && loopResult.LowestBreakIteration.HasValue)
            {
                Console.WriteLine("Loop encountred a break at {0}", loopResult.LowestBreakIteration.Value);
            }
        }

        [TestMethod]
        [Description("Shutsdown a loop more quickly that break and will probably be used more often than not.")]
        public void StoppingALoop()
        {
            var numbers = Enumerable.Range(0, 10000).ToArray();

            var loopResult = Parallel.For(0, numbers.Length, (i, state) =>
            {
                Console.WriteLine(i);
                if (i == 3)
                {
                    Console.WriteLine("STOP");
                    state.Stop();
                }
            });

            if (!loopResult.IsCompleted)
            {
                //there is no iteraction value for STOP like there is for BREAK
                Console.WriteLine("Loop encountred a stop");
            }
        }

        [TestMethod]
        public void CallingBreakThenStopTogether()
        {
            try
            {
                Parallel.For(0, 100, (i, state) =>
                    {
                        //Will throw an exception
                        state.Break();
                        state.Stop();
                    });
            }
            catch (AggregateException ax)
            {
                Assert.IsTrue(ax.Message.Contains("One or more errors occurred."));

                var innerExceptionType = ax.InnerException.GetType();
                Assert.AreEqual(typeof(InvalidOperationException), innerExceptionType);
                Assert.IsTrue(ax.InnerException.Message.Contains("Stop was called after Break was called."));
                return; //need to know we got here if we don't hit return then fail
            }
            Assert.Fail("Should have verified exceptions");
        }

        [TestMethod]
        public void CallingStopThenBreakTogether()
        {
            try
            {
                Parallel.For(0, 100, (i, state) =>
                {
                    //Will throw an exception
                    state.Stop();
                    state.Break();
                });
            }
            catch (AggregateException ax)
            {
                Assert.IsTrue(ax.Message.Contains("One or more errors occurred."));

                var innerExceptionType = ax.InnerException.GetType();
                Assert.AreEqual(typeof(InvalidOperationException), innerExceptionType);
                Assert.IsTrue(ax.InnerException.Message.Contains("Break was called after Stop was called."));
                return; //need to know we got here if we don't hit return then fail
            }
            Assert.Fail("Should have verified exceptions");
        }

        [TestMethod]
        public void CancellationTokens_Options()
        {
            var CANCELAFTER = new TimeSpan(0, 0, 0, 0, 3);

            var token = new CancellationTokenSource(CANCELAFTER);
            var parallelOptions = new ParallelOptions
                {
                    CancellationToken = token.Token
                };
            
            var numbers = Enumerable.Range(0, 99999);

            try
            {
                Parallel.ForEach(numbers, parallelOptions, (num, state) =>
                    {
                        Console.WriteLine("Made it to {0}", num);
                        if (parallelOptions.CancellationToken.IsCancellationRequested)
                        {
                            Console.WriteLine("Cancellation is occuring.....");
                            state.Stop();
                        }
                    });
            }
            catch (OperationCanceledException ocx)
            {
                Console.WriteLine("Exception Message: {0}", ocx.Message);
            }
        }

        [TestMethod]
        [Description("Small Loop Bodies can have massive overhead, using Partitioner can reduce this overhead.")]
        public void Partitioner_OverheadReduction()
        {
            const int MAX = 1000000;
            const int GROUP = 50000;

            var result = new double[MAX];

            /*
             * Each range will be given a span of 50,000 index values.
             * For 1 million interations, the system will use twenty
             * parallel interations (1000000, 50000). 
             */
            var partitioner = Partitioner.Create(0, MAX, GROUP);

            Parallel.ForEach(partitioner, range =>
                {
                    for (var i = range.Item1; i < range.Item2; i++)
                    {
                        var number = (i*i);
                        
                        //small, equally sized block of work
                        result[i] += number;
                        Console.WriteLine("Calculated Number being added {0}", number);
                    }
                });
        }

        [TestMethod]
        public void ControlDegreeOfParallelism()
        {
            const int MAX = 10000;

            var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 2
                };


            IList<int> threadIds = new List<int>();
            Parallel.For(0, MAX, options, i =>
                {

                    var id = Thread.CurrentThread.ManagedThreadId;
                    Console.WriteLine("Number '{0}' on thread {1}", i, id);
                    threadIds.Add(id);
                });

            //No more than two task should ever run for this.
            //Assert.AreEqual(2, threadIds.Distinct().Count());
        }

        [TestMethod]
        public void ControlDegreeOfParallelism_WithExtensions()
        {
            const int MAX_TASKS = 8;

            var numbers = Enumerable.Range(0, 10000000);

            IList<int> threadIds = new List<int>(MAX_TASKS);
            numbers.AsParallel()
                   .WithDegreeOfParallelism(MAX_TASKS)
                   .ForAll(i =>
                       {
                           var id = Thread.CurrentThread.ManagedThreadId;
                           if (!threadIds.Contains(id))
                           {
                               threadIds.Add(id);
                           }
                       });

            Assert.IsTrue(threadIds.Count > 2);
            Assert.IsTrue(threadIds.Count <= MAX_TASKS);
            Console.WriteLine(threadIds.Count);
        }

        [TestMethod]
        [Description(
            "Task-Local State in a loop body, you must use Task Local State to make calls to non-threadsafe methods")]
        public void TaskLocalState()
        {
            //Random is not thread safe

            const int NUMBEROFSTEPS = 100000;

            var result = new double[NUMBEROFSTEPS];

            var partitioner = Partitioner.Create(0, NUMBEROFSTEPS);

            Parallel.ForEach(partitioner,
                             new ParallelOptions(),
                             () => new Random(DateTime.Now.Millisecond), //local initalize
                             (range, state, random) =>
                                 {
                                     for (var i = range.Item1; i < range.Item2; i++)
                                     {
                                         var value = random.NextDouble();
                                         result[i] = value;
                                         Console.WriteLine("Adding value: '{0}'", value);
                                     }
                                     return random;
                                 },
                             _ => { /*local finally do nothing*/} );

            //NOTE: Random is used for example to get real randoms not psueodo, use
            //http://msdn.microsoft.com/en-us/library/system.security.cryptography.rngcryptoserviceprovider.aspx

        }
    }
}
