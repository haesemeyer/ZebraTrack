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
    public abstract class Effector : IDisposable
    {
        /// <summary>
        /// The maximum desired output on the effector system
        /// </summary>
        double _maxOutput;

        /// <summary>
        /// The minimum desired output on the effector system
        /// </summary>
        double _minOutput;

        /// <summary>
        /// The current output on the effector system
        /// </summary>
        double _output;

        /// <summary>
        /// The maximum desired output on the effector system
        /// </summary>
        public virtual double MaxOutput
        {
            get
            {
                return _maxOutput;
            }
            set
            {
                _maxOutput = value;
            }
        }

        /// <summary>
        /// The minimum desired output on the effector system
        /// </summary>
        public virtual double MinOutput
        {
            get
            {
                return _minOutput;
            }
            set
            {
                _minOutput = value;
            }
        }

        /// <summary>
        /// Gets/sets the current output on the system
        /// </summary>
        public virtual double Output
        {
            get
            {
                return _output;
            }
            set
            {
                if (value > MaxOutput)
                    _output = MaxOutput;
                else if (value < MinOutput)
                    _output = MinOutput;
                else
                    _output = value;
                //Our desired output changed need to communicate to hardware
                RealizeOutput();
            }
        }

        /// <summary>
        /// This method should implement the scaling and communication
        /// between output values and the underlying hardware
        /// </summary>
        protected abstract void RealizeOutput();

        /// <summary>
        /// Used by the controller to shutdown
        /// the effector upon unrecoverable fault conditions
        /// </summary>
        public virtual void StopOnFatalFault()
        {
            //defaults simply to stop
            Stop();
        }

        /// <summary>
        /// Tells the effector to shut down
        /// </summary>
        public abstract void Stop();

        /// <summary>
        /// Allows the effector to perform initialization
        /// and startup routines
        /// </summary>
        public abstract void Start();

        #region IDisposable

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (!IsDisposed)
                Dispose(true);
            IsDisposed = true;
        }

        protected virtual void Dispose(bool disposing)
        {
            Stop();
        }

        #endregion
    }
}
