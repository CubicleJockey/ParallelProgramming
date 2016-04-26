using System;
using System.Linq;

namespace ParallelProgramming.Playground.Objects
{
    public class Worker
    {
        public int Id { get; set; }
        public Guid Name { get; set; }
        public int Age { get; set; }

        public long CalculateSalaryBasedOnName()
        {
            var name = Name.ToString();

            return name.Where(Char.IsDigit).Sum(t => Int64.Parse(t.ToString()));
        }

        #region Overridden

        public override string ToString()
        {
            return string.Format("Id: {0}, Name: {1}", Id, Name);
        }

        public override bool Equals(object obj)
        {
            var rhsWorker = obj as Worker;
            if (rhsWorker == null)
            {
                return false;
            }

            if (ReferenceEquals(this, rhsWorker))
            {
                return true;
            }
            return Id == rhsWorker.Id && Name == rhsWorker.Name;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode() + Name.GetHashCode();
        }

        #endregion Overridden
    }
}
