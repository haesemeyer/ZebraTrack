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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;


namespace MHApi.Threading
{
    public class ProducerConsumer<T> //where T:class
    {
        protected readonly object queueLock = new object();

        protected Queue<T> queue = new Queue<T>();

        //The maximum allowed element number
        public int MaxCount { get; protected set; }

        public ProducerConsumer()
        {
            MaxCount = 100;
        }

        public ProducerConsumer(int maxCount)
        {
            MaxCount = maxCount;
        }

        /// <summary>
        /// Puts an item on the queue. Locks access to the queue and pulses the lock to signal consumers. Clears the queue if maxcount reached
        /// </summary>
        /// <param name="item">The item to add to the queue</param>
        public void Produce(T item)
        {
            lock (queueLock)
            {
                if (queue.Count > MaxCount)
                {
                    queue.Clear();
                    System.Diagnostics.Debug.WriteLine("Overflow, queue was cleared.");
                }
                queue.Enqueue(item);
                
                /*if(queue.Count>1)
                    System.Diagnostics.Debug.WriteLine(String.Format("{0} items in queue.", queue.Count));*/
                
                //signal waiting threads that we will release the lock
                Monitor.Pulse(queueLock);
            }
        }

        /// <summary>
        /// Retrieves an item from the queue. Waits on a lock to the queue while the queue is empty allowing items to be added.
        /// </summary>
        /// <returns>Returns the next item from the queue</returns>
        public virtual T Consume()
        {
            lock (queueLock)
            {
                while (queue.Count == 0)
                {
                    Monitor.Wait(queueLock);
                }
                return queue.Dequeue();
            }
        }

        /// <summary>
        /// Retrieves an item from the queue. Waits on a lock to the queue while the queue is empty allowing items to be added.
        /// If stop is signaled, the method will immediately return allowing the thread to stop rather than entering deadlock
        /// once the producer is retired.
        /// </summary>
        /// <param name="stop">The event to signal to leave the method</param>
        /// <returns>Returns the next item from the queue</returns>
        public virtual T Consume(AutoResetEvent stop)
        {
            lock (queueLock)
            {
                while (queue.Count == 0)
                {
                    //If we are told to stop then we return null/default
                    if (stop.WaitOne(0))
                        throw new OperationCanceledException("Consume signaled to stop");
                    //We wait for a pulse for 100ms. After that we independently try to reacquire the lock.
                    //This way if the the producer has retired we are not indefinitely stuck on the wait and can listen
                    //for a stop signal
                    Monitor.Wait(queueLock,100);
                }
                return queue.Dequeue();
            }
        }
    }
}
