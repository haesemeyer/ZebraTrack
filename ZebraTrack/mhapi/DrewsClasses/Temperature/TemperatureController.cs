/*
Copyright 2011 Drew Robson
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
using System.ComponentModel;
using System.Diagnostics;
using MHApi.GUI;

namespace MHApi.DrewsClasses.Temperature{
    public class TemperatureController : INotifyPropertyChanged, IDisposable {
        public Thermistor TheThermistor { get; private set; }
        public Peltier ThePeltier { get; private set; }

        object lockObject = new object();
        bool isRunning;
        double setPoint;
        double dt = 1.0/25.0;//this means, that a velocity of 1 will ramp the setpoint by one degree within one second considering our framerate
        double dutyCycle = 0;
        //private int ControllerNum { get; set; }

        /// <summary>
        /// Determines at what temperature difference we switch off rapid ramping
        /// </summary>
        const double RapidOffThreshold = 1.0;

        /// <summary>
        /// Used to track what the maximum error is all controllers
        /// accumulate. Used to trigger integral faults on ALL controllers
        /// </summary>
        static double _maxError = 0;

        # region Properties
        public bool IsRunning {
            get { return isRunning; }
            set {
                if (isRunning == value) return;
                isRunning = value;
                if (isRunning)
                    Start();
                else
                    Stop();
                RaisePropertyChanged("IsRunning");
            }
        }

        double curTemp;
        public double CurTemp {
            get { return curTemp; }
            set { curTemp = value; RaisePropertyChanged("CurTemp"); }
        }

        double targetTemperature = 23;

        /// <summary>
        /// Gets or sets the current target temperature
        /// </summary>
        public double TargetTemperature {
            get { return targetTemperature; }
            set {
                //If its a bigger step down we want to clear the integrated error
                if ((targetTemperature - value) > 4)
                    ResetIntegralOfError();
                targetTemperature = value;
                RaisePropertyChanged("TargetTemperature");
            }
        }

        double _kP;
        public double KP {
            get { return _kP; }
            set { _kP = value; RaisePropertyChanged("KP"); }
        }

        double _kI;
        public double KI {
            get { return _kI; }
            set { _kI = value; RaisePropertyChanged("KI"); }
        }

        double _integralOfError;
        public double IntegralOfError {
            get { return _integralOfError; }
            private set { _integralOfError = value; RaisePropertyChanged("IntegralOfError"); }
        }

        bool resetIntegral;
        public bool ResetIntegral {
            get { return resetIntegral; }
            set {
                resetIntegral = value;
                if (resetIntegral)
                    IntegralOfError = 0;
                RaisePropertyChanged("ResetIntegral");
            }
        }

        double _velocity;
        /// <summary>
        /// The update velocity in degrees/second
        /// </summary>
        public double Velocity {
            get { return _velocity; }
            set { _velocity = value; RaisePropertyChanged("Velocity"); }
        }

        /*double curTargetTemperature; 
        public double CurTargetTemperature {
            get { return curTargetTemperature;  }
            set { curTargetTemperature = value; RaisePropertyChanged("CurTargetTemperature"); }
        }*/

        double maxDutyCycle = 0.9999;

        /// <summary>
        /// The maximum allowed duty cycle
        /// </summary>
        public double MaxDutyCycle {
            get { return maxDutyCycle; }
            set { maxDutyCycle = Math.Min(value,0.9999); }
        }

        double _maxCoolDC = 0.99;//0.05;
        /// <summary>
        /// The maximum allowed duty cycle during cooling
        /// </summary>
        public double MaxCoolDC
        {
            get
            {
                return _maxCoolDC;
            }
            set
            {
                _maxCoolDC = Math.Min(value, 0.99);
            }
        }

        private double _maximumTemperature;

        /// <summary>
        /// The maximum allowed temperature before the controller will switch off
        /// and signal a temperature fault
        /// </summary>
        public double MaximumTemperature
        {
            get
            {
                return _maximumTemperature;
            }
            set
            {
                _maximumTemperature = value;
                RaisePropertyChanged("MaximumTemperature");
            }
        }

        private double _minimumTemperature;

        /// <summary>
        /// The minimum allowed temperature before the controller will switch off
        /// and signal a temperature fault
        /// </summary>
        public double MinimumTemperature
        {
            get
            {
                return _minimumTemperature;
            }
            set
            {
                _minimumTemperature = value;
                RaisePropertyChanged("MinimumTemperature");
            }
        }

        private bool _temperatureFault;

        /// <summary>
        /// Indicates an over- or under-temperature fault condition
        /// </summary>
        public bool TemperatureFault
        {
            get
            {
                return _temperatureFault;
            }
            private set
            {
                _temperatureFault = value;
                RaisePropertyChanged("TemperatureFault");
            }
        }

        private static bool _integralFault;

        /// <summary>
        /// Indicates a failure because a controller accumulated too much
        /// integral of error
        /// </summary>
        public static bool IntegralFault
        {
            get
            {
                return _integralFault;
            }
            private set
            {
                _integralFault = value;
            }
        }

        /// <summary>
        /// Gives the name of the controller. Aides in debugging
        /// fault conditions.
        /// </summary>
        private string _controllerName;
        # endregion//Properties

        /// <summary>
        /// Set up temperature controller
        /// </summary>
        /// <param name="thermistor">The thermistor of the thermistor/peltier pair controlling this segment</param>
        /// <param name="peltier">The peltier of the thermistor/peltier pair controlling this segment</param>
        /// <param name="kP">Proportionality constant of the error term (start low 0.0001) - "correct" value will depend on setup</param>
        /// <param name="kI">Proportionality constant of the integrated error term (start at 0 increase as necessary)</param>
        /// <param name="velocity">Determines the maximum attempted degree change per second</param>
        public TemperatureController(Thermistor thermistor, Peltier peltier, double kP, double kI, double velocity, string controllerName) {
            TheThermistor = thermistor;
            ThePeltier = peltier;
            TheThermistor.PropertyChanged += ThermistorPropertyChanged;
            _kP = kP;
            _kI = kI;
            _velocity = velocity;
            setPoint = TargetTemperature;
            MaximumTemperature = 38;
            MinimumTemperature = 19;
            _controllerName = controllerName;
        }

        public void Start() {
            if (ViewModelBase.IsInDesignMode) return;
            IntegralOfError = 0;
            setPoint = TargetTemperature;
            lock (lockObject)
                IsRunning = true;
        }

        public void Stop() {
            lock (lockObject) {
                ThePeltier.DutyCycle = 0;
                IsRunning = false;
            }
        }

        /// <summary>
        /// Resets the integral of error to 0
        /// </summary>
        public void ResetIntegralOfError() {
            IntegralOfError = 0;
        }

        /// <summary>
        /// Regulate the peltier based on the new temperature read out from the thermistor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ThermistorPropertyChanged(object sender, PropertyChangedEventArgs e) {
            lock (lockObject) {
                //CurTemp = Thermistor.Temperature;

                if (!IsRunning) return;

                //if controllers have detected an intergral fault we cease all operation
                //this condition cannot be recovered from.
                if (IntegralFault)
                {
                    ThePeltier.DutyCycle = 0;
                    return;
                }

                if (setPoint > TargetTemperature) {
                    setPoint -= Velocity * dt;
                    if (setPoint < TargetTemperature)
                        setPoint = TargetTemperature;
                }
                else if (setPoint < TargetTemperature) {
                    setPoint += Velocity * dt;
                    if (setPoint > TargetTemperature)
                        setPoint = TargetTemperature;
                }
                CurTemp = TheThermistor.Temperature;

                //Detect temperature fault condition
                if (CurTemp > MaximumTemperature || CurTemp < MinimumTemperature)
                {
                    if (!TemperatureFault)
                        System.Diagnostics.Debug.WriteLine("Temperature fault set, current temperature {0} C", CurTemp);
                    TemperatureFault = true;
                    ThePeltier.DutyCycle = 0;//switch off peltier
                    return;
                }
                else if (TemperatureFault)
                {
                    System.Diagnostics.Debug.WriteLine("Temperature fault reset");
                    TemperatureFault = false;
                }

                

                double error = setPoint - CurTemp;
                //System.Diagnostics.Debug.WriteLine(setPoint);
                IntegralOfError += error;

                //Detect error integral fault condition
                if (Math.Abs(IntegralOfError) > _maxError)
                {
                    //System.Diagnostics.Debug.WriteLine("New maximum error: {0}",Math.Round(IntegralOfError));
                    _maxError = Math.Abs(IntegralOfError);
                    if (_maxError > 20000)
                    {//very relaxed triggering at the moment
                        IntegralFault = true;
                        System.Diagnostics.Debug.WriteLine("Integral fault triggered, shutting down all controllers.",_controllerName);
                        ThePeltier.DutyCycle = 0;
                        return;
                    }
                }

                //slightly increase effective kp for cooling - above 30C below 30C we decrease
                //the contribution of the Integral error term but ONLY if the integral of the error
                //is also <0
                if (error > 0)
                    dutyCycle = KP * error + KI * IntegralOfError;
                else
                {
                    if(CurTemp>30)
                        dutyCycle = KP * error * 1.4 + KI * IntegralOfError;
                    else if(IntegralOfError>0)
                        dutyCycle = KP * error * 0.8 + KI * IntegralOfError;
                    else
                        dutyCycle = KP * error * 0.8 + KI * IntegralOfError*0.5;
                    //dutyCycle = KP * error * 1.4 + KI * IntegralOfError;
                }
                //System.Diagnostics.Debug.WriteLine(dutyCycle);

                if (dutyCycle >= 0 && dutyCycle < MaxDutyCycle) {
                    ThePeltier.PeltierMode = PeltierModes.Heat;
                    ThePeltier.DutyCycle = dutyCycle;
                } else if (dutyCycle < 0) {
                    //We want to in general cool less aggressively than heat
                    double dc = Math.Abs(dutyCycle);
                    dc = Math.Min(dc, _maxCoolDC);
                    ThePeltier.PeltierMode = PeltierModes.Cool;
                    ThePeltier.DutyCycle = dc;
                } else if (dutyCycle >= MaxDutyCycle) {
                    ThePeltier.PeltierMode = PeltierModes.Heat;
                    ThePeltier.DutyCycle = MaxDutyCycle;
                }
                //Debug.WriteLine(ThePeltier.PeltierMode);
            }
        }

        public void Dispose() {
            lock (lockObject)
                IsRunning = false;
            ThePeltier.DutyCycle = 0;
            TheThermistor.PropertyChanged -= ThermistorPropertyChanged;
        }



        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        protected void RaisePropertyChanged(string name)
        {
            if (name != "")
            {
                VerifyPropertyName(name);
            }
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        protected void VerifyPropertyName(string propertyName)
        {
            var myType = GetType();
            if (myType.GetProperty(propertyName) == null)
                throw new ArgumentException("Property not found", propertyName);
        }
        #endregion

    }
}