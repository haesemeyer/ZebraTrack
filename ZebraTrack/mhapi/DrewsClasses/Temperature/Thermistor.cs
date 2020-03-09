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
using System.IO;
using System.Windows;
using System.ComponentModel;
using System.Diagnostics;
using MHApi.GUI;
using MHApi.DrewsClasses;
using MHApi.Utilities;

namespace MHApi.DrewsClasses.Temperature{

    public enum ThermistorType{
        [Description("WarnerBeadThermistorTS70B")]
        Warner = 0, 
        [Description("OmegaThermistorHSTH4403140")]
        Omega=4
    };

    public class Thermistor : PropertyChangeNotification {

        /// <summary>
        /// The temperature that is currently sensed by the thermistor
        /// </summary>
        double _temperature;

        /// <summary>
        /// The temperature that is currently sensed by the thermistor
        /// </summary>
        public double Temperature {
            get { return _temperature; }
            private set { _temperature = value; RaisePropertyChanged("Temperature"); }
        }

        /// <summary>
        /// The type of the current thermistor
        /// </summary>
        ThermistorType _type;

        /// <summary>
        /// The type of the current thermistor
        /// </summary>
        public ThermistorType Type
        {
            get
            {
                return _type;
            }
        }

        /// <summary>
        /// Constructs a new thermistor
        /// </summary>
        /// <param name="daq">The daq-board for reading the thermistor</param>
        /// <param name="channelName">The ai channel readint the thermistor bridge</param>
        /// <param name="type">The type of the thermistor</param>
        public Thermistor(PCIe6323 daq, string channelName, ThermistorType type) {
            if (ViewModelBase.IsInDesignMode) {
                Temperature = 12.3456789;
                return;
            }
            _type = type;
            if (channelName.ToLower() == "ai0") {
                daq.AI.AI0.MinValue = -5;
                daq.AI.AI0.MaxValue = 5;
                daq.AI.AI0.Callback = (sampleIndex, samples) => {
                    int length = samples.Length;
                    double sum = 0;
                    for (int i = 0; i < length; i++)
                        sum += samples[i];
                    Temperature = ConvertRToT(ConvertVToR(sum / length),_type);
                };
            }
            if (channelName.ToLower() == "ai1") {
                daq.AI.AI1.MinValue = -5;
                daq.AI.AI1.MaxValue = 5;
                daq.AI.AI1.Callback = (sampleIndex, samples) => {
                    int length = samples.Length;
                    double sum = 0;
                    for (int i = 0; i < length; i++)
                        sum += samples[i];
                    Temperature = ConvertRToT(ConvertVToR(sum / length),_type);
                };
            }
            if (channelName.ToLower() == "ai2") {
                daq.AI.AI2.MinValue = -5;
                daq.AI.AI2.MaxValue = 5;
                daq.AI.AI2.Callback = (sampleIndex, samples) => {
                    int length = samples.Length;
                    double sum = 0;
                    for (int i = 0; i < length; i++)
                        sum += samples[i];
                    Temperature = ConvertRToT(ConvertVToR(sum / length),_type);
                };
            }
            if (channelName.ToLower() == "ai3") {
                daq.AI.AI3.MinValue = -5;
                daq.AI.AI3.MaxValue = 5;
                daq.AI.AI3.Callback = (sampleIndex, samples) => {
                    int length = samples.Length;
                    double sum = 0;
                    for (int i = 0; i < length; i++)
                        sum += samples[i];
                    Temperature = ConvertRToT(ConvertVToR(sum / length),_type);
                };
            }
        }

        #region Resistance <-> Temperature

        /// <summary>
        /// Resistance of each bridge part (as well as thermistor base resistance)
        /// </summary>
        const double RBridge = 1e4;
        /// <summary>
        /// Source voltage of the thermistor bridge
        /// </summary>
        const double vS = 1.500;

        /// <summary>
        /// The temperature lookup table for Warner
        /// bead thermistors
        /// </summary>
        static double[] T_Warner;
        /// <summary>
        /// The minimum resistance of the Warner
        /// lookup table
        /// </summary>
        static int R0_Warner;
        /// <summary>
        /// The maximum resistance of the Warner
        /// lookup table
        /// </summary>
        static int RMax_Warner;

        /// <summary>
        /// The temperature lookup table for the
        /// Omega thermistor
        /// </summary>
        static double[] T_Omega;
        /// <summary>
        /// The minimum resistance of the
        /// Omega lookup table
        /// </summary>
        static int R0_Omega;
        /// <summary>
        /// The maximum resistance of the
        /// Omega lookup table
        /// </summary>
        static int RMax_Omega;


        const string RAndTUri_Warner = @"DrewsClasses\Temperature\Warner_Thermistor_R+T.txt";
        const string RAndTUri_Omega = @"DrewsClasses\Temperature\Omega_Thermistor_R+T.txt";
        
        /// <summary>
        /// Static constructor. Sets up the lookup tables relating resistances
        /// to temperatures
        /// </summary>
        static Thermistor() {
            if (ViewModelBase.IsInDesignMode) return;
            LoadWarner();
            LoadOmega();
            
        }

        /// <summary>
        /// Creates and populates the lookup table for omega thermistor
        /// </summary>
        static void LoadOmega()
        {
            //Prescan file to get size of dataset
            int numDataPoints = 0;
            try
            {
                using (var stream = File.OpenText(RAndTUri_Omega))
                {
                    while (!stream.EndOfStream)
                    {
                        stream.ReadLine();
                        numDataPoints++;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            //Read data into R[] and T[]
            int[] R = new int[numDataPoints];
            T_Omega = new double[numDataPoints];
            using (var stream = File.OpenText(RAndTUri_Omega))
            {
                string[] values;
                for (int i = 0; i < numDataPoints; i++)
                {
                    values = stream.ReadLine().Split(' ');
                    R[i] = int.Parse(values[0]);
                    T_Omega[i] = double.Parse(values[1]);
                }
            }
            //Save only R0 and RMax (for efficient lookup scheme) and verify assumption that R[i] = R[0] + i
            R0_Omega = R[0];
            RMax_Omega = R[R.Length - 1];
            for (int i = 0; i < numDataPoints; i++)
                if (R[i] != R0_Omega + i)
                    Debug.WriteLine("First column of Thermistor_R+T.txt must be sequential integers");
        }

        /// <summary>
        /// Creates and populates the lookup table for warner bead thermistor
        /// </summary>
        static void LoadWarner()
        {
            //Prescan file to get size of dataset
            int numDataPoints = 0;
            try
            {
                using (var stream = File.OpenText(RAndTUri_Warner))
                {
                    while (!stream.EndOfStream)
                    {
                        stream.ReadLine();
                        numDataPoints++;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            //Read data into R[] and T[]
            int[] R = new int[numDataPoints];
            T_Warner = new double[numDataPoints];
            using (var stream = File.OpenText(RAndTUri_Warner))
            {
                string[] values;
                for (int i = 0; i < numDataPoints; i++)
                {
                    values = stream.ReadLine().Split(' ');
                    R[i] = int.Parse(values[0]);
                    T_Warner[i] = double.Parse(values[1]);
                }
            }
            //Save only R0 and RMax (for efficient lookup scheme) and verify assumption that R[i] = R[0] + i
            R0_Warner = R[0];
            RMax_Warner = R[R.Length - 1];
            for (int i = 0; i < numDataPoints; i++)
                if (R[i] != R0_Warner + i)
                    Debug.WriteLine("First column of Thermistor_R+T.txt must be sequential integers");
        }

        /// <summary>
        /// Uses the known bridge resistances and bridge voltage
        /// to convert a Voltage read on AI into a resistance
        /// </summary>
        /// <param name="v">The measured voltage</param>
        /// <returns>The resistance of the thermistor</returns>
        public static double ConvertVToR(double v) {
            return RBridge * (vS - 2 * v) / (vS + 2 * v);
        }

        /// <summary>
        /// Uses the temperature lookup table to convert a resistance into
        /// a thermistor temperature
        /// </summary>
        /// <param name="r">The measured resistance</param>
        /// <param name="type">The type of thermistor</param>
        /// <returns>The temperature of the thermistor</returns>
        public static double ConvertRToT(double r, ThermistorType type) {
            switch (type)
            {
                case (ThermistorType.Warner):
                    if (r < R0_Warner) r = R0_Warner;
                    if (r > RMax_Warner) r = RMax_Warner;
                    return T_Warner[(int)r - R0_Warner];
                default:
                    if (r < R0_Omega) r = R0_Omega;
                    if (r > RMax_Omega) r = RMax_Omega;
                    return T_Omega[(int)r - R0_Omega];
            }
        }

        #endregion


    }
}