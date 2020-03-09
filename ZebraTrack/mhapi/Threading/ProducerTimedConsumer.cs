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
    public class ProducerTimedConsumer<T,K> : ProducerConsumer<T> where T:TimestampedObject<K>
    {
        /// <summary>
        /// The (minimum) delay in ms before an item on the queue gets consumed
        /// </summary>
        public uint ConsumeDelay { get; private set; }

        public ProducerTimedConsumer(uint consumeDelay)
        {
            MaxCount = 100;
            ConsumeDelay = consumeDelay;
        }

        public ProducerTimedConsumer(uint consumeDelay,int maxCount)
        {
            MaxCount = maxCount;
            ConsumeDelay = consumeDelay;
        }

        public override T Consume()
        {
            lock (queueLock)
            {
                while (queue.Count == 0 || (DateTime.Now-queue.Peek().Timestamp).TotalMilliseconds < ConsumeDelay)
                {
                    Monitor.Wait(queueLock);
                }
                return queue.Dequeue();
            }
        }

        public override T Consume(AutoResetEvent stop)
        {
            lock (queueLock)
            {
                while (queue.Count == 0 || (DateTime.Now - queue.Peek().Timestamp).TotalMilliseconds < ConsumeDelay)
                {
                    //If we are told to stop then we return null.
                    if (stop.WaitOne(0))
                        return null;
                    //We wait for a pulse for 100ms. After that we independently try to reacquire the lock.
                    //This way if the the producer has retired we are not indefinitely stuck on the wait and can listen
                    //for a stop signal
                    Monitor.Wait(queueLock, 100);
                }
                return queue.Dequeue();
            }
        }
    }
}
