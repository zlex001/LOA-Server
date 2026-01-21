using System;
using System.Threading;

namespace Basic.Threading
{
    /// <summary>
    /// Base class for background thread workers.
    /// Provides standard lifecycle management: Start, Stop, graceful shutdown.
    /// </summary>
    public abstract class ThreadWorker
    {
        private Thread _thread;
        private volatile bool _running;
        private readonly string _name;
        private readonly int _sleepIntervalMs;

        protected bool IsRunning => _running;

        protected ThreadWorker(string name, int sleepIntervalMs = 1)
        {
            _name = name;
            _sleepIntervalMs = sleepIntervalMs;
        }

        /// <summary>
        /// Start the worker thread.
        /// </summary>
        public void Start()
        {
            if (_thread != null && _thread.IsAlive)
            {
                Utils.Debug.Log.Warning("THREAD", $"[{_name}] Thread already running");
                return;
            }

            _running = true;
            _thread = new Thread(RunLoop)
            {
                Name = _name,
                IsBackground = true
            };
            _thread.Start();
            Utils.Debug.Log.Info("THREAD", $"[{_name}] Thread started");
        }

        /// <summary>
        /// Stop the worker thread gracefully.
        /// </summary>
        public void Stop(int timeoutMs = 3000)
        {
            if (_thread == null || !_thread.IsAlive)
            {
                return;
            }

            Utils.Debug.Log.Info("THREAD", $"[{_name}] Stopping thread...");
            _running = false;

            if (!_thread.Join(timeoutMs))
            {
                Utils.Debug.Log.Warning("THREAD", $"[{_name}] Thread did not stop gracefully, aborting");
                try
                {
                    _thread.Interrupt();
                }
                catch { }
            }
            else
            {
                Utils.Debug.Log.Info("THREAD", $"[{_name}] Thread stopped gracefully");
            }
        }

        private void RunLoop()
        {
            try
            {
                OnStart();

                while (_running)
                {
                    try
                    {
                        DoWork();
                    }
                    catch (Exception ex)
                    {
                        Utils.Debug.Log.Error("THREAD", $"[{_name}] Error in DoWork: {ex.Message}");
                    }

                    if (_sleepIntervalMs > 0)
                    {
                        Thread.Sleep(_sleepIntervalMs);
                    }
                }

                OnStop();
            }
            catch (ThreadInterruptedException)
            {
                Utils.Debug.Log.Warning("THREAD", $"[{_name}] Thread interrupted");
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("THREAD", $"[{_name}] Fatal error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Called once when the thread starts, before the main loop.
        /// </summary>
        protected virtual void OnStart() { }

        /// <summary>
        /// Called once when the thread stops, after the main loop.
        /// </summary>
        protected virtual void OnStop() { }

        /// <summary>
        /// The main work method, called repeatedly while the thread is running.
        /// </summary>
        protected abstract void DoWork();
    }
}
