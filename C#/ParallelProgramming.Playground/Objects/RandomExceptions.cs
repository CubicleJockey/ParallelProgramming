using System;

namespace ParallelProgramming.Playground.Objects
{
    public class RandomExceptionA : Exception
    {
        public RandomExceptionA(string message) : base(message){}
    }

    public class RandomExceptionB : Exception
    {
        public RandomExceptionB(string message) : base(message){}
    }
}
