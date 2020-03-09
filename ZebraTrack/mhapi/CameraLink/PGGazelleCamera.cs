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

namespace MHApi.CameraLink
{
    public unsafe class PGGazelleCamera : CameraLinkCamera
    {
        /// <summary>
        /// Defines the source of trigger signals to the camera
        /// </summary>
        public enum TriggerSources
        {
            SOFT = 0,
            CC1 = 1,
            CC2 = 2,
            CC3 = 3,
            CC4 = 4,
            GPIO = 5
        }

        /// <summary>
        /// Defines the trigger mode of the camera
        /// </summary>
        public enum TriggerModes
        {
            none = 0,
            single = 1,
            bulb = 2
        }

        /// <summary>
        /// Defines whether a frame gets triggered
        /// on the rising or falling edge of a trigger signal
        /// </summary>
        public enum TriggerActivations
        {
            risingEdge = 0,
            fallingEdge = 1
        }

        #region Fields

        /// <summary>
        /// The current trigger mode of the camera
        /// </summary>
        TriggerModes _triggerMode;

        /// <summary>
        /// The source of the trigger signal
        /// </summary>
        TriggerSources _triggerSource;

        /// <summary>
        /// Whether to trigger on a rising or falling edge
        /// </summary>
        TriggerActivations _triggerActivation;

        /// <summary>
        /// The raw exposure time value
        /// </summary>
        uint _eraw;

        #endregion

        #region Properties

        /// <summary>
        /// Queries the temperature of the camera
        /// </summary>
        public string Temperature
        {
            get
            {
                SerialWrite("dt");
                SerialFlush();
                uint toRead = 225;
                return SerialRead(ref toRead);
            }
        }

        /// <summary>
        /// The current trigger mode
        /// </summary>
        public TriggerModes TriggerMode
        {
            get
            {
                return _triggerMode;
            }
            set
            {
                _triggerMode = value;
                SetTriggerMode();
            }
        }

        /// <summary>
        /// The current trigger signal source
        /// </summary>
        public TriggerSources TriggerSource
        {
            get
            {
                return _triggerSource;
            }
            set
            {
                _triggerSource = value;
                SetTriggerSource();
            }
        }


        /// <summary>
        /// The current trigger activation edge (rising/falling)
        /// </summary>
        public TriggerActivations TriggerActivation
        {
            get
            {
                return _triggerActivation;
            }
            set
            {
                _triggerActivation = value;
                SetTriggerActivation();
            }
        }

        /// <summary>
        /// The currently set raw exposure
        /// </summary>
        public uint RawExposure
        {
            get
            {
                return _eraw;
            }
            set
            {
                if (value < MinRawExposure || value > MaxRawExposure)
                    throw new ArgumentOutOfRangeException("RawExposure", "The desired raw exposure is outside the range allowed in the current video mode.");
                _eraw = value;
                SetRawExposure();
            }
        }

        /// <summary>
        /// The minimum allowed raw exposure
        /// given the current video mode
        /// </summary>
        public uint MinRawExposure
        {
            get
            {
                SerialWrite("eraw gmin");
                SerialFlush();
                uint toRead = 225;
                var s = SerialRead(ref toRead);
                return uint.Parse(s);
            }
        }

        /// <summary>
        /// The maximum allowed raw exposure
        /// given the current video mode
        /// </summary>
        public uint MaxRawExposure
        {
            get
            {
                SerialWrite("eraw gmax");
                SerialFlush();
                uint toRead = 225;
                var s = SerialRead(ref toRead);
                return uint.Parse(s);
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructs a new point grey gazelle camera link object
        /// </summary>
        /// <param name="interfaceId">The interface name. Optionally followed by ::# identifying the port - 0 based</param>
        public PGGazelleCamera(string interfaceId) : base(interfaceId)
        {
            _triggerActivation = TriggerActivations.risingEdge;
            SetTriggerActivation();
            _triggerSource = TriggerSources.SOFT;
            SetTriggerSource();
            _triggerMode = TriggerModes.none;
            SetTriggerMode();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Writes a serial setter command and checks that the camera
        /// replies with a string starting with ACK
        /// </summary>
        /// <param name="command">The setter command</param>
        /// <returns>True if successfull</returns>
        bool SetAndCheckSerialCommand(string command)
        {
            if (!command.Contains(' '))
                throw new ArgumentException("command", "Serial set commands need to have at least two parts.");
            //append termination character if necessary
            if (command[command.Length-1] != '\r')
                command += '\r';
            SerialWrite(command);
            SerialFlush();
            uint toRead = 255;
            string reply = SerialRead(ref toRead);
            if (reply.Contains("ACK:"))
                return true;
            else
            {
                System.Diagnostics.Debug.WriteLine(reply);
                return false;
            }
        }

        /// <summary>
        /// Commits the currently selected triggering mode to the camera
        /// </summary>
        void SetTriggerMode()
        {
            switch (_triggerMode)
            {
                case TriggerModes.none:
                    SetAndCheckSerialCommand("trm none");
                    break;
                case TriggerModes.bulb:
                    SetAndCheckSerialCommand("trm bulb");
                    break;
                case TriggerModes.single:
                    SetAndCheckSerialCommand("trm single");
                    break;
                default:
                    System.Diagnostics.Debug.WriteLine("Unknown trigger mode.");
                    break;
            }
        }

        /// <summary>
        /// Commits the currently selected trigger source to the camera
        /// </summary>
        void SetTriggerSource()
        {
            switch (_triggerSource)
            {
                case TriggerSources.CC1:
                    SetAndCheckSerialCommand("trsrc line1");
                    break;
                case TriggerSources.CC2:
                    SetAndCheckSerialCommand("trsrc line2");
                    break;
                case TriggerSources.CC3:
                    SetAndCheckSerialCommand("trsrc line3");
                    break;
                case TriggerSources.CC4:
                    SetAndCheckSerialCommand("trsrc line4");
                    break;
                case TriggerSources.GPIO:
                    SetAndCheckSerialCommand("trsrc GPIO0");
                    break;
                case TriggerSources.SOFT:
                    SetAndCheckSerialCommand("trsrc soft");
                    break;
                default:
                    System.Diagnostics.Debug.WriteLine("Did not recognize trigger source.");
                    break;
            }
        }

        /// <summary>
        /// Commits the currently selected trigger activation mode to the camera
        /// </summary>
        void SetTriggerActivation()
        {
            if (_triggerActivation == TriggerActivations.fallingEdge)
                SetAndCheckSerialCommand("tra fe");
            else
                SetAndCheckSerialCommand("tra re");
        }

        /// <summary>
        /// Commits the currently selected raw exposure value
        /// to the camera
        /// </summary>
        void SetRawExposure()
        {
            SetAndCheckSerialCommand(string.Format("eraw {0}", _eraw));
        }

        /// <summary>
        /// Fires the camera software trigger
        /// </summary>
        public void FireSoftwareTrigger()
        {
            SetAndCheckSerialCommand("trgsoft");
        }

        /// <summary>
        /// Resets the camera to the power up state
        /// </summary>
        public void ResetDevice()
        {
            SetAndCheckSerialCommand("dr");
        }

        #endregion

        #region Cleanup

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        #endregion
    }
}
