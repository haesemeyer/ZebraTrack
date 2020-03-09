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

namespace MHApi.Threading
{
    public class TimestampedObject<K>
    {
        /// <summary>
        /// The timestapm associated with the contained object
        /// </summary>
        public DateTime Timestamp { get; private set; }

        /// <summary>
        /// The contained object
        /// </summary>
        public K Object { get; private set; }

        /// <summary>
        /// Constructor setting the timestamp to NOW
        /// </summary>
        /// <param name="obj">The contained object</param>
        public TimestampedObject(K obj)
        {
            Timestamp = DateTime.Now;
            Object = obj;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="obj">The contained object</param>
        /// <param name="timestamp">The timestamp to associate with the object</param>
        public TimestampedObject(K obj, DateTime timestamp)
        {
            Timestamp = timestamp;
            Object = obj;
        }
    }
}
