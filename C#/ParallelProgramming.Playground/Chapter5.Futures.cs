using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelProgramming.Playground.Objects;

namespace ParallelProgramming.Playground
{
    [TestClass]
    public class Chapter5
    {
        /*
         * Futures Pattern
         *     -Chapter 3 was about asyncronous actions (don't return values)
         *      this chapter is about asyncronous functions which do return values.
         *      
         * .NET also implements a variation of the Futures pattern which is known as the
         * continuation task. A continuation task is a task that automatically starts when
         * other tasks, known as its antecedents, complete.
         */ 


        /// <summary>
        ///  b = F1(a)
        ///  c = F2(a)
        ///  d = F3(c)
        ///  e = F4(b,d)
        ///  return e
        /// 
        /// F1 and F2 both take parameter a, so they can run in parallel, F4 begins when b is ready
        /// </summary>
        [TestMethod]
        public void SimpleFuturesExample()
        {
            const int A = 123456;

            var futureB = Task.Factory.StartNew(() => F1(A));
            var c = F2(A);
            var d = F3(c);
            var e = F4(futureB.Result, d);

            Assert.AreNotEqual(0, e);
            Console.WriteLine("Result '{0}'", e);
        }

        /// <summary>
        ///  b = F1(a)
        ///  c = F2(a)
        ///  d = F3(c)
        ///  e = F4(b,d)
        ///  return e
        /// </summary>
        [TestMethod]
        public void SimpleFuturesExample2()
        {
            const int A = 1234;

            var futureD = Task.Factory.StartNew(() => F3(F2(A)));

            var b = F1(A);
            var e = F4(b, futureD.Result);

            Assert.AreNotEqual(0, e);
            Console.WriteLine("Result '{0}'", e);
        }

        [TestMethod]
        public void FutureWithException()
        {
            const int A = 14;
            
            try
            {
                var futureD = Task.Factory.StartNew(() => F3_Error(F2(A)));
                var b = F1(A);
                var e = F4(b, futureD.Result);
            }
            catch (AggregateException ax)
            {
                ax.Handle(x =>
                    {
                        if (x is RandomExceptionA)
                        {
                            Console.WriteLine(x.Message);
                            return true;
                        }
                        return false;
                    });
                return;
            }
            Assert.Fail("Should have gotten an exception");
        }

        [TestMethod]
        public void ContinuationTasks()
        {
            const int A = 99;

            var futureB = Task.Factory.StartNew(() =>
                {
                    Console.WriteLine("F1 ran at '{0}'", DateTime.Now);
                    return F1(A);
                });
            var futureD = Task.Factory.StartNew(() =>
                {
                    
                    Console.WriteLine("F2/F3 ran at '{0}'", DateTime.Now);
                    return F3(F2(A));
                });

            var futureF = 
                Task.Factory.ContinueWhenAll(new[] { futureB, futureD }, 
                                             tasks => F4(futureB.Result, futureD.Result));

            futureF.ContinueWith(task =>
                {
                    Console.WriteLine("FutureF result continued at '{0}'", DateTime.Now);
                    Console.WriteLine("FutureD = '{0}'", task.Result);
                });
        }

        /// <summary>
        /// b = Func1(a)
        /// d = Func2(c)
        /// e = Func3(b, d)
        /// f = Func4(e)
        /// g = Func5(e)
        /// h = Func6(f, g)
        /// 
        /// Do with the minimum amount of futures.
        /// </summary>
        [TestMethod]
        [Description("Exercise 1 from page 101")]
        public void Exercise1()
        {
            const int A = 100;
            const int C = 9001; //it's over 9000

            var futureB = Task.Factory.StartNew(() => Func1(A));
            var futureD = Task.Factory.StartNew(() => Func2(C));
            
            var futureE = Task.Factory.StartNew(() => Func3(futureB.Result, futureD.Result));

            var futureF = Task.Factory.StartNew(() => Func4(futureE.Result));
            var futureG = Task.Factory.StartNew(() => Func5(futureE.Result));

            var h = Func6(futureF.Result, futureG.Result);

            Console.WriteLine("\n\nFinal Result: '{0}'", h);
        }



        #region HelperMethods

        private static int F1(int i)
        {
            Console.WriteLine("F1 is on thread '{0}'", Thread.CurrentThread.ManagedThreadId);
            return i*42;
        }

        private static int F2(int i)
        {
            Console.WriteLine("F2 is on thread '{0}'", Thread.CurrentThread.ManagedThreadId);
            return i%2;
        }

        private static int F3(int i)
        {
            Console.WriteLine("F3 is on thread '{0}'", Thread.CurrentThread.ManagedThreadId);
            return i*i;
        }

        private static int F3_Error(int i)
        {
            throw new RandomExceptionA("I dunno what happened, ASPLODE!!!");
        }

        private static int F4(int i, int j)
        {
            Console.WriteLine("F4 is on thread '{0}'", Thread.CurrentThread.ManagedThreadId);
            return (i ^ 2) - j;
        }



        #endregion HelperMethods

        #region Exercise Methods

        private static int Func1(int i)
        {
            Console.WriteLine("Func1 is on thread '{0}' at '{1}'", Thread.CurrentThread.ManagedThreadId, DateTime.Now);
            Console.WriteLine("Incoming value: '{0}'", i);
            
            var result = i*i;
            Console.WriteLine("Outgoing value: '{0}'\n", result);
            
            return result;
        }

        private static int Func2(int i)
        {
            Console.WriteLine("Func2 is on thread '{0}' at '{1}'", Thread.CurrentThread.ManagedThreadId, DateTime.Now);
            Console.WriteLine("Incoming value: '{0}'", i);

            var result = i ^ 2;
            Console.WriteLine("Outgoing value: '{0}'\n", result);

            return result;
        }

        private static int Func3(int a, int b)
        {
            Console.WriteLine("Func3 is on thread '{0}' at '{1}'", Thread.CurrentThread.ManagedThreadId, DateTime.Now);
            Console.WriteLine("Incoming values: '{0}' & '{1}'", a, b);

            var result = a + b;
            Console.WriteLine("Outgoing value: '{0}'\n", result);

            return result;
        }

        private static int Func4(int i)
        {
            Console.WriteLine("Func4 is on thread '{0}' at '{1}'", Thread.CurrentThread.ManagedThreadId, DateTime.Now);
            Console.WriteLine("Incoming value: '{0}'", i);

            var result = i - 4;
            Console.WriteLine("Outgoing value: '{0}'\n", result);

            return result;
        }

        private static int Func5(int i)
        {
            Console.WriteLine("Func5 is on thread '{0}' at '{1}'", Thread.CurrentThread.ManagedThreadId, DateTime.Now);

            Console.WriteLine("Incoming value: '{0}'", i);

            var result = i ^ 5;
            Console.WriteLine("Outgoing value: '{0}'\n", result);

            return result;
        }

        private static int Func6(int a, int b)
        {
            Console.WriteLine("Func6 is on thread '{0}' at '{1}'", Thread.CurrentThread.ManagedThreadId, DateTime.Now);
            Console.WriteLine("Incoming values: '{0}' & '{1}'", a, b);

            var result = (a - 2) + (b + 5);
            Console.WriteLine("Outgoing value: '{0}'\n", result);

            return result;
        }

        #endregion Exercise Methods
    }
}
