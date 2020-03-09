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

namespace MHApi.NIControl
{
    /// <summary>
    /// Encapsulates a NI Digital Out task for square pulse generation on port0
    /// Also has static triggering capabilities
    /// </summary>
    public class TriggeredPWMTask : IDisposable
    {
        Task _doTask;
        DigitalSingleChannelWriter _doWriter;
        string _line;

        /// <summary>
        /// Construct a TriggeredPWMTask - will initialize trigger infrastructure with default values if not done so yet
        /// </summary>
        /// <param name="line">The line on which to write the PWM packets (f.e. "Dev1/port0")</param>
        /// <param name="clockFrequency">The clock frequency to use</param>
        /// <param name="firstWrite">The first packet payload (necessary to initialize buffer!)</param>
        public TriggeredPWMTask(string line, int clockFrequency, uint[] firstWrite)
        {
            if(!TriggerInititalized)
                InitializeTrigger("Dev1", "Dev1", "port1/line0", "PFI0");
            _refcount++;
            _line = line;
            string taskname = string.Format("{0}{1}", "doTask", _refcount);
            _doTask = new Task(taskname);
            _doTask.DOChannels.CreateChannel(_line, "", ChannelLineGrouping.OneChannelForAllLines);
            _doTask.Timing.ConfigureSampleClock("", clockFrequency, SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples);
            _doTask.Stream.WriteRegenerationMode = WriteRegenerationMode.DoNotAllowRegeneration;//don't regenerate sample automatically ad infinitum
            _doTask.Stream.Timeout = -1; //never time out on a write
            _doTask.Triggers.StartTrigger.ConfigureDigitalEdgeTrigger(TriggerPin, DigitalEdgeStartTriggerEdge.Rising);
            _doWriter = new DigitalSingleChannelWriter(_doTask.Stream);
            _doWriter.WriteMultiSamplePort(false, firstWrite);
            _doTask.Start();
        }

        /// <summary>
        /// Writes the packets to the set port
        /// </summary>
        /// <param name="packets">The packets to write</param>
        public void WritePackets(uint[] packets)
        {
            _doWriter.WriteMultiSamplePort(false, packets);
        }

        /// <summary>
        /// Reset all lines/pins to 0 - called by Dispose method
        /// </summary>
        private void ResetPort()
        {
            Task reset = new Task("Reset");
            reset.DOChannels.CreateChannel(_line, "do0", ChannelLineGrouping.OneChannelForAllLines);
            DigitalSingleChannelWriter resetWriter = new DigitalSingleChannelWriter(reset.Stream);
            resetWriter.WriteSingleSamplePort(true, (uint)0);
            reset.Dispose();
        }

        #region static

        /// <summary>
        /// The task used for triggering
        /// </summary>
        static Task _triggerTask;
        /// <summary>
        /// Object to guard access to the trigger task
        /// </summary>
        static object _triggerTaskLock = new object();
        /// <summary>
        /// The streamwriter to set and reset the trigger
        /// </summary>
        static DigitalSingleChannelWriter _triggerWriter;
        /// <summary>
        /// Indicates if the trigger task has been set up
        /// </summary>
        public static bool TriggerInititalized { get; private set; }
        /// <summary>
        /// The static reference count is used to decide when to dispose the trigger task
        /// </summary>
        static int _refcount;       

        /// <summary>
        /// The device on which to recieve the trigger signal
        /// </summary>
        public static string TriggerDeviceReciever { get; private set; }

        /// <summary>
        /// The device pin on which to listen for the trigger signal
        /// </summary>
        public static string TriggerPin { get; private set; }

        /// <summary>
        /// The device from which to send the trigger
        /// </summary>
        public static string TriggerDeviceSender { get; private set; }

        /// <summary>
        /// The device port from which to send the trigger
        /// </summary>
        public static string TriggerPort { get; private set; }

        /// <summary>
        /// Initializes or reinitializes the trigger infrastructure
        /// </summary>
        /// <param name="senderDevice">The device board sending the trigger signal (f.e. "Dev1")</param>
        /// <param name="recieverDevice">The device board recieving the trigger signal (f.e. "Dev1")</param>
        /// <param name="sendPort">The port from which to send the trigger signal (f.e. "port1/line0")</param>
        /// <param name="recievePin">The pin to listen on for the trigger signal (f.e. "PFI0")</param>
        public static void InitializeTrigger(string senderDevice, string recieverDevice, string sendPort, string recievePin)
        {
            if (_refcount > 0)
                throw new ApplicationException("Can't reinitialize trigger. Dispose all PWMTask objects first.");
            TriggerDeviceSender = senderDevice;
            TriggerDeviceReciever = recieverDevice;
            TriggerPin = recievePin;
            TriggerPort = sendPort;
            lock (_triggerTaskLock)
            {
                if (_triggerTask != null)
                    _triggerTask.Dispose();
                _triggerTask = new Task("TriggerAll");
                string line = TriggerDeviceSender.Trim('/') + '/' + TriggerPort.Trim('/');
                _triggerTask.DOChannels.CreateChannel(line, "", ChannelLineGrouping.OneChannelForEachLine);
                _triggerTask.Stream.Timeout = -1;
                _triggerWriter = new DigitalSingleChannelWriter(_triggerTask.Stream);
            }
            TriggerInititalized = true;
        }

        /// <summary>
        /// Set the trigger by writing a high on the trigger line
        /// </summary>
        public static void TriggerAll()
        {
            if (!TriggerInititalized)
                throw new ApplicationException("Trigger not initialized. Create PWMTask object first or call InitializeTrigger.");
            System.Diagnostics.Debug.WriteLineIf(_refcount < 1, "Triggering but no PWMTaskobject was created!");
            lock(_triggerTaskLock)
                _triggerWriter.WriteSingleSamplePort(true, 1);
        }

        /// <summary>
        /// Reset the trigger by writing a low on the trigger line
        /// </summary>
        public static void ResetTrigger()
        {
            if (!TriggerInititalized)
                throw new ApplicationException("Trigger not initialized. Create PWMTask object first or call InitializeTrigger.");
            lock(_triggerTaskLock)
                _triggerWriter.WriteSingleSamplePort(true, 0);
        }

        #endregion //static

        #region IDisposable

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                //Dispose _doTask and then reset the port
                if (_doTask != null)
                {
                    _doTask.Dispose();
                    ResetPort();
                }
                //decrement static refcount - if it reaches 0 we want to dispose the static trigger task
                _refcount--;
                if (_refcount < 1)
                {
                    lock (_triggerTaskLock)
                    {
                        if (_triggerTask != null)
                        {
                            //Make sure we leave trigger in low state
                            if(TriggerInititalized)
                                ResetTrigger();
                            _triggerTask.Dispose();
                            _triggerTask = null;
                            _triggerWriter = null;
                            TriggerInititalized = false;
                        }
                    }
                }
                IsDisposed = true;
            }
        }

        #endregion //IDisposable
    }
}
