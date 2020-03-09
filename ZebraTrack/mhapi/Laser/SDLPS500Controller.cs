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
        /// out of the control voltage every time (since this req.
        /// square-rooting). So we cash power values everytime they
        /// are set and invalidate the cash when the _controlOutput
        /// is et directly
        /// </summary>
        double _powerCashed;

        /// <summary>
        /// Indicates validity of our power cash
        /// </summary>
        bool _powerCashValid;


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
                    if (co > 0.8778)//because of the control voltage restriction an output voltage <1V is way out of the usable domain anyways
                        _powerCashed = -4.6498e3 + Math.Sqrt((co - 0.8778) / 2.184e-8);
                    else
                        _powerCashed = 0;
                    if (_powerCashed < 0)
                        _powerCashed = 0;
                    _powerCashValid = true;
                    return _powerCashed;
                }
            }
            set
            {
                if (value > 8000 || value < 0)
                    throw new ArgumentOutOfRangeException("The requested power has to be btw. 0 and 8000 mW");
                double co;
                if (value == 0)
                    co = 0;
                else
                    co = 2.184E-8 * value * value + 0.0002031 * value + 1.35;
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
