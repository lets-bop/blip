using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Worker.Common
{
    public class AllocatorWorkItem
    {
    }

    public class AllocatorWorkItem<T> : AllocatorWorkItem
    {
        public int Priority { get; }
        public Func<T> AllocateAction { get; }

        public AllocatorWorkItem(int priority, Func<T> allocateAction)
        {
            this.Priority = priority;
            this.AllocateAction = allocateAction;
        }
    }
}
