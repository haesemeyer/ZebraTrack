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
using ZebraTrack.ViewModels;

namespace ZebraTrack.Experiments
{

    /// <summary>
    /// Very simple experiment class that for a given time
    /// writes fish position and heading to file
    /// </summary>
    unsafe class TrackAndWrite : TrackingExperiment
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
        /// If set to true, in addition to coordinates
        /// we will save a fish region image and corresponding
        /// background image
        /// </summary>
        bool _writeFishImages;

        /// <summary>
        /// The file writer to save experiment information
        /// </summary>
        StreamWriter _trackWriter;

        /// <summary>
        /// Writes fish region camera image
        /// </summary>
        TiffWriter _imageWriter;

        /// <summary>
        /// Writes fish region background model
        /// </summary>
        TiffWriter _backgroundWriter;

        /// <summary>
        /// Image for region around fish
        /// </summary>
        Image8 _camRegion;
        
        public TrackAndWrite(int expSeconds, int frameRate, int pxPERmm, string folder, string name, string fishID, bool writeImage) : base(folder, name, fishID, expSeconds, frameRate, pxPERmm)
        {
            _totalFrames = ExperimentLength * FrameRate;
            _writeFishImages = writeImage;
            if (FileSaver != null)
            {
                _trackWriter = FileSaver.GetStreamWriter(".track");
                if (_writeFishImages)
                {
                    _imageWriter = FileSaver.GetTiffWriter("_camImage.tif", true);
                    _backgroundWriter = FileSaver.GetTiffWriter("_bgImage.tif", true);
                    //Create 10x10 mm region image
                    _camRegion = new Image8(Properties.Settings.Default.PixelsPermm * 10,
                        Properties.Settings.Default.PixelsPermm * 10);
                }
            }
            //This is an open-loop experiment rather not important to stay fully up-to-date
            SuggestedBufferSeconds = 0.1f;
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
                {
                    _trackWriter.WriteLine("{0}\t{1}\t{2}\t{3}", frameNumber, fish.Centroid.x, fish.Centroid.y, fish.Angle);
                    if (_writeFishImages)
                    {
                        //blank and copy
                        ip.ippiSet_8u_C1R(0, _camRegion.Image, _camRegion.Stride, _camRegion.Size);
                        MainViewModel.CopyRegionImage(fish.Centroid, _camRegion, camImage);
                        _imageWriter.WriteFrame(_camRegion);
                        MainViewModel.CopyRegionImage(fish.Centroid, _camRegion, Tracker.Background);
                        _backgroundWriter.WriteFrame(_camRegion);
                    }
                }
                else
                {
                    _trackWriter.WriteLine("NaN\tNaN\tNaN\tNaN");
                    if (_writeFishImages)
                    {
                        //blank
                        ip.ippiSet_8u_C1R(0, _camRegion.Image, _camRegion.Stride, _camRegion.Size);
                        _imageWriter.WriteFrame(_camRegion);
                        _backgroundWriter.WriteFrame(_camRegion);
                    }
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
                if(_backgroundWriter != null)
                {
                    _backgroundWriter.Dispose();
                    _backgroundWriter = null;
                }
                if(_camRegion != null)
                {
                    _camRegion.Dispose();
                    _camRegion = null;
                }
            }
        }
    }
}
