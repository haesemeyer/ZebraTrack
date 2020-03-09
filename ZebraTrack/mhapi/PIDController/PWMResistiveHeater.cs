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

using NationalInstruments.DAQmx;

namespace MHApi.PIDController
{
    /// <summary>
    /// Implements a pulse-width-modulated resistive
    /// heater as an effector
    /// </summary>
    public class PWMResistiveHeater : Effector
    {

        #region Fields

        /// <summary>
        /// Represents our pwm signal
        /// </summary>
        CODataFrequency _pwmSignal;

        /// <summary>
        /// The task to encapsulate the counter
        /// </summary>
        Task _pwmTask;

        /// <summary>
        /// The streamwriter for our counter task
        /// </summary>
        CounterSingleChannelWriter _pwmWriter;

        #endregion

        #region Properties

        /// <summary>
        /// The current duty cycle
        /// </summary>
        public double DutyCycle
        {
            get
            {
                return _pwmSignal.DutyCycle;
            }
        }

        /// <summary>
        /// The current pwm frequency
        /// </summary>
        public double Frequency
        {
            get
            {
                return _pwmSignal.Frequency;
            }
        }

        public bool IsRunning { get; private set; }

        #endregion


        /// <summary>
        /// Constructs a new PWMResistive Heater
        /// </summary>
        /// <param name="deviceName">The device on which the pwm counter sits</param>
        /// <param name="pwmCounterName">The name of the counter to use</param>
        /// <param name="pwmCounterOutput">The pin on which to generate the output</param>
        public PWMResistiveHeater(string deviceName, string pwmCounterName, string pwmCounterOutput)
        {
            MinOutput = 0.001;
            //By default limit maximum duty cycle of this
            //heater to 50%
            MaxOutput = 0.5;
            //Set default generation frequency to 500 Hz
            _pwmSignal = new CODataFrequency(500, MinOutput);
            string connection = deviceName + "/" + pwmCounterName;
            //create our pwm task, set up a counter channelr on the output pin and configure it to run until stopped
            _pwmTask = new Task("ResistiveHeater_" + deviceName + pwmCounterName);
            _pwmTask.COChannels.CreatePulseChannelFrequency(connection, "", COPulseFrequencyUnits.Hertz, COPulseIdleState.Low, 0, _pwmSignal.Frequency, _pwmSignal.DutyCycle);
            _pwmTask.Timing.ConfigureImplicit(SampleQuantityMode.ContinuousSamples);
            _pwmTask.ExportSignals.CounterOutputEventOutputTerminal = pwmCounterOutput;
            //create writer
            _pwmWriter = new CounterSingleChannelWriter(_pwmTask.Stream);
            //start task
            _pwmTask.Start();
            _pwmWriter.WriteSingleSample(false, _pwmSignal);
        }

        #region Methods

        protected override void RealizeOutput()
        {
            if (IsDisposed)
                throw new ObjectDisposedException("PWMResistiveHeater");
            if (!IsRunning)
                return;
            _pwmSignal.DutyCycle = Output;
            _pwmWriter.WriteSingleSample(false, _pwmSignal);
        }

        public override void Stop()
        {
            //simply reduce duty cycle to almost 0
            _pwmSignal.DutyCycle = 0.001;
            _pwmWriter.WriteSingleSample(false, _pwmSignal);
            IsRunning = false;
        }

        public override void Start()
        {
            //all initialization already performed in the constructor
            //only switch running state and go to configured minimum output
            _pwmSignal.DutyCycle = MinOutput;
            _pwmWriter.WriteSingleSample(false, _pwmSignal);
            IsRunning = true;
        }

        #endregion

        #region IDisposable

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (_pwmTask != null)
            {
                _pwmTask.Dispose();
                _pwmTask = null;
            }
        }

        #endregion
    }
}
