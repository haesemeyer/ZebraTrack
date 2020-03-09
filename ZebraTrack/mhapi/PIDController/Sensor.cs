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

namespace MHApi.PIDController
{
    /// <summary>
    /// Implements a generic sensor for a
    /// PID controller
    /// </summary>
    public abstract class Sensor
    {
        /// <summary>
        /// The value sensed by the sensor
        /// </summary>
        double _value;

        /// <summary>
        /// The value currently sensed
        /// by the sensor
        /// </summary>
        public virtual double Value
        {
            get
            {
                return _value;
            }
            protected set
            {
                _value = value;
                if (ValueChanged != null)
                    ValueChanged(_value);
            }
        }

        /// <summary>
        /// This event gets raised whenever the sensor
        /// value changes
        /// </summary>
        public Action<double> ValueChanged = delegate { };
    }
}
