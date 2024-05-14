using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Worker.Common
{
    public class AllocatorInstance
    {
        private AllocatorWorkQueue<AllocatorWorkItem> workQueue;
        private int instanceId;
        private bool started;

        public AllocatorInstance(int instanceId, AllocatorWorkQueue<AllocatorWorkItem> sharedQueue)
        {
            this.instanceId = instanceId;
            this.workQueue = sharedQueue;
        }

        public void Start()
        {
            if (!started)
            {
                Thread t = new Thread(this.ThreadThunk);
                t.Start();

                started = true;
            }
        }

        public void ThreadThunk()
        {
            while(true)
            {
                try
                {
                    if (!this.TryHandleWorkItem())
                    {
                        // Idle Work
                        Thread.Sleep(3000);
                    }
                }
                catch (Exception e)
                {
                    // Log it
                    Console.WriteLine(e);
                }
            }
        }

        private bool TryHandleWorkItem()
        {
            if (this.workQueue.TryDequeue(out AllocatorWorkItem workItem))
            {
                this.HandleWorkItem(workItem);
                return true;
            }

            return false;
        }

        private void HandleWorkItem(AllocatorWorkItem workItem)
        {
        }
    }
}
