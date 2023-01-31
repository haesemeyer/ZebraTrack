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

namespace MHApi.Laser
{

    /// <summary>
    /// Class to control a Shanghai Dream Laser
    /// SDLPS500 driver via a national instruments board 
    /// </summary>
    public class SDLPS500Controller : IDisposable
    {
        
        /// <summary>
        /// The control output
        /// to send to the laser
        /// </summary>
        double _controlOutput;

        /// <summary>
        /// The task linked to the analog out channel
        /// that controls the laser output
        /// </summary>
        Task _aoTask;

        /// <summary>
        /// The single channel writer for changing
        /// the laser output
        /// </summary>
        AnalogSingleChannelWriter _aoWriter;

        /// <summary>
        /// For efficiency we don't want to calculate the power
        /// out of the control voltage every time (since this requires a tree search).
        /// So we cash power values everytime they
        /// are set and invalidate the cash when the _controlOutput
        /// is et directly
        /// </summary>
        double _powerCashed;

        /// <summary>
        /// Indicates validity of our power cash
        /// </summary>
        bool _powerCashValid;

        /// <summary>
        /// Interpolated lookup to convert from power at sample to control voltage
        /// and vice versa
        /// </summary>
        private PowerAtSampleLookup _converter;


        /// <summary>
        /// The control output to send to the laser.
        /// NOTE: This actually only properly works if the front dial is set to 10 (max)!
        /// </summary>
        public double ControlOutput
        {
            get
            {
                return _controlOutput;
            }
            set
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(this.ToString());
                if (value < 0 || value > 5)
                    throw new ArgumentOutOfRangeException("The control output has to be between 0 and 5V");
                if (value != _controlOutput)
                {
                    _controlOutput = value;
                    _aoWriter.WriteSingleSample(true, _controlOutput);
                    _powerCashValid = false;//invalidate our cashed power value
                }
            }
        }

        /// <summary>
        /// Gets/sets the intended laser output
        /// power in mW
        /// </summary>
        public double LaserPower
        {
            get
            {
                if (_powerCashValid)
                    return _powerCashed;
                else
                {
                    var co = ControlOutput;
                    _powerCashed = _converter.GetPowerByAOV(co);
                    _powerCashValid = true;
                    return _powerCashed;
                }
            }
            set
            {
                if (value > 4000 || value < 0)
                    throw new ArgumentOutOfRangeException("The requested power at sample has to be btw. 0 and 4000 mW");
                double co;
                if (value == 0)
                    co = 0;
                else
                    co = _converter.GetAOVByPower(value);
                ControlOutput = co;
                _powerCashed = value;//cash the requested power for quick access
                _powerCashValid = true;
            }
        }


        public SDLPS500Controller(string device, string aoName)
        {
            _controlOutput = 0;
            _powerCashValid = true;
            _powerCashed = 0;
            //Set up task and channel writer
            _aoTask = new Task("LaserOutput");
            _aoTask.AOChannels.CreateVoltageChannel(device + "/" + aoName, "LasOut", 0, 5, AOVoltageUnits.Volts);
            _aoWriter = new AnalogSingleChannelWriter(_aoTask.Stream);
            //Set output to 0
            _aoWriter.WriteSingleSample(true, 0);
            //Create our converter based on power at sample measurements
            double[] aov_vals = new double[]
            {
                1.3,
                1.35,
                1.4,
                1.5,
                1.6,
                1.7,
                1.8,
                1.9,
                2,
                2.1,
                2.2,
                2.3,
                2.4,
                2.5,
                2.6,
                2.7,
                2.8,
                2.9,
                3,
                3.1,
                3.2,
                3.3,
                3.4,
                3.5,
                3.6,
                3.7,
                3.8,
                3.9,
                4,
                4.1,
                4.2
            };
            double[] pas_vals = new double[]
            {
                0,
                84,
                125,
                332,
                555,
                800,
                1030,
                1250,
                1450,
                1630,
                1775,
                1900,
                2035,
                2150,
                2280,
                2390,
                2570,
                2650,
                2720,
                2830,
                2970,
                3120,
                3220,
                3360,
                3520,
                3680,
                3810,
                3950,
                4110,
                4210,
                4330
            };
            _converter = new PowerAtSampleLookup(pas_vals, aov_vals, true);
        }

        #region IDisposable

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (IsDisposed)
                return;
            if (_aoTask != null)
            {
                _aoWriter.WriteSingleSample(true, 0);
                _aoTask.Dispose();
                _aoTask = null;
            }
            IsDisposed = true;
        }

        #endregion
    }
}
