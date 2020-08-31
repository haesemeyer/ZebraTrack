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
using Microsoft.Win32;
using System.Collections.Generic;

namespace ZebraTrack.ViewModels
{
    /// <summary>
    /// ViewModel for simple TrackAndWrite experiment
    /// </summary>
    class ReplayViewModel : ViewModelBase, IExperimentVM
    {
        /// <summary>
        /// The content of the info-file
        /// Will be filled after loading in
        /// Create Experiment
        /// </summary>
        string _infoText;

        public string InfoText
        {
            get
            {
                return _infoText;
            }
            set
            {
                _infoText = value;
                RaisePropertyChanged(nameof(InfoText));
            }
        }

        public ReplayViewModel()
        {
            InfoText = "";
        }

        public ExperimentBase CreateExperiment(string name, string fishID)
        {
            string folder = Path.Combine(Properties.Settings.Default.SavePath,"Replay");
            //Let user select file and load
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.FileName = "Experiment";
            dlg.DefaultExt = ".info";
            dlg.Filter = "Experiment info (.info)|*.info";
            bool? result = dlg.ShowDialog();
            Replay.OriginalType originalType = Replay.OriginalType.Unknown;
            Queue<string> info_content = null;
            Queue<string> track_content = null;
            // Process open file dialog box results
            if (result == true)
            {
                string info_file = dlg.FileName;
                string[] parts = info_file.Split('.');
                parts[parts.Length - 1] = "track";
                string track_file = string.Join(".", parts);
                try
                {
                    StreamReader infoReader = File.OpenText(info_file);
                    info_content = new Queue<string>();
                    //Load info file and check each line for signal words about experiment type
                    while (!infoReader.EndOfStream)
                    {
                        var s = infoReader.ReadLine();
                        if (s.Contains("Circular Gradient"))
                            originalType = Replay.OriginalType.CircularGradient;
                        info_content.Enqueue(s);
                    }
                    infoReader.Close();
                    StreamReader trackReader = File.OpenText(track_file);
                    track_content = new Queue<string>();
                    while (!trackReader.EndOfStream)
                    {
                        var s = trackReader.ReadLine();
                        track_content.Enqueue(s);
                    }
                    trackReader.Close();
                }
                catch (IOException)
                {
                    System.Diagnostics.Debug.WriteLine("Could not open either .info or .track file. Aborting");
                    return null;
                }
                if(originalType == Replay.OriginalType.Unknown)
                {
                    System.Diagnostics.Debug.WriteLine("Did not recognize type of original experiment. Aborting");
                    return null;
                }
                InfoText = info_file;
            }

            var frame_rate = Properties.Settings.Default.FrameRate;
            Replay replay = new Replay(track_content, info_content, originalType, track_content.Count / frame_rate, frame_rate, Properties.Settings.Default.PixelsPermm, folder, name, fishID);
            return replay;
        }
    }
}
