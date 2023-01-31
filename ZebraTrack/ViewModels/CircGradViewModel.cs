/*
Copyright 2020 Martin Haesemeyer
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
    class CircGradViewModel : ViewModelBase, IExperimentVM
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
        /// The length of the gradient phase in seconds
        /// </summary>
        int _gradientLength;

        /// <summary>
        /// The number of trials to run
        /// </summary>
        int _nTrials;

        /// <summary>
        /// The dish center laser power and power during pre phase
        /// </summary>
        double _centerLaserPower;

        /// <summary>
        /// The laser power at the edge of the dish
        /// </summary>
        double _edgeLaserPower;

        public CircGradViewModel()
        {
            HabituationLength = 10 * 60;
            PreLength = 20 * 60;
            GradientLength = PreLength;
            NTrials = 1;
            CenterLaserPower = 0;
            EdgeLaserPower = 1000;
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
        /// The length of the gradient phase in seconds
        /// </summary>
        public int GradientLength
        {
            get
            {
                return _gradientLength;
            }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(GradientLength), "Gradient needs to be at least 1s long");
                _gradientLength = value;
                RaisePropertyChanged(nameof(GradientLength));
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
        /// The laser power in the dish center and during pre phase
        /// </summary>
        public double CenterLaserPower
        {
            get
            {
                return _centerLaserPower;
            }
            set
            {
                if (value < 0 || value > 8000)
                    throw new ArgumentOutOfRangeException(nameof(CenterLaserPower), "Laser power needs to be between 0 and 8000 mW");
                _centerLaserPower = value;
                RaisePropertyChanged(nameof(CenterLaserPower));
            }
        }

        /// <summary>
        /// The laser power at the edge of the dish
        /// </summary>
        public double EdgeLaserPower
        {
            get
            {
                return _edgeLaserPower;
            }
            set
            {
                if (value < 0 || value > 8000)
                    throw new ArgumentOutOfRangeException(nameof(EdgeLaserPower), "Laser power needs to be between 0 and 8000 mW");
                _edgeLaserPower = value;
                RaisePropertyChanged(nameof(EdgeLaserPower));
            }
        }

        public ExperimentBase CreateExperiment(string name, string fishID)
        {
            string folder = Path.Combine(Properties.Settings.Default.SavePath,"CircularGradient");
            CircularGradient cgrad = new CircularGradient(HabituationLength, PreLength, GradientLength, NTrials, Properties.Settings.Default.FrameRate, Properties.Settings.Default.PixelsPermm, folder, name, fishID);
            cgrad.CenterLaserPower = CenterLaserPower;
            cgrad.EdgeLaserPower = EdgeLaserPower;
            return cgrad;
        }
    }
}
