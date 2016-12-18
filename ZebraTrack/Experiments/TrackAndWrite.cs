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
using ipp;
using MHApi.DrewsClasses;

namespace ZebraTrack.Experiments
{

    /// <summary>
    /// Very simple experiment class that for a given time
    /// writes fish position and heading to file
    /// </summary>
    class TrackAndWrite : TrackingExperiment
    {
        /// <summary>
        /// The last frame that was processed
        /// </summary>
        int _lastFrame = 0;

        /// <summary>
        /// The total number of frames in the experiment
        /// </summary>
        int _totalFrames;

        /// <summary>
        /// The file writer to save experiment information
        /// </summary>
        StreamWriter _trackWriter;
        
        public TrackAndWrite(int expSeconds, int frameRate, int pxPERmm, string folder, string name, string fishID) : base(folder, name, fishID, expSeconds, frameRate, pxPERmm)
        {
            _totalFrames = ExperimentLength * FrameRate;
            if(FileSaver != null)
                _trackWriter = FileSaver.GetStreamWriter(".track");
            //This is an open-loop experiment rather not important to stay fully up-to-date
            SuggestedBufferSeconds = 2;
        }

        public override string StatusMessage
        {
            get
            {
                return "";
            }
        }

        /// <summary>
        /// The number of seconds remaining in the experiment
        /// </summary>
        public override int SecondsRemaining
        {
            get
            {
                return (int)(ExperimentLength - _lastFrame / FrameRate);
            }
        }
        
        /// <summary>
        /// Process the next frame
        /// </summary>
        /// <param name="frameNumber">The frame index</param>
        /// <param name="fishCentroid">The tracked fishes centroid position</param>
        /// <param name="heading">The heading of the fish</param>
        /// <param name="fishImage">Subregion image of the fish</param>
        /// <returns></returns>
        public override bool ProcessNext(int frameNumber, Image8 camImage, out IppiPoint? poi)
        {
            base.ProcessNext(frameNumber, camImage, out poi);
            _lastFrame = frameNumber;
            if (frameNumber >= _totalFrames)
                return false;
            var fish = Tracker.Track(camImage);
            if (fish != null)
                poi = new IppiPoint(fish.Centroid.x, fish.Centroid.y);
            if (_trackWriter != null)
            {
                if (fish != null)
                    _trackWriter.WriteLine("{0}\t{1}\t{2}\t{3}", frameNumber, fish.Centroid.x, fish.Centroid.y, fish.Angle);
                else
                    _trackWriter.WriteLine("NaN\tNaN\tNaN\tNaN");
            }
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (_trackWriter != null)
                {
                    _trackWriter.Dispose();
                    _trackWriter = null;

                }
            }
        }
    }
}
