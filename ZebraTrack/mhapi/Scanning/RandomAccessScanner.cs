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

using ipp;

namespace MHApi.Scanning
{
    /// <summary>
    /// Indicates the success or failure of a targeting operation
    /// </summary>
    public enum HitStatus {
        /// <summary>
        /// Given point or voltage could be targeted
        /// </summary>
        Success,
        /// <summary>
        /// Given point/voltage tried to target out of range voltage
        /// </summary>
        OutOfRange,
        /// <summary>
        /// Coordinates were interpreted as voltages since
        /// lookup table absent
        /// </summary>
        NoConversion,
        /// <summary>
        /// An exception was thrown during the targeting
        /// </summary>
        Exception
    }

    /// <summary>
    /// Class to do 2D random access scanning
    /// </summary>
    public class RandomAccessScanner : IDisposable
    {
        #region Members

        /// <summary>
        /// The lookup table for converting coordinates
        /// into scan voltages
        /// </summary>
        PointVoltageConverter _pointVoltageConverter;

        /// <summary>
        /// The analog out task handling
        /// the mirror positions
        /// </summary>
        Task _mirrorTask;

        /// <summary>
        /// The multichannel writer to control
        /// mirror positions
        /// </summary>
        AnalogMultiChannelWriter _multiWriter;

        /// <summary>
        /// The minimum voltage
        /// </summary>
        readonly double _vMin;

        /// <summary>
        /// The maximum voltage
        /// </summary>
        readonly double _vMax;

        #endregion

        #region Construction

        /// <summary>
        /// Creates a new RandomAccessScanner writing samples to the first device
        /// using AO0 for the xMirror and AO1 for the YMirror
        /// </summary>
        public RandomAccessScanner() : this(null,"dev1/ao0","dev1/ao1",-10,10)
        {
            
        }

        /// <summary>
        /// Creates a new RandomAccessScanner writing samples to the first device
        /// using AO0 for the xMirror and AO1 for the YMirror
        /// </summary>
        /// <param name="lookupTable">The point-voltage conversion table to use</param>
        public RandomAccessScanner(PointVoltageConverter lookupTable) : this()
        {
            if (!lookupTable.Complete)
                throw new ArgumentException("The provided lookup table has to be complete");
            CoordinateConverter = lookupTable;
        }

        /// <summary>
        /// Creates a new RandomAccessScanner writing samples to the indicated channels
        /// </summary>
        /// <param name="lookupTable">The point-voltage conversion table to use</param>
        /// <param name="xChannel">The device and ao-channel for the x-mirror</param>
        /// <param name="yChannel">The device and ao-channel for the y-mirror</param>
        /// <param name="vMin">The minimum of the voltage range used on the Ao channels</param>
        /// <param name="vMax">The maximum of the voltage range used on the Ao channels</param>
        public RandomAccessScanner(PointVoltageConverter lookupTable, string xChannel, string yChannel, double vMin, double vMax)
        {
            if (! (vMin < vMax))
                throw new ArgumentException("Minimum voltage has to be smaller than maximum voltage");
            if (vMin < -10 || vMax > 10)
                throw new ArgumentException("The minimum voltage can't be below -10 and the maximum voltage can't be above 10");

            _vMin = vMin;
            _vMax = vMax;
            //_xMirrorTask = new Task("AOXMirror");
            //_xMirrorTask.AOChannels.CreateVoltageChannel(xChannel, "XOut", vMin, vMax, AOVoltageUnits.Volts);
            //_xWriter = new AnalogSingleChannelWriter(_xMirrorTask.Stream);
            //_yMirrorTask = new Task("AOYMirror");
            //_yMirrorTask.AOChannels.CreateVoltageChannel(yChannel, "YOut", vMin, vMax, AOVoltageUnits.Volts);
            //_yWriter = new AnalogSingleChannelWriter(_yMirrorTask.Stream);
            _mirrorTask = new Task("AOMirror");
            _mirrorTask.AOChannels.CreateVoltageChannel(xChannel, "XOut", vMin, vMax, AOVoltageUnits.Volts);
            _mirrorTask.AOChannels.CreateVoltageChannel(yChannel, "YOut", vMin, vMax, AOVoltageUnits.Volts);
            _multiWriter = new AnalogMultiChannelWriter(_mirrorTask.Stream);
            if (lookupTable!=null && !lookupTable.Complete)
                throw new ArgumentException("The provided lookup table has to be complete");
            CoordinateConverter = lookupTable;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The lookup table for converting x/y positions
        /// into scan-mirror angles
        /// </summary>
        public PointVoltageConverter CoordinateConverter
        {
            get
            {
                return _pointVoltageConverter;
            }
            set
            {
                if (value!=null && !value.Complete)
                    throw new ArgumentException("Lookup table has to be complete");
                _pointVoltageConverter = value;
            }
        }

        /// <summary>
        /// The minimum of the scan voltage range
        /// </summary>
        public double VMin
        {
            get
            {
                return _vMin;
            }
        }

        /// <summary>
        /// The maximum of the scan voltage range
        /// </summary>
        public double VMax
        {
            get
            {
                return _vMax;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Tries to target a given coordinate
        /// </summary>
        /// <param name="coordinate">The 2D point coordinate which should be targeted</param>
        /// <returns>The status code of the targeting attempt</returns>
        public HitStatus Hit(IppiPoint coordinate)
        {
            if (CoordinateConverter == null)
            {
                //No lookup table present. Try to interpret coordinates as raw voltages
                if (coordinate.x < VMin || coordinate.y < VMin || coordinate.x > VMax || coordinate.y > VMax)
                    return HitStatus.OutOfRange;
                //_xWriter.WriteSingleSample(true, coordinate.x);
                //_yWriter.WriteSingleSample(true, coordinate.y);
                double[] voltages = new double[2];
                voltages[0] = coordinate.x;
                voltages[1] = coordinate.y;
                _multiWriter.WriteSingleSample(true, voltages);
                return HitStatus.NoConversion;
            }
            return Hit(CoordinateConverter[coordinate]);             
        }

        /// <summary>
        /// Rotates the mirrors according to given voltages
        /// </summary>
        /// <param name="voltages">The x/y voltages to apply to the servo controllers</param>
        /// <returns>The status code of the targeting attempt</returns>
        public HitStatus Hit(IppiPoint_32f voltages)
        {
            if (voltages.x < VMin || voltages.y < VMin || voltages.x > VMax || voltages.y > VMax)
                return HitStatus.OutOfRange;
            try
            {
                //_xWriter.WriteSingleSample(true, voltages.x);
                //_yWriter.WriteSingleSample(true, voltages.y);
                double[] volts = new double[2];
                volts[0] = voltages.x;
                volts[1] = voltages.y;
                _multiWriter.WriteSingleSample(true, volts);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                return HitStatus.Exception;
            }
            return HitStatus.Success;
        }

        /// <summary>
        /// Tests the connection with the mirror setup reading set values
        /// back from dev1 ai0 (for x-mirror) and dev1 ai1 (for y-mirror)
        /// and default ai scale factor of 0.75
        /// </summary>
        /// <returns>True if successfull</returns>
        public bool TestConnection()
        {
            return TestConnection("dev1", "ai0", "ai1", 0.75);
        }

        /// <summary>
        /// Tests the connection with the mirror setup reading position
        /// values back from the indicated analog in channels on the indicated device
        /// </summary>
        /// <param name="aiDevice">The NI board to connect to (f.e. "dev1")</param>
        /// <param name="aiX">The analog in that reads x-mirror position (f.e. "ai0")</param>
        /// <param name="aiY">The analog in that reads y-mirror position (f.e. "ai1")</param>
        /// <param name="scaleFactor">The scale factor between supplied ao-voltage and the corresponding ai-voltage supplied by the breakout board (scale = V(ai)/V(a0))</param>
        /// <returns>True if successfull</returns>
        public bool TestConnection(string aiDevice, string aiX, string aiY, double scaleFactor)
        {
            string connectX = aiDevice + '/' + aiX;
            string connectY = aiDevice + '/' + aiY;
            Task mirrorPosTask = new Task("mirrorRead");
            var samples = new double[2, 100];//in each read we read 10 samples from 2 analog ins (mirrorX and mirrorY)
            double avgx, avgy;//the averages of the read voltages for both mirrors
            mirrorPosTask.AIChannels.CreateVoltageChannel(connectX, "ChanXMirror", AITerminalConfiguration.Differential, -10, 10, AIVoltageUnits.Volts);
            mirrorPosTask.AIChannels.CreateVoltageChannel(connectY, "ChanYMirror", AITerminalConfiguration.Differential, -10, 10, AIVoltageUnits.Volts);
            mirrorPosTask.Timing.ConfigureSampleClock("", 100, SampleClockActiveEdge.Rising, SampleQuantityMode.FiniteSamples, 100);
            AnalogMultiChannelReader aiReader = new AnalogMultiChannelReader(mirrorPosTask.Stream);
            //hit first point
            IppiPoint_32f target = new IppiPoint_32f(-5, -5);
            Hit(target);
            System.Threading.Thread.Sleep(10);//give mirrors time to settle
            mirrorPosTask.Start();
            while (mirrorPosTask.Stream.AvailableSamplesPerChannel < 100) { }//wait until all samples are acquired
            //read mirror position back
            samples = aiReader.ReadMultiSample(100);
            mirrorPosTask.Stop();
            avgx = avgy = 0;
            for (int i = 0; i < 100; i++)
            {
                avgx += (samples[0, i] / 100);
                avgy += (samples[1, i] / 100);
            }
            if (Math.Round(avgx/scaleFactor) != -5 || Math.Round(avgy/scaleFactor) != -5)
            {
                mirrorPosTask.Dispose();
                return false;
            }
            //hit second point
            target.x = 5;
            target.y = 5;
            Hit(target);
            System.Threading.Thread.Sleep(10);//give mirrors time to settle
            mirrorPosTask.Start();
            while (mirrorPosTask.Stream.AvailableSamplesPerChannel < 100) { }//wait until all samples are acquired
            //read mirror position back
            samples = aiReader.ReadMultiSample(100);
            mirrorPosTask.Stop();
            avgx = avgy = 0;
            for (int i = 0; i < 100; i++)
            {
                avgx += (samples[0, i] / 100);
                avgy += (samples[1, i] / 100);
            }
            if (Math.Round(avgx/scaleFactor) != 5 || Math.Round(avgy/scaleFactor) != 5)
            {
                mirrorPosTask.Dispose();
                return false;
            }
            mirrorPosTask.Dispose();
            return true;
        }

        #endregion

        #region Cleanup

        public bool IsDisposed { get; private set; }

        protected virtual void Dispose(bool Disposing)
        {
            /*if (_xMirrorTask != null)
                _xMirrorTask.Dispose();
            if (_yMirrorTask != null)
                _yMirrorTask.Dispose();*/
            if (_mirrorTask != null)
            {
                _mirrorTask.Dispose();
                _mirrorTask = null;
            }
            if (_multiWriter != null)
            {
                _multiWriter = null;
            }
            if (!Disposing)//stuff we don't want to do in a finalizer
            {
            }
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;
            Dispose(true);
            IsDisposed = true;
        }

        ~RandomAccessScanner()
        {
            if (!IsDisposed)
            {
                Dispose(false);
                System.Diagnostics.Debug.WriteLine("RandomAccessScanner was not properly disposed.");
            }
        }

        #endregion
    }
}
