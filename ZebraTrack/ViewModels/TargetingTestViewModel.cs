/*
Copyright 2019 Martin Haesemeyer
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
    /// View model for laser alignment
    /// </summary>
    class TargetingTestViewModel : ViewModelBase, IExperimentVM
    {
        /// <summary>
        /// The length of the experiment in seconds
        /// </summary>
        int _experimentLength;

        /// <summary>
        /// The length of the experiment in seconds
        /// </summary>
        public int ExperimentLength
        {
            get
            {
                return _experimentLength;
            }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(ExperimentLength), "Experiment needs to be at least 1s long");
                _experimentLength = value;
                RaisePropertyChanged(nameof(ExperimentLength));
            }
        }

        public TargetingTestViewModel()
        {
            ExperimentLength = 10 * 60;
        }

        public ExperimentBase CreateExperiment(string name, string fishID)
        {
            string folder = Path.Combine(Properties.Settings.Default.SavePath, "TargetingTest");
            return new TargetingTest(_experimentLength, Properties.Settings.Default.FrameRate, Properties.Settings.Default.PixelsPermm, folder, name, fishID);
        }
    }
}
