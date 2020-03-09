/*
Copyright 2016 Martin Haesemeyer
   Licensed under the MIT License, see License.txt.
   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
   AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
   LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
   OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
   SOFTWARE.
*/

using System;
using System.Threading;


namespace MHApi.Threading
{
    /// <summary>
    /// Generic thread encapsulation that allows to pass arbitrary
    /// data to be passed to a thread job
    /// </summary>
    /// <typeparam name="T">Data type to relay to thread procedure</typeparam>
    public class WorkerT<T> : IDisposable
    {
        /// <summary>
        /// The autoreset event used to signal the thread to stop executing
        /// </summary>
        AutoResetEvent _stop;

        /// <summary>
        /// The actual thread running our thread job
        /// </summary>
        Thread _thread;

        /// <summary>
        /// Data that will be passed as an argument to the thread job
        /// </summary>
        protected T ThreadData {  get;  set; }

        /// <summary>
        /// The number of ms we wait for the thread-join before throwing an exception
        /// </summary>
        public int JoinTimeout { get; protected set; }

        /// <summary>
        /// Indicates if the thread is started
        /// </summary>
        public bool IsRunning { get; protected set; }

        /// <summary>
        /// Delegate for the client thread procedure
        /// </summary>
        /// <param name="stop">The autoreset event used to communicate with the thread</param>
        /// <param name="dispatcher">The dispatcher of the threads owner</param>
        public delegate void ThreadJob(AutoResetEvent stop, T threadData);

        /// <summary>
        /// The thread job to execute
        /// </summary>
        protected ThreadJob threadJob;


        /// <summary>
        /// Constructor with autostart
        /// </summary>
        /// <param name="job">The method to execute from the thread</param>
        public WorkerT(ThreadJob job, T threadData) : this(job, threadData, true, 500)
        { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="job">The method to execute from the thread</param>
        /// <param name="autostart">Whether to automatically or manually start the thread</param>
        public WorkerT(ThreadJob job, T threadData, bool autostart) : this(job, threadData, autostart, 500)
        { }
        

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="job">The method to execute from the thread</param>
        /// <param name="autostart">Whether to automatically or manually start the thread</param>
        /// <param name="joinTimeout">The time to wait for the thread join on disposal or stop</param>
        public WorkerT(ThreadJob job, T threadData, bool autostart, int joinTimeout)
        {
            JoinTimeout = joinTimeout;
            threadJob = job;
            ThreadData = threadData;
            Init(autostart);
        }

        /// <summary>
        /// Manual starting of the thread
        /// </summary>
        public void Start()
        {
            if (IsDisposed)
                throw new ObjectDisposedException("Worker","Attempted to start disposed worker thread");
            if (IsRunning)
                throw new ApplicationException("Attempted to start already running thread");
            _thread.Start();
            IsRunning = true;
        }

        /// <summary>
        /// Manual stop of the thread
        /// </summary>
        public void Stop()
        {
            if (IsDisposed)
                throw new ObjectDisposedException("Worker","Attempted to start disposed worker thread");
            if (IsRunning)
            {
                _stop.Set();
                if (!_thread.Join(JoinTimeout))
                {
                    throw new ApplicationException("ThreadJoinFailed");
                }
            }
            IsRunning = false;
        }

        /// <summary>
        /// Initialization routine called by the constructors
        /// </summary>
        /// <param name="autostart">Whether to automatically or manually start the thread</param>
        protected virtual void Init(bool autostart)
        {
            _stop = new AutoResetEvent(false);
            _thread = new Thread(threadProc);
            _thread.Name = threadJob.Method.ToString();
            _thread.IsBackground = true;
            if (autostart)
            {
                _thread.Start();
                IsRunning = true;
            }
        }

        private void threadProc()
        {
            threadJob.Invoke(_stop,ThreadData);
            IsRunning = false;
        }



        #region IDisposable Members

        public bool IsDisposed { get; private set; }

        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;
            if (disposing)
            {
                if (IsRunning)
                {
                    _stop.Set();
                    if (!_thread.Join(JoinTimeout))
                    {
                        throw new ApplicationException("ThreadJoinFailed");
                    }
                }
            }
            else
                if (IsRunning)
                    _stop.Set();
            IsDisposed = true;
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~WorkerT()
        {
            Dispose(false);
        }

        #endregion
    }
}
