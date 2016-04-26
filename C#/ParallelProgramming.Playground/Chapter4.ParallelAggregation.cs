using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelProgramming.Playground.Extensions;

namespace ParallelProgramming.Playground
{
    /*
     * Parallel Aggregation Pattern (aka Parallel Reduction Pattern)
     * 
     * lets you use multiple cores to calculate sums and other types
     *  of accumilations that are based on associative operations.
     * 
     */ 

    [TestClass]
    public class Chapter4
    {
        [TestMethod]
        public void PLINQ_AggregateExtension_Simple()
        {
            var numbers = Enumerable.Range(1, 1000000);

            var total = numbers.AsParallel().Aggregate(0, (subtotal, item) => subtotal += item);

            Console.WriteLine(total);
        }

        [TestMethod]
        public void ParallelForEach_Aggregation()
        {
            var numbers = Enumerable.Range(0, 1000000);
            var lockObject = new object();
            var sum = 0;

            Parallel.ForEach(numbers,
                             () => 0, /*local variable initialization*/
                             (num, state, result) =>
                                 {
                                     /*The loop body*/
                                     return result += num;
                                 },
                             localPartialSum =>
                                 {
                                     //Enforce serial access to single, shared result
                                     lock (lockObject)
                                     {
                                         sum += localPartialSum;
                                     }
                                 });

            Assert.AreNotEqual(0, sum);
            Console.WriteLine("Parallel Foreach aggregation result: {0}", sum);
        }

        [TestMethod]
        public void RangeParitionerForAggregation()
        {
            var numbers = Enumerable.Range(0, 1000000).ToArray();
            var lockObject = new object();
            var sum = 0;

            var rangePartitioner = Partitioner.Create(0, numbers.Length);

            Parallel.ForEach(rangePartitioner,
                             () => 0, /*local variable initialization*/
                             (range, state, initialValue) =>
                             {
                                 /*The loop body*/
                                 var partialSum = initialValue;
                                 for (var i = range.Item1; i < range.Item2; i++)
                                 {
                                     partialSum += numbers[i];
                                 }
                                 return partialSum;
                             },
                             localPartialSum =>
                             {
                                 //Enforce serial access to single, shared result
                                 lock (lockObject)
                                 {
                                     sum += localPartialSum;
                                 }
                             });

            Assert.AreNotEqual(0, sum);
            Console.WriteLine("Parallel Foreach aggregation result: {0}", sum);
        }

        [TestMethod, Ignore]
        public void PLINQ_Aggregation_RangeSelection()
        {
            const int MAXRANGE = 9000000;

            var result = ParallelEnumerable.Range(0, MAXRANGE).Aggregate(
                //1 - Create an empty local accumulator object
                //    that includes all task-local state.
                () => new Tuple<IList<int>, RNGCryptoServiceProvider>(new List<int>(), new RNGCryptoServiceProvider()),

                //2- Run simulator and add to accumalator
                (localAccumalator, i) =>
                    {
                        var box = new byte[8];
                        localAccumalator.Item2.GetBytes(box);

                        var sample = (double)(BitConverter.ToUInt64(box, 0) / ulong.MaxValue);
                        if (sample > 0.0 && sample < 1.0)
                        {
                            var average = Simulation(sample, MakeHistogram());

                            localAccumalator.Item1[i] = average;
                        }
                        return localAccumalator;
                    },
                
                //3- combine local results pair-wise
                (localAccumalator, localAccumalator2) => new Tuple<IList<int>, RNGCryptoServiceProvider>(
                                                             CombineResults(localAccumalator.Item1, localAccumalator2.Item1),
                                                             null),

                //4- get finally accumalator
                finalAccumalator => finalAccumalator.Item1
                
                );

            Assert.IsTrue(result.Count > 0);
        }

        #region HelperMethods

        private static IList<int> MakeHistogram()
        {
            var numbers = Enumerable.Range(0, 1000000);
            return numbers.Shuffle().ToList();
        }

        private static int Simulation(double sample, IList<int> data)
        {
           for (var i = 0; i < data.Count; i++)
           {
               data[i] += (int)Math.Log10(sample/data[i]);
           }
           return (int)data.Average(n => n);
        }

        private static IList<int> CombineResults(IEnumerable<int> lhs, IEnumerable<int> rhs)
        {
            var combinedResult = new List<int>();
            
            combinedResult.AddRange(lhs);
            combinedResult.AddRange(rhs);

            return combinedResult;
        } 

        #endregion HelperMethods
    }
}
