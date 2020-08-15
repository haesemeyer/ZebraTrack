/*
Copyright 2016-2020 Martin Haesemeyer
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
using ipp;
using MHApi.DrewsClasses;

namespace ZebraTrack.Experiments
{
    abstract class ExperimentBase : IExperiment, IDisposable
    {
        /// <summary>
        /// Text file interface for experiment information
        /// </summary>
        private StreamWriter _infoWriter;

        /// <summary>
        /// The saver object from which different file
        /// writers can be derived
        /// </summary>
        protected Saver FileSaver { get; private set; }

        /// <summary>
        /// The acquisition framerate
        /// </summary>
        protected int FrameRate { get; private set; }

        /// <summary>
        /// The intended length of the experiment in seconds
        /// </summary>
        protected int ExperimentLength { get; private set; }

        /// <summary>
        /// The name of the experiment
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The ID/kind of fish used for the experiment
        /// </summary>
        public string FishID { get; private set; }

        /// <summary>
        /// A comment string associated with the experiment
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// The Date of birth of the fish
        /// </summary>
        public DateTime DOB { get; set; }

        /// <summary>
        /// The center of the dish
        /// </summary>
        public IppiPoint DishCenter { get; set; }

        /// <summary>
        /// The radius of a circular dish
        /// </summary>
        public int Radius { get; set; }

        /// <summary>
        /// The time the experiment object was constructed
        /// </summary>
        protected DateTime StartTime { get; set; }

        /// <summary>
        /// Constructs a new experiment base
        /// </summary>
        /// <param name="folder">The folder in which to save files</param>
        /// <param name="name">The name of the experiment</param>
        /// <param name="fishID">The fish used for the experiment</param>
        /// <param name="experimentLength">The length of the experiment in seconds</param>
        /// <param name="frameRate">The acquisition framerate</param>
        public ExperimentBase(string folder, string name, string fishID, int experimentLength, int frameRate)
        {
            if (folder != "" && !Path.IsPathRooted(folder))
                throw new ArgumentException("folder needs to be a rooted path", nameof(folder));
            if (folder != "")
                FileSaver = new Saver(folder, name + '_' + fishID, true);
            else
                FileSaver = null;
            Name = name;
            FishID = fishID;
            if (FileSaver != null)
                _infoWriter = FileSaver.GetStreamWriter(".info");
            StartTime = DateTime.Now;
            ExperimentLength = experimentLength;
            FrameRate = frameRate;
            SuggestedBufferSeconds = null;
        }

        /// <summary>
        /// Writes information about the experiment to the info file
        /// </summary>
        public void WriteExperimentInformation()
        {
            WriteExperimentInfo(_infoWriter);
        }

        /// <summary>
        /// Write overview information about the experiment to file
        /// can be overridden for adding specific information
        /// </summary>
        /// <param name="infoWriter"></param>
        protected virtual void WriteExperimentInfo(StreamWriter infoWriter)
        {
            if (infoWriter != null)
            {
                infoWriter.WriteLine("Experiment name: {0}", Name);
                infoWriter.WriteLine("Fish name: {0}", FishID);
                infoWriter.WriteLine("Fish DOB: {0}", DOB);
                infoWriter.WriteLine("Dish center (x, y): ({0}, {1})", DishCenter.x, DishCenter.y);
                infoWriter.WriteLine("Dish radius: {0}", Radius);
                infoWriter.WriteLine("------");
                infoWriter.WriteLine("Comment:");
                infoWriter.WriteLine(Comment);
                infoWriter.WriteLine("------");
                infoWriter.WriteLine("Start date: {0}", StartTime.ToShortDateString());
                infoWriter.WriteLine("Start Time: {0}", StartTime.ToShortTimeString());
                infoWriter.WriteLine("");
            }
        }


        #region IExperiment
        public abstract int SecondsRemaining { get; }
        public abstract string StatusMessage { get; }

        public abstract bool ProcessNext(int frameNumber, Image8 camImage, out IppiPoint? poi);

        public float? SuggestedBufferSeconds { get; protected set; }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                //write experiment end time to file
                if (_infoWriter != null)
                {
                    _infoWriter.WriteLine("--------------");
                    _infoWriter.WriteLine("Experiment ended:");
                    _infoWriter.WriteLine(DateTime.Now.ToString());
                }
                if (disposing)
                {
                    if(_infoWriter != null)
                    {
                        _infoWriter.Dispose();
                        _infoWriter = null;
                    }
                }

                disposedValue = true;
            }
        }

        ~ExperimentBase()
        {
           Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
