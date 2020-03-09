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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;
using System.ComponentModel;
//using IMAQViewer.Utilities;
using NationalInstruments.DAQmx;
using MHApi.Threading;
using MHApi.Utilities;

namespace MHApi.DrewsClasses {
    public class PCIe6323 : IDisposable {

        public enum AIReadTriggerType { None, AO, External };

        string _deviceName;

        /// <summary>
        /// Determines whether and how ai-reading will depend on a trigger signal
        /// Having this static is fine as long as there is only one board maximum in use!!
        /// </summary>
        public static AIReadTriggerType AiReadTrigger;

        public PCIe6323(string deviceName, AIReadTriggerType aiReadTrigger) {
            AI = new AIType(deviceName);
            AO = new AOType(deviceName);
            DO = new DOType(deviceName);
            CO = new COType(deviceName);
            _deviceName = deviceName;
            AiReadTrigger = aiReadTrigger;
        }

        public AIType AI { get; private set; }
        public class AIType {
            string deviceName;
            Task streamingTask;
            Worker thread;
            double sampleRate;
            int samplesPerRead;

            /// <summary>
            /// Autoreset event gets set when the ReadThread is ready to read samples from AI
            /// </summary>
            AutoResetEvent _readReady = new AutoResetEvent(false);

            /// <summary>
            /// Autoreset event gets set when the ReadThread is ready to read samples from AI
            /// </summary>
            public AutoResetEvent ReadThreadReady
            {
                get
                {
                    return _readReady;
                }
            }

            public AIType(string deviceName) {
                this.deviceName = deviceName;
                AI0 = new AIChannelType(deviceName, 0);
                AI1 = new AIChannelType(deviceName, 1);
                AI2 = new AIChannelType(deviceName, 2);
                AI3 = new AIChannelType(deviceName, 3);
                AI4 = new AIChannelType(deviceName, 4);
                AI5 = new AIChannelType(deviceName, 5);
                AI6 = new AIChannelType(deviceName, 6);
                AI7 = new AIChannelType(deviceName, 7);
                //add additional channel definitions - also update property definitions AND definition of channels variable below!
            }

            public AIChannelType AI0 { get; private set; }
            public AIChannelType AI1 { get; private set; }
            public AIChannelType AI2 { get; private set; }
            public AIChannelType AI3 { get; private set; }
            public AIChannelType AI4 { get; private set; }
            public AIChannelType AI5 { get; private set; }
            public AIChannelType AI6 { get; private set; }
            public AIChannelType AI7 { get; private set; }

            public void Start(double sampleRate) {
                Start(sampleRate, (int)(sampleRate / 10));
            }

            /// <summary>
            /// Starts the acquisition
            /// </summary>
            /// <param name="sampleRate">Samples/second</param>
            /// <param name="samplesPerRead">Samples per read (=>nReads = sampleRate/samplesPerRead)</param>
            public void Start(double sampleRate, int samplesPerRead) {
                if (thread != null) {
                    Debug.WriteLine("Found thread not null, skipping Start()");
                    return;
                }
                this.sampleRate = sampleRate;
                this.samplesPerRead = samplesPerRead;
                thread = new Worker(ReadThreadRun, true, 5000);
            }

            public void Stop() {
                if (thread != null) thread.Dispose();
                thread = null;
                //Reset read ready signal
                _readReady.Reset();
            }

            void ReadThreadRun(AutoResetEvent stop, Dispatcher currentDispatcher) {
                try
                {
                    int i, j;
                    var channels = new[] { AI0, AI1, AI2, AI3, AI4, AI5, AI6, AI7 };//need to add additional channels here

                    //depending on our trigger setting this task will not be triggered, triggered by our TriggeredTask mechanism, or by ao generation start - see code below
                    streamingTask = new Task(deviceName + " AI Streaming Task");
                    



                    var callbacks = new List<Action<long, double[]>>();
                    for (i = 0; i < channels.Length; i++)
                    {
                        if (channels[i].Callback != null)
                        {
                            callbacks.Add(channels[i].Callback);
                            streamingTask.AIChannels.CreateVoltageChannel(deviceName + "/ai" + i, deviceName + " ai " + i, AITerminalConfiguration.Differential, channels[i].MinValue, channels[i].MaxValue, AIVoltageUnits.Volts);
                            streamingTask.Timing.ConfigureSampleClock("", sampleRate, SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples);
                        }
                    }
                    int numChannels = callbacks.Count;
                    if (numChannels == 0)
                    {
                        streamingTask.Dispose();
                        streamingTask = null;
                        return;
                    }
                    streamingTask.Stream.ConfigureInputBuffer((int)(2 * sampleRate));

                    long t0, t1, t2;
                    double stopwatchFrequency;
                    var reader = new AnalogMultiChannelReader(streamingTask.Stream);
                    //var waveforms = new AnalogWaveform<double>[numChannels];
                    var samples = new double[numChannels, samplesPerRead];
                    double[][] samplesByChannel = new double[numChannels][];
                    for (i = 0; i < numChannels; i++)
                        samplesByChannel[i] = new double[samplesPerRead];
                    int samplesRead;
                    long sampleIndex = 0;
                    double elapsed;
                    sampleIndex = 0;
                    if (AiReadTrigger == AIReadTriggerType.AO)
                    {
                        //We wait for the analog out to be ready
                        streamingTask.Triggers.StartTrigger.ConfigureDigitalEdgeTrigger("ao/StartTrigger", DigitalEdgeStartTriggerEdge.Rising);
                    }
                    else if (AiReadTrigger == AIReadTriggerType.External)
                    {
                        //We wait for trigger pulse on Digital Port 1 line 0
                        streamingTask.Triggers.StartTrigger.ConfigureDigitalEdgeTrigger("PFI0", DigitalEdgeStartTriggerEdge.Rising);
                    }


                    streamingTask.Start();

                    //Signal to other threads that we are ready to read
                    _readReady.Set();

                    while (true)
                    {
                        if (streamingTask.Stream.AvailableSamplesPerChannel >= samplesPerRead)
                        {
                            reader.MemoryOptimizedReadMultiSample(samplesPerRead, ref samples, out samplesRead);
                            //waveforms = reader.ReadWaveform(samplesPerRead);
                            t0 = Stopwatch.GetTimestamp();
                            for (i = 0; i < numChannels; i++)
                                for (j = 0; j < samplesPerRead; j++)
                                    samplesByChannel[i][j] = samples[i, j];
                            t1 = Stopwatch.GetTimestamp();
                            for (i = 0; i < numChannels; i++)
                                callbacks[i](sampleIndex, samplesByChannel[i]);
                            //callbacks[i](sampleIndex, samplesPerRead, waveforms[i].GetScaledData());
                            t2 = Stopwatch.GetTimestamp();
                            stopwatchFrequency = Stopwatch.Frequency;
                            elapsed = (t1 - t0) / stopwatchFrequency;
                            if (elapsed > 0.05)
                                Debug.WriteLine("Copy operation took " + elapsed + " ms");
                            elapsed = (t2 - t1) / stopwatchFrequency;
                            if (elapsed > 0.2)
                                Debug.WriteLine("Callbacks took " + elapsed + " ms - try to make them faster and/or transfer work to another thread");
                            sampleIndex += samplesPerRead;
                        }
                        else if (stop.WaitOne(100))
                            break;
                    }
                } /*catch (Exception e) {
                    Debug.WriteLine(e.Message);
                    Debug.WriteLine(e.StackTrace);
                }*/
                finally
                {
                    if (streamingTask != null)
                    {
                        streamingTask.Stop();
                        streamingTask.Dispose();
                    }
                }
                streamingTask = null;
            }

            public class AIChannelType : PropertyChangeNotification {
                readonly string deviceName;
                readonly int channelIndex;
                Task channelTask;
                AnalogSingleChannelReader channelReader;

                public AIChannelType(string deviceName, int channelIndex) {
                    this.deviceName = deviceName;
                    this.channelIndex = channelIndex;
                    minValue = -10;
                    maxValue = 10;
                }

                double minValue;
                public double MinValue {
                    get { return minValue; }
                    set {
                        minValue = value;
                        channelReader = null;
                        if (channelTask != null) channelTask.Dispose();
                        channelTask = null;
                        RaisePropertyChanged("MinValue");
                    }
                }

                double maxValue;
                public double MaxValue {
                    get { return maxValue; }
                    set {
                        maxValue = value;
                        channelReader = null;
                        if (channelTask != null) channelTask.Dispose();
                        channelTask = null;
                        RaisePropertyChanged("MaxValue");
                    }
                }

                public double Value {
                    get {
                        double value;
                        if (channelReader == null) {
                            channelTask = new Task(deviceName + " AI" + channelIndex + " Static Task");
                            channelTask.AIChannels.CreateVoltageChannel(deviceName + "/ai" + channelIndex, "ai" + channelIndex, AITerminalConfiguration.Differential, MinValue, MaxValue, AIVoltageUnits.Volts);
                            channelReader = new AnalogSingleChannelReader(channelTask.Stream);
                        }
                        value = channelReader.ReadSingleSample();
                        channelTask.Stop();
                        return value;
                    }
                }

                public Action<long, double[]> Callback { get; set; }
            }//class AIChannelType
        }

        #region AnalogOut

        public AOType AO { get; private set; }
        public class AOType {
            string deviceName;

            public AOType(string deviceName) {
                this.deviceName = deviceName;
                AO0 = new AOChannelType(deviceName, 0);
                AO1 = new AOChannelType(deviceName, 1);
                AO2 = new AOChannelType(deviceName, 2);
                AO3 = new AOChannelType(deviceName, 3);
            }

            public AOChannelType AO0 { get; private set; }
            public AOChannelType AO1 { get; private set; }
            public AOChannelType AO2 { get; private set; }
            public AOChannelType AO3 { get; private set; }

            public class AOChannelType : PropertyChangeNotification {
                readonly string deviceName;
                readonly int channelIndex;
                Task channelTask;
                AnalogSingleChannelWriter channelWriter;

                public AOChannelType(string deviceName, int channelIndex) {
                    this.deviceName = deviceName;
                    this.channelIndex = channelIndex;
                    minValue = 0;
                    maxValue = 5;
                }

                double minValue;
                public double MinValue {
                    get { return minValue; }
                    set {
                        minValue = value;
                        channelWriter = null;
                        if (channelTask != null) channelTask.Dispose();
                        channelTask = null;
                        RaisePropertyChanged("MinValue");
                    }
                }

                double maxValue;
                public double MaxValue {
                    get { return maxValue; }
                    set {
                        maxValue = value;
                        channelWriter = null;
                        if (channelTask != null) channelTask.Dispose();
                        channelTask = null;
                        RaisePropertyChanged("MaxValue");
                    }
                }

                double v;
                public double Value {
                    get { return v; }
                    set {
                        v = value;
                        if (channelWriter == null) {
                            channelTask = new Task(deviceName + " AO" + channelIndex + " Static Task");
                            channelTask.AOChannels.CreateVoltageChannel(deviceName + "/ao" + channelIndex, "ao" + channelIndex, MinValue, MaxValue, AOVoltageUnits.Volts);
                            channelWriter = new AnalogSingleChannelWriter(channelTask.Stream);
                        }
                        channelWriter.WriteSingleSample(true, v);
                        channelTask.Stop();
                        RaisePropertyChanged("Value");
                    }
                }
            }//class AOChannelType
        }//class AOType

        #endregion

        #region DigitalOut

        public DOType DO { get; private set; }
        public class DOType {
            string deviceName;

            public DOType(string deviceName) {
                this.deviceName = deviceName;
                Port0 = new Port0Type(deviceName);
                Port1 = new Port1Type(deviceName);
                Port2 = new Port1Type(deviceName);
            }

            public Port0Type Port0 { get; private set; }
            public Port1Type Port1 { get; private set; }
            public Port1Type Port2 { get; private set; }

            public class Port0Type {
                string deviceName;

                public Port0Type(string deviceName) {
                    this.deviceName = deviceName;
                    Line0 = new Port0LineType(deviceName, 0);
                    Line1 = new Port0LineType(deviceName, 1);
                    Line2 = new Port0LineType(deviceName, 2);
                    Line3 = new Port0LineType(deviceName, 3);
                    Line4 = new Port0LineType(deviceName, 4);
                    Line5 = new Port0LineType(deviceName, 5);
                    Line6 = new Port0LineType(deviceName, 6);
                    Line7 = new Port0LineType(deviceName, 7);
                    //create more to be able to access more lines
                }

                public Port0LineType Line0 { get; private set; }
                public Port0LineType Line1 { get; private set; }
                public Port0LineType Line2 { get; private set; }
                public Port0LineType Line3 { get; private set; }
                public Port0LineType Line4 { get; private set; }
                public Port0LineType Line5 { get; private set; }
                public Port0LineType Line6 { get; private set; }
                public Port0LineType Line7 { get; private set; }
                //create more to be able to access more lines

                public class Port0LineType : PropertyChangeNotification {
                    readonly string deviceName;
                    readonly int lineIndex;
                    Task lineTask;
                    DigitalSingleChannelWriter lineWriter;

                    public Port0LineType(string deviceName, int lineIndex) {
                        this.deviceName = deviceName;
                        this.lineIndex = lineIndex;
                        lineTask = new Task(deviceName + "Port0 line" + lineIndex + " Static Task");
                        lineTask.DOChannels.CreateChannel(deviceName + "/port0/line" + lineIndex, "port0line" + lineIndex, ChannelLineGrouping.OneChannelForAllLines);
                        lineWriter = new DigitalSingleChannelWriter(lineTask.Stream);
                    }

                    bool v;
                    public bool Value {
                        get { return v; }
                        set { v = value; lineWriter.WriteSingleSampleSingleLine(true, v); lineTask.Stop(); RaisePropertyChanged("Value"); }
                    }
                }//class Port0LineType
            }//class Port0Type

            #region DO/Port1

            public class Port1Type {
                string deviceName;

                public Port1Type(string deviceName) {
                    this.deviceName = deviceName;
                }

                Port1LineType line0;
                public Port1LineType Line0 { get { return line0 ?? (line0 = new Port1LineType(deviceName, 0)); } }
                Port1LineType line1;
                public Port1LineType Line1 { get { return line1 ?? (line1 = new Port1LineType(deviceName, 1)); } }
                Port1LineType line2;
                public Port1LineType Line2 { get { return line2 ?? (line2 = new Port1LineType(deviceName, 2)); } }
                Port1LineType line3;
                public Port1LineType Line3 { get { return line3 ?? (line3 = new Port1LineType(deviceName, 3)); } }
                Port1LineType line4;
                public Port1LineType Line4 { get { return line0 ?? (line4 = new Port1LineType(deviceName, 4)); } }
                Port1LineType line5;
                public Port1LineType Line5 { get { return line1 ?? (line5 = new Port1LineType(deviceName, 5)); } }
                Port1LineType line6;
                public Port1LineType Line6 { get { return line2 ?? (line6 = new Port1LineType(deviceName, 6)); } }
                Port1LineType line7;
                public Port1LineType Line7 { get { return line3 ?? (line7 = new Port1LineType(deviceName, 7)); } }

                public class Port1LineType : PropertyChangeNotification {
                    Task lineTask;
                    DigitalSingleChannelWriter lineWriter;

                    public Port1LineType(string deviceName, int lineIndex) {
                        lineTask = new Task(deviceName + " Port1 line" + lineIndex + " Static Task");
                        lineTask.DOChannels.CreateChannel(deviceName + "/port1/line" + lineIndex, "port1line" + lineIndex, ChannelLineGrouping.OneChannelForAllLines);
                        lineWriter = new DigitalSingleChannelWriter(lineTask.Stream);
                    }

                    bool v;
                    public bool Value {
                        get { return v; }
                        set { v = value; lineWriter.WriteSingleSampleSingleLine(true, v); lineTask.Stop(); RaisePropertyChanged("Value"); }
                    }
                }//class Port1LineType

            }//class Port1Type

            #endregion //DO/Port1
        }//class DOType

        #endregion//DigitalOut

        #region Counter

        public COType CO { get; private set; }

        public class COType {
            readonly string deviceName;

            public COType(string deviceName) {
                this.deviceName = deviceName;
                CO0 = new COChannelType(deviceName, 0);
                CO1 = new COChannelType(deviceName, 1);
                CO2 = new COChannelType(deviceName, 2);
                CO3 = new COChannelType(deviceName, 3);
            }

            public COChannelType CO0 { get; private set; }
            public COChannelType CO1 { get; private set; }
            public COChannelType CO2 { get; private set; }
            public COChannelType CO3 { get; private set; }

            public class COChannelType{
                string deviceName;
                readonly int channelIndex;

                public COChannelType(string deviceName, int channelIndex) {
                    this.deviceName = deviceName;
                    this.channelIndex = channelIndex;

                }

                public void Pulse(double durationInSeconds) {
                    var task = new Task();
                    task.COChannels.CreatePulseChannelTime(deviceName + "/ctr" + channelIndex, "ctr" + channelIndex, COPulseTimeUnits.Seconds, COPulseIdleState.Low, 0, 1, durationInSeconds);
                    task.Start();
                    task.WaitUntilDone();
                    task.Stop();
                    task.Dispose();
                }
            }//class COChannelType
        }//class COType

        #endregion

        public void Dispose() {
            AI.Stop();
        }
    }
}