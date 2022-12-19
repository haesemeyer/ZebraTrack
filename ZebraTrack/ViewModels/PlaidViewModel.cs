/*
Copyright 2022 Martin Haesemeyer
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
    /// View model for plaid paradigm
    /// </summary>
    class PlaidViewModel : ViewModelBase, IExperimentVM
    {
        /// <summary>
        /// The length of the habituation phase in seconds
        /// </summary>
        int _habituationLength;

        /// <summary>
        /// The length of the pre phase in seconds
        /// </summary>
        int _preLength;

        /// <summary>
        /// The length of the plaid phase in seconds
        /// </summary>
        int _plaidLength;

        /// <summary>
        /// The number of trials to run
        /// </summary>
        int _nTrials;

        /// <summary>
        /// The plaid trough laser power and power during pre phase
        /// </summary>
        double _troughLaserPower;

        /// <summary>
        /// The laser power at the plaid peak
        /// </summary>
        double _peakLaserPower;

        /// <summary>
        /// The period of the plaid stimulus in mm
        /// </summary>
        double _plaidPeriodMM;

        public PlaidViewModel()
        {
            HabituationLength = 10 * 60;
            PreLength = 20 * 60;
            PlaidLength = PreLength;
            NTrials = 1;
            TroughLaserPower = 0;
            PeakLaserPower = 1500;
            PlaidPeriodMM = 20;
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
        /// The length of the pre phase in seconds
        /// </summary>
        public int PreLength
        {
            get
            {
                return _preLength;
            }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(PreLength), "Pre phase needs to be at least 1s long");
                _preLength = value;
                RaisePropertyChanged(nameof(PreLength));
            }
        }

        /// <summary>
        /// The length of the plaid phase in seconds
        /// </summary>
        public int PlaidLength
        {
            get
            {
                return _plaidLength;
            }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(PlaidLength), "Plaid phase needs to be at least 1s long");
                _plaidLength = value;
                RaisePropertyChanged(nameof(PlaidLength));
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
        /// The laser power at the trough of the plaid and during pre phase
        /// </summary>
        public double TroughLaserPower
        {
            get
            {
                return _troughLaserPower;
            }
            set
            {
                if (value < 0 || value > 8000)
                    throw new ArgumentOutOfRangeException(nameof(TroughLaserPower), "Laser power needs to be between 0 and 8000 mW");
                _troughLaserPower = value;
                RaisePropertyChanged(nameof(TroughLaserPower));
            }
        }

        /// <summary>
        /// The laser power at the plaid peak
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

        /// <summary>
        /// The period (peak-to-peak distance) of the plaid in mm
        /// </summary>
        public double PlaidPeriodMM
        {
            get
            {
                return _plaidPeriodMM;
            }
            set
            {
                _plaidPeriodMM = value;
                RaisePropertyChanged(nameof(PlaidPeriodMM));
            }
        }

        public ExperimentBase CreateExperiment(string name, string fishID)
        {
            string folder = Path.Combine(Properties.Settings.Default.SavePath, "Plaid");
            Plaid plaid = new Plaid(HabituationLength, PreLength, PlaidLength, NTrials, Properties.Settings.Default.FrameRate, Properties.Settings.Default.PixelsPermm, folder, name, fishID);
            plaid.TroughLaserPower = TroughLaserPower;
            plaid.PeakLaserPower = PeakLaserPower;
            plaid.PlaidPeriodMM = PlaidPeriodMM;
            return plaid;
        }
    }
}
