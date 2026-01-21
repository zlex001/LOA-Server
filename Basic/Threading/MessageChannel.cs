using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Basic.Threading
{
    /// <summary>
    /// Thread-safe message channel interface for inter-thread communication.
    /// Designed with forward compatibility for future inter-process communication (Phase 2).
    /// </summary>
    public interface IMessageChannel<T>
    {
        void Send(T message);
        bool TryReceive(out T message);
        int Count { get; }
    }

    /// <summary>
    /// In-process message channel using ConcurrentQueue.
    /// Phase 1 implementation - can be replaced with socket-based channel in Phase 2.
    /// </summary>
    public class InProcessChannel<T> : IMessageChannel<T>
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();

        public void Send(T message)
        {
            _queue.Enqueue(message);
        }

        public bool TryReceive(out T message)
        {
            return _queue.TryDequeue(out message);
        }

        public int Count => _queue.Count;
    }

    /// <summary>
    /// Batch message channel that allows processing multiple messages at once.
    /// </summary>
    public class BatchChannel<T> : IMessageChannel<T>
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        private readonly int _maxBatchSize;

        public BatchChannel(int maxBatchSize = 100)
        {
            _maxBatchSize = maxBatchSize;
        }

        public void Send(T message)
        {
            _queue.Enqueue(message);
        }

        public bool TryReceive(out T message)
        {
            return _queue.TryDequeue(out message);
        }

        public int Count => _queue.Count;

        /// <summary>
        /// Process up to maxBatchSize messages in one call.
        /// Returns the number of messages processed.
        /// </summary>
        public int ProcessBatch(Action<T> handler)
        {
            int processed = 0;
            while (processed < _maxBatchSize && _queue.TryDequeue(out T message))
            {
                try
                {
                    handler(message);
                }
                catch (Exception ex)
                {
                    Utils.Debug.Log.Error("CHANNEL", $"Error processing message: {ex.Message}");
                }
                processed++;
            }
            return processed;
        }
    }
}
