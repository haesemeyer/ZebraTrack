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
using System.IO;
using ZebraTrack.Experiments;

using MHApi.GUI;

namespace ZebraTrack.ViewModels
{
    /// <summary>
    /// ViewModel for simple TrackAndWrite experiment
    /// </summary>
    class OLLRampViewModel : ViewModelBase, IExperimentVM
    {
        /// <summary>
        /// The length of the habituation phase in seconds
        /// </summary>
        int _habituationLength;

        /// <summary>
        /// The length of each ramp phase in seconds
        /// </summary>
        int _rampLength;

        /// <summary>
        /// The length of the rest phase in seconds
        /// </summary>
        int _restLength;

        /// <summary>
        /// The number of trials to run
        /// </summary>
        int _nTrials;

        /// <summary>
        /// The baseline laser power
        /// as rest and lowest ramp value in mW
        /// </summary>
        double _baseLaserPower;

        /// <summary>
        /// The ramp peak laser power in mW
        /// </summary>
        double _peakLaserPower;

        public OLLRampViewModel()
        {
            HabituationLength = 10 * 60;
            RampLength = 60;
            RestLength = RampLength * 2;
            NTrials = 10;
            BaseLaserPower = 300;
            PeakLaserPower = 2000;
        }

        /// <summary>
        /// The length of the habituation phase in seconds
        /// </summary>
        public int HabituationLength
        {
            get
            {
                return _habituationLength;
            }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(HabituationLength), "Habituation needs to be at least 1s long");
                _habituationLength = value;
                RaisePropertyChanged(nameof(HabituationLength));
            }
        }

        /// <summary>
        /// The length of each ramp phase in seconds
        /// </summary>
        public int RampLength
        {
            get
            {
                return _rampLength;
            }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(RampLength), "Ramp needs to be at least 1s long");
                _rampLength = value;
                RaisePropertyChanged(nameof(RampLength));
            }
        }

        /// <summary>
        /// The length of the rest phase in seconds
        /// </summary>
        public int RestLength
        {
            get
            {
                return _restLength;
            }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(RestLength), "Rest needs to be at least 1s long");
                _restLength = value;
                RaisePropertyChanged(nameof(RestLength));
            }
        }

        /// <summary>
        /// The number of experimental trials
        /// </summary>
        public int NTrials
        {
            get
            {
                return _nTrials;
            }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(NTrials), "Need to have at least 1 trial");
                _nTrials = value;
                RaisePropertyChanged(nameof(NTrials));
            }
        }

        /// <summary>
        /// The baseline laser power
        /// as rest and lowest ramp value in mW
        /// </summary>
        public double BaseLaserPower
        {
            get
            {
                return _baseLaserPower;
            }
            set
            {
                if (value < 0 || value > 8000)
                    throw new ArgumentOutOfRangeException(nameof(BaseLaserPower), "Laser power needs to be between 0 and 8000 mW");
                _baseLaserPower = value;
                RaisePropertyChanged(nameof(BaseLaserPower));
            }
        }

        /// <summary>
        /// The ramp peak laser power in mW
        /// </summary>
        public double PeakLaserPower
        {
            get
            {
                return _peakLaserPower;
            }
            set
            {
                if (value < 0 || value > 8000)
                    throw new ArgumentOutOfRangeException(nameof(PeakLaserPower), "Laser power needs to be between 0 and 8000 mW");
                _peakLaserPower = value;
                RaisePropertyChanged(nameof(PeakLaserPower));
            }
        }

        public ExperimentBase CreateExperiment(string name, string fishID)
        {
            string folder = Path.Combine(Properties.Settings.Default.SavePath,"TrackAndWrite");
            OpenLoopLaserRamp ollramp = new OpenLoopLaserRamp(HabituationLength, RampLength, RestLength, NTrials, Properties.Settings.Default.FrameRate, Properties.Settings.Default.PixelsPermm, folder, name, fishID);
            ollramp.BaseLaserPower = BaseLaserPower;
            ollramp.PeakLaserPower = PeakLaserPower;
            return ollramp;
        }
    }
}
