using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Worker.Common
{
    public class AllocatorWorkQueue<T> : IDisposable where T : class
    {
        private Queue<T> workItems;
        private object _lockObject;

        public AllocatorWorkQueue()
        {
            this.workItems = new Queue<T>();
            this._lockObject = new object();
        }

        public bool TryDequeue(out T workItem)
        {
            lock (this._lockObject)
            {
                if (this.workItems.Count == 0)
                {
                    workItem = default(T);
                    return false;
                }

                workItem = this.workItems.Dequeue();
                return true;
            }
        }

        public bool TryEnqueue(T workItem)
        {
            lock (this._lockObject)
            {
                this.workItems.Enqueue(workItem);
                return true;
            }
        }

        public void Dispose()
        {
            this._lockObject = null;
            this.workItems = null;
        }
    }
}
