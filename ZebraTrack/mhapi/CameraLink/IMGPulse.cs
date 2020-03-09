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

using MHApi.CameraLink.Imports;

namespace MHApi.CameraLink
{
    /// <summary>
    /// Represents a camera link board pulse
    /// that can be triggered from and emitted
    /// on various lines
    /// </summary>
    public class IMGPulse : IDisposable
    {
        #region Fields

        /// <summary>
        /// The id of the generated pulse
        /// </summary>
        uint _plsId;

        #endregion

        

        #region Constructor

        /// <summary>
        /// Creates a new pulse that is triggered based on an external line
        /// </summary>
        /// <param name="timeBase">The timebase of the pulse</param>
        /// <param name="delay">The ticks of the off-phase</param>
        /// <param name="width">The ticks of the on-phase</param>
        /// <param name="signalType">The type of trigger signal line</param>
        /// <param name="signalIdentifier">The trigger signal line identifier</param>
        /// <param name="signalPolarity">The polarity of the trigger signal</param>
        /// <param name="outputType">The type of pulse output line</param>
        /// <param name="outputNumber">The output line identifier</param>
        /// <param name="outputPolarity">The polarity of the on-phase</param>
        /// <param name="pulseMode">The pulse generation mode</param>
        public IMGPulse(PulseTimebase timeBase, uint delay, uint width, IMG_SIGNAL_TYPE signalType, uint signalIdentifier, TriggerPolarity signalPolarity, IMG_SIGNAL_TYPE outputType, uint outputNumber, PulsePolarity outputPolarity, PulseMode pulseMode)
        {
            NIImaq.CheckError(NIImaq.imgPulseCreate2(timeBase, delay, width, signalType, signalIdentifier, signalPolarity, outputType, outputNumber, outputPolarity, pulseMode, ref _plsId));
        }

        /// <summary>
        /// Creates a new pulse that is triggered based on an internal event/signal
        /// </summary>
        /// <param name="timeBase">The timebase of the pulse</param>
        /// <param name="delay">The ticks of the off-phase</param>
        /// <param name="width">The ticks of the on-phase</param>
        /// <param name="signalType">The type of trigger signal line</param>
        /// <param name="triggerEvent">The internal trigger event</param>
        /// <param name="signalPolarity">The polarity of the trigger signal</param>
        /// <param name="outputType">The type of pulse output line</param>
        /// <param name="outputNumber">The output line identifier</param>
        /// <param name="outputPolarity">The polarity of the on-phase</param>
        /// <param name="pulseMode">The pulse generation mode</param>
        public IMGPulse(PulseTimebase timeBase, uint delay, uint width, IMG_SIGNAL_TYPE signalType, InternalSignalIdentifier triggerEvent, TriggerPolarity signalPolarity, IMG_SIGNAL_TYPE outputType, uint outputNumber, PulsePolarity outputPolarity, PulseMode pulseMode)
        {
            if (signalType != IMG_SIGNAL_TYPE.IMG_SIGNAL_STATUS)
            {
                throw new ArgumentException("signalType","If an internal event is specified to trigger pulse generation, the signal type must be IMG_SIGNAL_STATUS!");
            }
            NIImaq.CheckError(NIImaq.imgPulseCreate2(timeBase, delay, width, signalType, triggerEvent, signalPolarity, outputType, outputNumber, outputPolarity, pulseMode, ref _plsId));
        }



        #endregion

        #region Methods

        /// <summary>
        /// Arms a triggered pulse to be run on the
        /// configured session
        /// </summary>
        /// <param name="sessionId">The session to which the pulse should be associated</param>
        public void Start(uint sessionId)
        {
            NIImaq.CheckError(NIImaq.imgPulseStart(_plsId, sessionId));
        }

        /// <summary>
        /// Stops pulse generation
        /// </summary>
        public void Stop()
        {
            NIImaq.CheckError(NIImaq.imgPulseStop(_plsId));
        }

        #endregion

        #region Cleanup

        public bool IsDisposed { get; private set; }

		protected virtual void Dispose(bool disposing)
		{
			if (IsDisposed)
				return;
            if (NIImaq.imgPulseDispose(_plsId) != 0)
                System.Diagnostics.Debug.WriteLine("Could not dispose pulse");
		}

		public void Dispose()
		{
			Dispose(true);
            IsDisposed = true;
		}

		~IMGPulse()
		{
			if (!IsDisposed)
            {
                Dispose(false);
			}
		}

		#endregion
    }
}
