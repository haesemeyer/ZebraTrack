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
    /// Extended implementation of a PI controller
    /// that tries minimizing both error from a setpoint as
    /// well as the absolute value of a constraint sensor
    /// </summary>
    /// <remarks>This controller operates under the assumption
    /// that any energy input in the system has the consequence
    /// of INCREASING the delta given as the secondary constraint
    /// (f.e.: Heating system tries to get average of two probes
    /// close to the setpoint but uneven heating at the same time
    /// increases the delta-T between the probes which may be
    /// undesirable)</remarks>
    public class DeltaConstrainedPIController : IDisposable
    {
        #region Fields

        /// <summary>
        /// Indicates whether the control loop is running
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
        /// The proportional effect of our constraint
        /// </summary>
        double _kPConstraint;

        /// <summary>
        /// The integrated effect of our constraint
        /// </summary>
        double _kIConstraint;

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
        /// The accumulated error on our constraint
        /// </summary>
        double _constraintIOE;

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
        /// For our constraint condition the
        /// acceptable absolute delta
        /// </summary>
        double _acceptableDelta;

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
        /// The main sensor used by the controller
        /// </summary>
        Sensor _sensor;

        /// <summary>
        /// The sensor of our secondary constraint
        /// condition
        /// </summary>
        Sensor _delta;

        /// <summary>
        /// The effector used by the controller
        /// </summary>
        Effector _effector;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether the control loop is running
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return _isRunning;
            }
            protected set
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
        /// The proportional effect of our constraint
        /// </summary>
        public double KPConstraint
        {
            get
            {
                return _kPConstraint;
            }
            set
            {
                _kPConstraint = value;
            }
        }

        /// <summary>
        /// The integrated effect of our constraint
        /// </summary>
        public double KIConstraint
        {
            get
            {
                return _kIConstraint;
            }
            set
            {
                _kIConstraint = value;
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

        /// <summary>
        /// For our constraint condition the
        /// acceptable absolute delta
        /// </summary>
        /// <remarks>If this value is set too
        /// low in relation to what is feasible
        /// by the system, the integral of the
        /// constraint error will grow out of
        /// bounds, effectively disabling the
        /// controller!</remarks>
        public double AcceptableDelta
        {
            get
            {
                return _acceptableDelta;
            }
            set
            {
                _acceptableDelta = value;
            }
        }

        #endregion

        /// <summary>
        /// Constructs a new DeltaConstrainedPIController object
        /// </summary>
        /// <param name="target">The initial target of the system</param>
        /// <param name="sensor">The main sensor to read</param>
        /// <param name="delta">The sensor reporting on the delta of our constraint</param>
        /// <param name="effector">The system effector</param>
        /// <param name="kP">The proportional constant of our controller</param>
        /// <param name="kI">The integral constant of our controller</param>
        /// <param name="updateStep">The step width with which we move towards a new setpoint</param>
        /// <param name="kpConstraint">The proportional effect of the delta constraint</param>
        /// <param name="kIConstraint">The integral effect of the delta constraint</param>
        /// <param name="acceptableDelta">The maximum acceptable delta (without effect on controller)</param>
        /// <param name="id">The name of the controller</param>
        public DeltaConstrainedPIController(double target, Sensor sensor, Sensor delta, Effector effector, double kP, 
            double kI, double updateStep, double kpConstraint, double kIConstraint, double acceptableDelta, string id)
        {
            _sensor = sensor;
            _sensor.ValueChanged += ControlLoop;
            _delta = delta;
            _effector = effector;
            KP = kP;
            KI = kI;
            UpdateStep = updateStep;
            KPConstraint = kpConstraint;
            KIConstraint = kIConstraint;
            AcceptableDelta = acceptableDelta;
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
            //compute error on delta constraint - this error is only negative if we are within
            //the acceptable range - so the integral can grow out of bounds if the acceptable
            //value is set too stringent effectively disabling the controller!
            double errorDelta = Math.Abs(_delta.Value) - _acceptableDelta;
            _constraintIOE += errorDelta;
            //our integral of the constraint should not get negative!
            if (_constraintIOE < 0)
                _constraintIOE = 0;
            //detect integral fault
            if (Math.Abs(_integralOfError) > _maxIntegralError)
            {
                System.Diagnostics.Debug.WriteLine("Integral fault detected in controller {0}. Integral error = {1}. Shutting down controller", _id, _integralOfError);
                _effector.StopOnFatalFault();
                _integralFault = true;
                return;
            }
            //a negative errorDelta should not impact the output
            if (errorDelta < 0)
                errorDelta = 0;
            //update the output on the effector
            _effector.Output = error * _kP + _integralOfError * _kI - errorDelta * _kPConstraint - _constraintIOE * _kIConstraint;
        }

        /// <summary>
        /// Start up the controller with a new initial setpoint
        /// </summary>
        /// <param name="initialTarget">The initial immediate setpoint</param>
        public void Start(double initialTarget)
        {
            if (IsRunning)
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
            if (!IsRunning)
                return;
            _effector.Stop();
            IsRunning = false;
        }

        /// <summary>
        /// Resets the accumulated errors to 0
        /// </summary>
        public void ResetIntegralOfError()
        {
            _integralOfError = 0;
            _constraintIOE = 0;
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
