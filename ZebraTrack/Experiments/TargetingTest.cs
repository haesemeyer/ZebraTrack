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
using ipp;
using MHApi.DrewsClasses;
using MHApi.Imaging;
using MHApi.Laser;
using MHApi.Scanning;

namespace ZebraTrack.Experiments
{

    /// <summary>
    /// Experiment to test targeting accuracy by interleaving laser on frames
    /// which are excluded from tracking with laser off frames. For laser to
    /// be visible, blocking filter needs to be removed from camera.
    /// </summary>
    unsafe class TargetingTest : TrackingExperiment
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

        /// <summary>
        /// Writes fish region camera image
        /// </summary>
        TiffWriter _imageWriter;

        SDLPS500Controller _laser;

        RandomAccessScanner _scanner;
        
        public TargetingTest(int expSeconds, int frameRate, int pxPERmm, string folder, string name, string fishID) : base(folder, name, fishID, expSeconds, frameRate, pxPERmm)
        {
            _totalFrames = ExperimentLength * FrameRate;
            if (FileSaver != null)
            {
                _trackWriter = FileSaver.GetStreamWriter(".track");               
                _imageWriter = FileSaver.GetTiffWriter("_camImage.tif", true);
            }
            _laser = new SDLPS500Controller(Properties.Settings.Default.DAQ, Properties.Settings.Default.LaserAO);
            string xchannel = Properties.Settings.Default.DAQ + "/" + Properties.Settings.Default.ScannerX;
            string ychannel = Properties.Settings.Default.DAQ + "/" + Properties.Settings.Default.ScannerY;
            //Load scan lookup table from file
            try
            {
                TextReader reader = File.OpenText("main.calib");
                var scanTable = BLIScanLookupTable.LoadFromFile(reader);
                _scanner = new RandomAccessScanner(scanTable, xchannel, ychannel, -10, 10);
                reader.Dispose();
            }
            catch (IOException)
            {
                System.Diagnostics.Debug.WriteLine("Could not find calibration data. No targeting available");
            }
            //This is an closed-loop experiment - try to stay fully up-to-date
            SuggestedBufferSeconds = 0;
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
        /// <param name="camImage">The camera image</param>
        /// <param name="poi">The fish location</param>
        /// <returns>Whether experiment should continue or not</returns>
        public override bool ProcessNext(int frameNumber, Image8 camImage, out IppiPoint? poi)
        {
            if (_scanner == null)
            {
                System.Diagnostics.Debug.WriteLine("Scanner was not initialized. Terminating experiment");
                return false;
            }
            int writeEvery = Properties.Settings.Default.FrameRate * 4;
            if (frameNumber % writeEvery == writeEvery - 1)
                _laser.LaserPower = Properties.Settings.Default.LaserCalibPowermW;
            else
                _laser.LaserPower = 0;
            base.ProcessNext(frameNumber, camImage, out poi);
            _lastFrame = frameNumber;
            if (frameNumber >= _totalFrames)
                return false;
            BlobWithMoments fish = null;
            // Every 4s we turn on the laser - those frames won't be tracked but the image will be saved
            if(frameNumber % writeEvery != 0)
                fish = Tracker.Track(camImage);
            if (fish != null)
            {
                poi = new IppiPoint(fish.Centroid.x, fish.Centroid.y);
                if (_scanner != null)
                    _scanner.Hit(poi.GetValueOrDefault());
            }
            if (_trackWriter != null)
            {
                if (fish != null)
                {
                    _trackWriter.WriteLine("{0}\t{1}\t{2}\t{3}", frameNumber, fish.Centroid.x, fish.Centroid.y, fish.Angle);
                }
                else
                {
                    _trackWriter.WriteLine("NaN\tNaN\tNaN\tNaN");
                }
                // Write image of Laser On frames and the two surrounding frames on each side to file
                if (frameNumber % writeEvery <=2 || frameNumber % writeEvery >= writeEvery-2)
                {
                    _imageWriter.WriteFrame(camImage);
                }
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
                if(_imageWriter != null)
                {
                    _imageWriter.Dispose();
                    _imageWriter = null;
                }
                if(_laser != null)
                {
                    _laser.Dispose();
                }
                if(_scanner != null)
                {
                    _scanner.Dispose();
                }
            }
        }
    }
}
