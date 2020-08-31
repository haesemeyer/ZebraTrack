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
using ZebraTrack.ViewModels;
using System.Collections.Generic;

namespace ZebraTrack.Experiments
{

    /// <summary>
    /// Experiment to play back experienced laser powers from a closed-loop experiment
    /// </summary>
    unsafe class Replay : TrackingExperiment
    {
        public enum ExperimentPhase { Done = 4, Replay = 8 };

        /// <summary>
        /// Identifies the type of the original closed loop experiment
        /// </summary>
        public enum OriginalType { Unknown, CircularGradient };

        #region Members

        /// <summary>
        /// The last frame that was processed
        /// </summary>
        int _lastFrame = 0;

        /// <summary>
        /// The file writer to save experiment information
        /// </summary>
        StreamWriter _trackWriter;

        /// <summary>
        /// Writes fish region camera image
        /// </summary>
        TiffWriter _imageWriter;

        /// <summary>
        /// Writes fish region background image
        /// </summary>
        TiffWriter _backgroundWriter;

        /// <summary>
        /// Image for region around fish
        /// </summary>
        Image8 _camRegion;

        /// <summary>
        /// Our laser power controller
        /// </summary>
        SDLPS500Controller _laser;

        /// <summary>
        /// Scanner to acquire fish images
        /// </summary>
        RandomAccessScanner _scanner;

        /// <summary>
        /// The original experiment type so
        /// we know how to interpret the track data
        /// </summary>
        OriginalType _originalType;

        /// <summary>
        /// The line-by-line data of the track file of the original experiment
        /// </summary>
        Queue<string> _originalTrackData;

        /// <summary>
        /// The line-by-line data of the original experiment's info file
        /// </summary>
        Queue<string> _originalInfoData;

        ExperimentPhase _experimentPhase;

        #endregion

        public Replay(Queue<string> originalTrackData, Queue<string> originalInfoData, OriginalType originalType, int originalLength, int frameRate, int pxPERmm, string folder, string name, string fishID) : 
            base(folder, name, fishID, originalLength, frameRate, pxPERmm)
        {
            if (FileSaver != null)
            {
                _trackWriter = FileSaver.GetStreamWriter(".track");
                _imageWriter = FileSaver.GetTiffWriter("_camImage.tif", true);
                _backgroundWriter = FileSaver.GetTiffWriter("_bgImage.tif", true);
                //Create 10x10 mm region image
                _camRegion = new Image8(Properties.Settings.Default.PixelsPermm * 10, Properties.Settings.Default.PixelsPermm * 10);
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
            //This is a closed-loop experiment - try to stay fully up-to-date
            SuggestedBufferSeconds = 0;
            _originalType = originalType;
            _originalTrackData = originalTrackData;
            _originalInfoData = originalInfoData;
            _experimentPhase = ExperimentPhase.Replay;
        }

        #region Properties

        /// <summary>
        /// The number of seconds remaining in the experiment
        /// </summary>
        public override int SecondsRemaining
        {
            get
            {
                return (ExperimentLength - _lastFrame / FrameRate);
            }
        }

        public override string StatusMessage
        {
            get
            {
                switch (_experimentPhase)
                {
                    case ExperimentPhase.Replay:
                        return "Replay";
                }
                return "Done";
            }
        }

        #endregion

        #region Methods

        double ProcessTrackItems(string[] items, out int originalPhase, out int originalTrial)
        {
            if (_originalType == OriginalType.CircularGradient)
            {
                double power;
                bool success = double.TryParse(items[6], out power);
                success = success & int.TryParse(items[4], out originalPhase);
                success = success & int.TryParse(items[5], out originalTrial);
                if (!success)
                    power = double.NaN;
                return power;
            }
            else
                throw new NotImplementedException();
        }

        /// <summary>
        /// Write pertinent information to info file
        /// </summary>
        /// <param name="infoWriter">The stream writer of the info file</param>
        protected override void WriteExperimentInfo(StreamWriter infoWriter)
        {
            base.WriteExperimentInfo(infoWriter);
            infoWriter.WriteLine();
            infoWriter.Write("Replay experiment of: ");
            switch (_originalType)
            {
                case OriginalType.CircularGradient:
                    infoWriter.WriteLine("Circular gradient experiment");
                    break;
            }
            infoWriter.WriteLine("------------------------");
            infoWriter.WriteLine("Original info file data follows:");
            foreach (string item in _originalInfoData)
                infoWriter.WriteLine(item);
            infoWriter.WriteLine("------------------------");
            infoWriter.WriteLine();
        }

        /// <summary>
        /// Write information to track file
        /// </summary>
        /// <param name="fish"></param>
        private void WriteTrackInfo(int frameNumber, BlobWithMoments fish, int originalPhase, int originalTrial)
        {
            if (fish != null)
            {
                _trackWriter.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}", frameNumber, fish.Centroid.x, fish.Centroid.y, fish.Angle, originalPhase, originalTrial, _laser.LaserPower);
            }
            else
            {
                _trackWriter.WriteLine("NaN\tNaN\tNaN\tNaN\t{0}\t{1}\tNaN", originalPhase, originalTrial);
            }
        }

        /// <summary>
        /// Processes one frame during the replay experiment
        /// </summary>
        private void RunReplay(double originalPower)
        {
            _laser.LaserPower = originalPower;
            //If queue is empty now, change phase to done
            if (_originalTrackData.Count < 1)
                _experimentPhase = ExperimentPhase.Done;
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
            if (_originalType == OriginalType.Unknown)
            {
                //This should never happen...
                System.Diagnostics.Debug.WriteLine("Unknown original experiment type. Terminating experiment");
                return false;
            }
            if (_scanner == null)
            {
                System.Diagnostics.Debug.WriteLine("Scanner was not initialized. Terminating experiment");
                return false;
            }
            if (_experimentPhase == ExperimentPhase.Done)
                return false;
            
            BlobWithMoments fish = null;
            fish = Tracker.Track(camImage);
            if (fish != null)
            {
                poi = new IppiPoint(fish.Centroid.x, fish.Centroid.y);
                try
                {
                    if (_scanner != null)
                        _scanner.Hit(poi.GetValueOrDefault());
                }
                catch (ArgumentOutOfRangeException)
                {
                    System.Diagnostics.Debug.WriteLine("Tried to hit coordinates outside scan table area.");
                    System.Diagnostics.Debug.WriteLine("Coordinates were: x={0}, y={1}", fish.xc, fish.yc);
                    System.Diagnostics.Debug.WriteLine("Terminating Experiment");
                    _laser.LaserPower = 0;
                    return false;
                }
            }
            //Get the next data item
            string currentData = _originalTrackData.Dequeue();
            string[] currentItems = currentData.Split('\t');
            int phase, trial;
            double power = ProcessTrackItems(currentItems, out phase, out trial);
            if (double.IsNaN(power))
                power = 0;
            //Write track information
            WriteTrackInfo(frameNumber, fish, phase, trial);
            //Write images
            ip.ippiSet_8u_C1R(0, _camRegion.Image, _camRegion.Stride, _camRegion.Size);
            if(fish != null)
                MainViewModel.CopyRegionImage(fish.Centroid, _camRegion, camImage);
            _imageWriter.WriteFrame(_camRegion);
            if (fish != null)
                MainViewModel.CopyRegionImage(fish.Centroid, _camRegion, Tracker.Background);
            _backgroundWriter.WriteFrame(_camRegion);
            RunReplay(power);
            //If we have lost the fish, disengage the laser
            if (fish == null)
                _laser.LaserPower = 0;
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
                if(_laser != null)
                {
                    _laser.Dispose();
                }
                if(_scanner != null)
                {
                    _scanner.Dispose();
                }
                if(_camRegion != null)
                {
                    _camRegion.Dispose();
                    _camRegion = null;
                }
            }
        }

        #endregion
    }
}
