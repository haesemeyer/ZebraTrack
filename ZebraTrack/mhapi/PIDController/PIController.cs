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
    /// Generic implementation of a PI controller
    /// NOTE: The control loop gets clocked by the update frequency of the sensor!!
    /// </summary>
    public class PIController : IDisposable
    {
        #region Fields

        /// <summary>
        /// Indicates whether the controller is
        /// currently operational
        /// </summary>
        bool _isRunning;

        /// <summary>
        /// The proportional constant of the controller
        /// </summary>
        double _kP;

        /// <summary>
        /// The Integral constant of the controller
        /// </summary>
        double _kI;

        /// <summary>
        /// Specifies how fast we try to achieve
        /// a new setpoint - per update step
        /// we will move towards the target
        /// by this amount
        /// </summary>
        double _updateStep;

        /// <summary>
        /// If our sensor reports more than this value
        /// we will temporarily shut down the effector system
        /// </summary>
        double _maxSystemValue;

        /// <summary>
        /// If our sensor reports less than this value
        /// we will temporarily shut down the effector system
        /// </summary>
        double _minSystemValue;

        /// <summary>
        /// Indicates whether the controller is currently
        /// outside of the system limits
        /// </summary>
        bool _limitFault;

        /// <summary>
        /// The accumulated error
        /// </summary>
        double _integralOfError;

        /// <summary>
        /// Indicates the maximum allowed integral error
        /// before entering integral fault state
        /// </summary>
        double _maxIntegralError;

        /// <summary>
        /// If the controller accumulates a larger error
        /// than the allowed maximum we enter a non-recoverable
        /// fault condition
        /// </summary>
        bool _integralFault;

        /// <summary>
        /// The target we want to achieve
        /// </summary>
        double _target;

        /// <summary>
        /// Internal setpoint - moves towards target
        /// at maximum speed of _updateStep
        /// </summary>
        double _setPoint;

        /// <summary>
        /// The id of the controller to trace
        /// faults
        /// </summary>
        string _id;



        /// <summary>
        /// The sensor used by the controller
        /// </summary>
        Sensor _sensor;

        /// <summary>
        /// The effector used by the controller
        /// </summary>
        Effector _effector;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether the controller is
        /// currently operational
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return _isRunning;
            }
            private set
            {
                _isRunning = value;
            }
        }

        /// <summary>
        /// The proportional constant of the controller
        /// </summary>
        public double KP
        {
            get
            {
                return _kP;
            }
            set
            {
                _kP = value;
            }
        }

        /// <summary>
        /// The Integral constant of the controller
        /// </summary>
        public double KI
        {
            get
            {
                return _kI;
            }
            set
            {
                _kI = value;
            }
        }

        /// <summary>
        /// Specifies how fast we try to achieve
        /// a new setpoint - per update step
        /// we will move towards the target
        /// by this amount
        /// </summary>
        public double UpdateStep
        {
            get
            {
                return _updateStep;
            }
            set
            {
                _updateStep = value;
            }
        }

        /// <summary>
        /// If our sensor reports more than this value
        /// we will temporarily shut down the effector system
        /// </summary>
        public double MaxSystemValue
        {
            get
            {
                return _maxSystemValue;
            }
            set
            {
                _maxSystemValue = value;
            }
        }

        /// <summary>
        /// If our sensor reports less than this value
        /// we will temporarily shut down the effector system
        /// </summary>
        public double MinSystemValue
        {
            get
            {
                return _minSystemValue;
            }
            set
            {
                _minSystemValue = value;
            }
        }

        /// <summary>
        /// Indicates whether the controller is currently
        /// outside of the system limits
        /// </summary>
        public bool LimitFault
        {
            get
            {
                return _limitFault;
            }
        }

        /// <summary>
        /// Indicates the maximum allowed integral error
        /// before entering integral fault state
        /// </summary>
        public double MaxIntegralError
        {
            get
            {
                return _maxIntegralError;
            }
            set
            {
                _maxIntegralError = value;
            }
        }

        /// <summary>
        /// If the controller accumulates a larger error
        /// than the allowed maximum we enter a non-recoverable
        /// fault condition
        /// </summary>
        public bool IntegralFault
        {
            get
            {
                return _integralFault;
            }
        }

        /// <summary>
        /// The target we want to achieve
        /// </summary>
        public double Target
        {
            get
            {
                return _target;
            }
            set
            {
                _target = value;
            }
        }

        #endregion

        /// <summary>
        /// Constructs a new PI controller object
        /// </summary>
        /// <param name="target">The initial target of the system</param>
        /// <param name="sensor">The sensor to read</param>
        /// <param name="effector">The effector to adjust the system</param>
        /// <param name="kP">The proportional term</param>
        /// <param name="kI">The integral term</param>
        /// <param name="updateStep">The step width with which we move towards a new setpoint</param>
        /// <param name="id">The id of this controller</param>
        public PIController(double target, Sensor sensor, Effector effector, double kP, double kI, double updateStep, string id)
        {
            _sensor = sensor;
            _sensor.ValueChanged += ControlLoop;
            _effector = effector;
            KP = kP;
            KI = kI;
            UpdateStep = updateStep;
            _id = id;
            Target = target;
            _setPoint = target;
            _integralOfError = 0;
            //by default don't enforce any limits
            //on the controller
            MaxSystemValue = double.MaxValue;
            MinSystemValue = double.MinValue;
            MaxIntegralError = double.MaxValue;
        }

        #region Methods

        void ControlLoop(double newValue)
        {
            //ignore sensor updates as long as the controller is not running
            //or if we are in unrecoverable integral fault
            if (!_isRunning || _integralFault)
                return;
            //approach setpoint to target
            if (_setPoint > _target)
                _setPoint = (_setPoint - _updateStep > _target) ? _setPoint - _updateStep : _target;
            else if (_setPoint < _target)
                _setPoint = (_setPoint + _updateStep < _target) ? _setPoint + _updateStep : _target;
            //if we currently have a limit fault but are within limits now we recover - otherwise we exit
            //at this point
            if (_limitFault)
            {
                if (newValue <= _maxSystemValue && newValue >= _minSystemValue)
                {
                    _limitFault = false;
                    _effector.Start();
                    System.Diagnostics.Debug.WriteLine("Recovered from limit fault in controller" + _id);
                }
                else
                    return;
            }
            //detect new system limit condition
            if (newValue > _maxSystemValue || newValue < _minSystemValue)
            {
                System.Diagnostics.Debug.WriteLine("Limit fault detected in controller {0}. Value = {1}.", _id, newValue);
                _limitFault = true;
                //temporarily stop the effector and exit
                _effector.Stop();
                return;
            }
            //compute current error
            double error = _setPoint - newValue;
            _integralOfError += error;
            //detect integral fault
            if (Math.Abs(_integralOfError) > _maxIntegralError)
            {
                System.Diagnostics.Debug.WriteLine("Integral fault detected in controller {0}. Integral error = {1}. Shutting down controller", _id, _integralOfError);
                _effector.StopOnFatalFault();
                _integralFault = true;
                return;
            }
            //update the output on the effector
            _effector.Output = error * _kP + _integralOfError * _kI;
        }

        /// <summary>
        /// Start up the controller with a new initial setpoint
        /// </summary>
        /// <param name="initialTarget">The initial immediate setpoint</param>
        public void Start(double initialTarget)
        {
            if(IsRunning)
                return;
            if (IsDisposed)
                throw new ObjectDisposedException("PIController");
            Target = initialTarget;
            _setPoint = initialTarget;
            ResetIntegralOfError();
            _effector.Start();
            IsRunning = true;
        }

        /// <summary>
        /// Restart the controller at the current setpoint
        /// </summary>
        public void Restart()
        {
            Start(_setPoint);
        }

        public void Stop()
        {
            if (IsDisposed)
                throw new ObjectDisposedException("PIController");
            if(!IsRunning)
                return;
            _effector.Stop();
            IsRunning = false;
        }

        /// <summary>
        /// Resets the accumulated error to 0
        /// </summary>
        public void ResetIntegralOfError()
        {
            _integralOfError = 0;
        }

        #endregion

        #region IDisposable

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (IsDisposed)
                return;
            Dispose(true);
            IsDisposed = true;
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (IsRunning)
                Stop();
            _sensor.ValueChanged -= ControlLoop;
        }

        #endregion
    }
}
