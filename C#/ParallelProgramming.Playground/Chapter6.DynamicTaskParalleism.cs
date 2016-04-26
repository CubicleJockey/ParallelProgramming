using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelProgramming.Playground.Objects;

namespace ParallelProgramming.Playground
{
    /// <summary>
    /// Dynamically added to the work queue as computation is process is known
    /// as Dynamic Task Parallelism.  A simple example of dynamic task parallelism
    /// occurs in cases where the sequential version of an algorithm includes recursion.
    /// </summary>
    [TestClass]
    public class Chapter6
    {
        private static readonly Random _numGenerator = new Random(DateTime.Now.Millisecond);
        private const int MAXNUMBER = 1000000;
        private int[] _RandomNumber;

        #region Setup

        [TestInitialize]
        public void TestInitialize()
        {
            _RandomNumber = new int[MAXNUMBER];
            for (var i = 0; i < MAXNUMBER; i++)
            {
                _RandomNumber[i] = _numGenerator.Next(0, MAXNUMBER);
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _RandomNumber = null;
        }

        #endregion Setup

        [TestMethod]
        public void SequentialTreeWalk()
        {
            const int MAX = 1000000;

            var trunk = PopulaterComplexTree(MAX, 100000, 7500);

            var start = DateTime.Now;

            SequentialWalk(trunk, Console.WriteLine);

            var end = DateTime.Now;

            Console.WriteLine("Sequential Tree Walk : '{0}'", (end - start));
        }

        [TestMethod]
        public void ParallelTreeWalk()
        {
            const int MAX = 1000000;

            var trunk = PopulaterComplexTree(MAX, 100000, 7500);

            var start = DateTime.Now;

            ParallelWalk(trunk, Console.WriteLine);

            var end = DateTime.Now;

            Console.WriteLine("Sequential Tree Walk : '{0}'", (end - start));
        }


        #region Tree HelperMethods

        private static void SequentialWalk<T>(Tree<T> tree, Action<T> action)
        {
            if (tree == null)
            {
                return;
            }
            action(tree.Data);
            SequentialWalk(tree.Left, action);
            SequentialWalk(tree.Right, action);
        }

        private static void ParallelWalk<T>(Tree<T> tree, Action<T> action)
        {
            if (tree == null)
            {
                return;
            }

            var dataTask = Task.Factory.StartNew(() => action(tree.Data));
            var leftRecursion = Task.Factory.StartNew(() => ParallelWalk(tree.Left, action));
            var rightRecursion = Task.Factory.StartNew(() => ParallelWalk(tree.Right, action));

            Task.WaitAll(dataTask, leftRecursion, rightRecursion);
        }

        private static Tree<int> PopulaterComplexTree(int maxRandom, int leftSize, int rightSize)
        {
            var tree = new Tree<int>
                {
                    Data = _numGenerator.Next(0, maxRandom),
                    Left = new Tree<int>(),
                    Right = new Tree<int>()
                };
            SetLeftItems(tree.Left, maxRandom, 0, leftSize);
            SetRightItems(tree.Right, maxRandom, 0, rightSize);
            return tree;
        } 

        private static void SetLeftItems(Tree<int> tree, int maxRandom, int current, int end)
        {
            if (current != end)
            {
                tree.Data = _numGenerator.Next(0, maxRandom);
                tree.Left = new Tree<int>();
                SetLeftItems(tree.Left, maxRandom, ++current, end);
            }
        }

        private static void SetRightItems(Tree<int> tree, int maxRandom, int current, int end)
        {
            if (current != end)
            {
                tree.Data = _numGenerator.Next(0, maxRandom);
                tree.Right = new Tree<int>();
                SetRightItems(tree.Right, maxRandom, ++current, end);
            }
        }

        #endregion HelperMethods
    }
}
