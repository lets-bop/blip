using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlacementStore
{
    public class LongSFTransactionTimeFailover
    {
        // Lock for maintaining the queue.
        private readonly object lockObject;

        /// <summary>
        /// The time in the past to maintain items in the queue.
        /// Any items older than that time will be discarded opportunistically.
        /// </summary>
        private readonly TimeSpan fullTimeWindow;

        private readonly TimeSpan halfTimeWindow;

        private readonly int maxReadWriteServiceFabricTimeInMicroseconds;
        private readonly bool failoverAzAllocatorEnabled;

        private DateTime now
        {
            get
            {
                return DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Internal queue we control access to. Has all the datapoints for the first time window period.
        /// It is expected the front is the earliest time, and the back is the latest.
        /// Always lock(queue) before using it. 
        /// </summary>
        private readonly Queue<TransactionData> firstBucketQueue;

        /// <summary>
        /// Internal queue we control access to. Has all the datapoints for the second time window period.
        /// It is expected the front is the earliest time, and the back is the latest.
        /// Always lock(queue) before using it. 
        /// </summary>
        private readonly Queue<TransactionData> secondBucketQueue;

        private readonly Action<string> gracefulFailoverAction;

        internal double FirstBucketAverage { get; private set; }
        internal double SecondBucketAverage { get; private set; }

        public LongSFTransactionTimeFailover(
            TimeSpan fullTimeWindow,
            TimeSpan halfTimeWindow,
            int maxReadWriteServiceFabricTimeInMicroseconds,
            bool failoverAzAllocatorEnabled,
            Action<string> gracefulFailoverAction)
        {
            this.firstBucketQueue = new Queue<TransactionData>();
            this.secondBucketQueue = new Queue<TransactionData>();
            this.lockObject = new object();
            this.FirstBucketAverage = 0;
            this.SecondBucketAverage = 0;
            this.fullTimeWindow = fullTimeWindow;
            this.halfTimeWindow = halfTimeWindow;
            this.maxReadWriteServiceFabricTimeInMicroseconds = maxReadWriteServiceFabricTimeInMicroseconds;
            this.failoverAzAllocatorEnabled = failoverAzAllocatorEnabled;
            this.gracefulFailoverAction = gracefulFailoverAction;
        }

        /// <summary>
        /// Adds a new item, rebalances two queues and recalculates the new averages in two buckets.
        /// Checks if a failover is needed based on the value of the averages.
        /// </summary>
        public void NotifyTransactionTime(long elapsedMicroseconds)
        {
            if (this.failoverAzAllocatorEnabled == false)
            {
                return;
            }

            lock (this.lockObject)
            {
                DateTime firstWindowStart = this.now - this.halfTimeWindow;
                DateTime secondWindowStart = this.now - this.fullTimeWindow;

                int firstBucketOldCount = this.firstBucketQueue.Count;
                int secondBucketOldCount = this.secondBucketQueue.Count;

                this.firstBucketQueue.Enqueue(new TransactionData(elapsedMicroseconds, this.now));

                double firstBucketDelta = elapsedMicroseconds;
                double secondBucketDelta = 0;

                while (this.firstBucketQueue.Count > 0 && this.firstBucketQueue.Peek().TimeStamp < firstWindowStart)
                {
                    TransactionData firstBucketDatapoint = this.firstBucketQueue.Dequeue();
                    firstBucketDelta -= firstBucketDatapoint.ElapsedMicroseconds;

                    this.secondBucketQueue.Enqueue(firstBucketDatapoint);
                    secondBucketDelta += firstBucketDatapoint.ElapsedMicroseconds;
                }

                while (this.secondBucketQueue.Count > 0 && this.secondBucketQueue.Peek().TimeStamp < secondWindowStart)
                {
                    secondBucketDelta -= this.secondBucketQueue.Dequeue().ElapsedMicroseconds;
                }

                this.FirstBucketAverage = RecalculateAverage(firstBucketDelta, this.FirstBucketAverage, firstBucketOldCount, this.firstBucketQueue.Count);
                this.SecondBucketAverage = RecalculateAverage(secondBucketDelta, this.SecondBucketAverage, secondBucketOldCount, this.secondBucketQueue.Count);

                if ((this.firstBucketQueue.Count > 15 && this.FirstBucketAverage > this.maxReadWriteServiceFabricTimeInMicroseconds) &&
                    (this.secondBucketQueue.Count > 15 && this.SecondBucketAverage > this.maxReadWriteServiceFabricTimeInMicroseconds))
                {
                    Console.WriteLine($"ServiceFabric transactions take long time. Average in a first bucket is {this.FirstBucketAverage}, average in a second bucket is {this.SecondBucketAverage}.");
                }
            }
        }

        private static double RecalculateAverage(double delta, double oldAverage, int oldCount, int newCount)
        {
            if (newCount == 0) return 0;

            return (oldAverage * oldCount + delta) / newCount;
        }
    }

    internal struct TransactionData
    {
        public DateTime TimeStamp { get; }
        public long ElapsedMicroseconds { get; }

        public TransactionData(long elapsedMicroseconds, DateTime timeStamp)
        {
            this.TimeStamp = timeStamp;
            this.ElapsedMicroseconds = elapsedMicroseconds;
        }
    }
}
