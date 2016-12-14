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
using MHApi.Imaging;

namespace ZebraTrack.Experiments
{

    /// <summary>
    /// Very simple experiment class that for a given time
    /// writes fish position and heading to file
    /// </summary>
    class TrackAndWrite : ExperimentBase
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
        
        public TrackAndWrite(int expSeconds, int frameRate, string folder, string name, string fishID) : base(folder, name, fishID, expSeconds, frameRate)
        {
            _totalFrames = ExperimentLength * FrameRate;
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
        public override bool ProcessNext(int frameNumber, BlobWithMoments fish, Image8 fishImage)
        {
            _lastFrame = frameNumber;
            if (frameNumber >= _totalFrames)
                return false;
            if (fish != null)
                _trackWriter.WriteLine("{0}\t{0}\t{0}\t{0}", frameNumber, fish.Centroid.x, fish.Centroid.y, fish.Angle);
            else
                _trackWriter.WriteLine("NaN\tNaN\tNaN\tNaN");
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
