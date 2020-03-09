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

using NationalInstruments.DAQmx;

namespace MHApi.Laser
{
    /// <summary>
    /// Class to control a Thorlabs LDC240xx laser controller
    /// via a national instruments board 
    /// </summary>
    public class ThorlabsLDC240Controller : IDisposable
    {
        /// <summary>
        /// Indicates whether the laser is on or off (TTL control)
        /// </summary>
        bool _laserOn;

        /// <summary>
        /// The percentage of the maximum driver current
        /// to supply to the laser
        /// </summary>
        double _current;

        /// <summary>
        /// The task linked to the digital out channel
        /// that switches the laser on or off
        /// </summary>
        Task _onOffTask;

        /// <summary>
        /// The single channel writer for switching
        /// the laser's on/off status
        /// </summary>
        DigitalSingleChannelWriter _onOffWriter;

        /// <summary>
        /// The task linked to the analog out channel
        /// that switches the laser on or off
        /// </summary>
        Task _currentTask;

        /// <summary>
        /// The single channel writer for changing
        /// the laser current
        /// </summary>
        AnalogSingleChannelWriter _currentWriter;

        /// <summary>
        /// Used to switch laser output on or off
        /// </summary>
        public bool LaserOn
        {
            get
            {
                return _laserOn;
            }
            set
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(this.ToString());
                _laserOn = value;
                _onOffWriter.WriteSingleSampleSingleLine(true, value);
            }
        }

        /// <summary>
        /// The current, in percent of the maximal current, to supply to the laser.
        /// NOTE: This is actually an offset from the current set via the dial!
        /// </summary>
        public double PercentLaserCurrent
        {
            get
            {
                return _current;
            }
            set
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(this.ToString());
                if (value < 0 || value > 100)
                    throw new ArgumentOutOfRangeException("The current percentage has to be between 0 and 100");
                if (value != _current)
                {
                    _current = value;
                    _currentWriter.WriteSingleSample(true, _current/10.0);
                }
            }
        }


        public ThorlabsLDC240Controller(string device, string aoCurrent, string doOnOff)
        {
            _laserOn = false;
            _current = 0;
            //Set up tasks and channel writers
            _onOffTask = new Task("LaserOnOff");
            _onOffTask.DOChannels.CreateChannel(device + "/" + doOnOff, "OnOff", ChannelLineGrouping.OneChannelForAllLines);
            _onOffWriter = new DigitalSingleChannelWriter(_onOffTask.Stream);
            _currentTask = new Task("LaserCurrent");
            _currentTask.AOChannels.CreateVoltageChannel(device + "/" + aoCurrent, "Current", 0, 10, AOVoltageUnits.Volts);
            _currentWriter = new AnalogSingleChannelWriter(_currentTask.Stream);
            //Set current to 0
            _currentWriter.WriteSingleSample(true, 0);
            //The laser controller only reacts to edges on the TTL line - so to gain control briefly switch on, then off
            _onOffWriter.WriteSingleSampleSingleLine(true, true);
            Thread.Sleep(100);
            _onOffWriter.WriteSingleSampleSingleLine(true, false);
        }

        #region IDisposable

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (IsDisposed)
                return;
            if (_onOffTask != null)
            {
                _onOffWriter.WriteSingleSampleSingleLine(true, false);
                _onOffTask.Dispose();
                _onOffTask = null;
            }
            if (_currentTask != null)
            {
                _currentWriter.WriteSingleSample(true, 0);
                _currentTask.Dispose();
                _currentTask = null;
            }
            IsDisposed = true;
        }

        #endregion
    }
}
