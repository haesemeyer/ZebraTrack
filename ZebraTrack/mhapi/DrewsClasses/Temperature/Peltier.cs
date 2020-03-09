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
using NationalInstruments.DAQmx;
using MHApi.GUI;
using MHApi.Utilities;

namespace MHApi.DrewsClasses.Temperature {
    public class Peltier : PropertyChangeNotification, IDisposable  {
        double minDutyCycle = 0.0001;
        double maxDutyCycle = 0.9999;
        public double MinDutyCycle {
            get { return minDutyCycle; }
            set { minDutyCycle = value; }
        }

        public double MaxDutyCycle {
            get { return maxDutyCycle; }
            set { maxDutyCycle = value; }
        }


        readonly Task pwmTask;
        readonly CounterSingleChannelWriter pwmWriter;

        readonly Task doHeatingPolarityTask; // HIGH = hot, LOW = cold
        readonly DigitalSingleChannelWriter doHeatingPolarityWriter;

        CODataFrequency dataFrequency;
        public double DutyCycle {
            get { return dataFrequency.DutyCycle; }
            set {
                if (IsDisposed) return;
                if (value > MaxDutyCycle)
                {
                    dataFrequency.DutyCycle = MaxDutyCycle;
                    //Debug.WriteLine("Requested dutyCycle {0} got ductyCycle{1}", value, MaxDutyCycle);
                }
                else if (value < MinDutyCycle)
                {
                    dataFrequency.DutyCycle = MinDutyCycle;
                }
                else
                    dataFrequency.DutyCycle = value;

                if (!ViewModelBase.IsInDesignMode) pwmWriter.WriteSingleSample(false, dataFrequency);
                RaisePropertyChanged("DutyCycle");
                //Debug.WriteLine(DutyCycle);
            }
        }

        PeltierModes peltierMode;
        public PeltierModes PeltierMode {
            get {
                return peltierMode;
            }
            set {
                if (IsDisposed) return;
                peltierMode = value;
                doHeatingPolarityWriter.WriteSingleSampleSingleLine(true, peltierMode == PeltierModes.Heat);//change if polarity needs to be reversed
                RaisePropertyChanged("PeltierMode");
                //System.Diagnostics.Debug.WriteLine(peltierMode);
            }
        }

        /*public string[] PeltierModes {
            get { return Enum.GetNames(typeof(PeltierMode)); }
        }*/

        /// <summary>
        /// Constructor of Peltier control class
        /// </summary>
        /// <param name="deviceName">The device name of the NI board, f.e. dev1</param>
        /// <param name="pwmCounterName">The name of the counter to use for this peltier, f.e. ctr0</param>
        /// <param name="pwmCounterOutput">The pinout of the counter (?) f.e. PFI12</param>
        /// <param name="pwmFrequency">The frequency of the PWM on the counter</param>
        /// <param name="heatingPolarityDOLine">The digital out line for the polarity switch</param>
        public Peltier(string deviceName, string pwmCounterName, string pwmCounterOutput, double pwmFrequency, string heatingPolarityDOLine) {
            if (ViewModelBase.IsInDesignMode) {
                DutyCycle = .2;
                PeltierMode = PeltierModes.Heat;
                return;
            }
            doHeatingPolarityTask = new Task();
            doHeatingPolarityTask.DOChannels.CreateChannel(deviceName + "/" + heatingPolarityDOLine, "", ChannelLineGrouping.OneChannelForAllLines);
            doHeatingPolarityWriter = new DigitalSingleChannelWriter(doHeatingPolarityTask.Stream);
            PeltierMode = PeltierModes.Heat;

            dataFrequency = new CODataFrequency(pwmFrequency, MinDutyCycle);
            pwmTask = new Task("Peltier " + deviceName + " " + pwmCounterName);
            pwmTask.COChannels.CreatePulseChannelFrequency(deviceName + "/" + pwmCounterName, "", COPulseFrequencyUnits.Hertz, COPulseIdleState.Low, 0, dataFrequency.Frequency, dataFrequency.DutyCycle);
            pwmTask.Timing.ConfigureImplicit(SampleQuantityMode.ContinuousSamples);
            pwmTask.ExportSignals.CounterOutputEventOutputTerminal = pwmCounterOutput;
            pwmWriter = new CounterSingleChannelWriter(pwmTask.Stream);
            pwmTask.Start();
            DutyCycle = 0;
        }

        #region IDisposable

        public bool IsDisposed { get; private set; }

        public void Dispose() {
            if (!pwmTask.IsDone)
                DutyCycle = 0;
            pwmTask.Stop();
            pwmTask.Dispose();
            doHeatingPolarityTask.Dispose();
            IsDisposed = true;
        }

        #endregion

    }

    public enum PeltierModes {
        Heat,
        Cool
    }


}
