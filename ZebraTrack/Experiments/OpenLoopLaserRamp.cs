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

namespace ZebraTrack.Experiments
{

    /// <summary>
    /// Open loop paradigm presenting an increasing power ramp followed by a decreasing power ramp
    /// to larval zebrafish. Up/down ramps occur back-to-back and are interleaved with resting phases
    /// </summary>
    unsafe class OpenLoopLaserRamp : TrackingExperiment
    {
        public enum ExperimentPhase { Habituation=0, RampUp=1, RampDown=2, Rest=3, Done=4};

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
        /// The current experiment phase
        /// </summary>
        ExperimentPhase _experimentPhase;

        /// <summary>
        /// Laser power at the start of the heating ramp and during resting
        /// </summary>
        double _baseLaserPower;

        /// <summary>
        /// Laser power at the end of the heating ramp
        /// </summary>
        double _peakLaserPower;

        /// <summary>
        /// The total number of habituation frames
        /// </summary>
        int _habitFrames;

        /// <summary>
        /// The total number of rest phase frames
        /// </summary>
        int _restFrames;

        /// <summary>
        /// The total number of frames ramping up or down
        /// </summary>
        int _rampFrames;

        /// <summary>
        /// The number of trials
        /// </summary>
        int _nTrials;

        /// <summary>
        /// The frame number within the current phase
        /// </summary>
        int _currentPhaseFrame;

        /// <summary>
        /// The currently active trial
        /// </summary>
        int _currentTrial;

        #endregion

        public OpenLoopLaserRamp(int habitSeconds, int rampSeconds, int restSeconds, int nTrials, int frameRate, int pxPERmm, string folder, string name, string fishID) : 
            base(folder, name, fishID, habitSeconds + (rampSeconds*2+restSeconds)*nTrials, frameRate, pxPERmm)
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
            _habitFrames = habitSeconds * FrameRate;
            _restFrames = restSeconds * FrameRate;
            _rampFrames = rampSeconds * FrameRate;
            _nTrials = nTrials;
            _currentPhaseFrame = 0;
            _currentTrial = 0;//Start at 0 during habituation - will be pre-incremented as we enter ramp up phase and checked for completing at end of rest phase
            //This is a closed-loop experiment - try to stay fully up-to-date
            SuggestedBufferSeconds = 0;
            //Start in habituation phase
            _experimentPhase = ExperimentPhase.Habituation;
        }

        #region Properties

        /// <summary>
        /// Laser power at the start of the heating ramp and during resting
        /// </summary>
        public double BaseLaserPower
        {
            get
            {
                return _baseLaserPower;
            }
            set
            {
                if (value >= 0 && value <= 8000)
                    _baseLaserPower = value;
                else
                    throw new ArgumentOutOfRangeException(nameof(BaseLaserPower), "Laser power has to be between 0 and 8000 mW");
            }
        }

        /// <summary>
        /// Laser power at the end of the heating ramp
        /// </summary>
        public double PeakLaserPower
        {
            get
            {
                return _peakLaserPower;
            }
            set
            {
                if (value >= 0 && value <= 8000)
                    _peakLaserPower = value;
                else
                    throw new ArgumentOutOfRangeException(nameof(PeakLaserPower), "Laser power has to be between 0 and 8000 mW");
            }
        }

        public override string StatusMessage
        {
            get
            {
                switch (_experimentPhase)
                {
                    case ExperimentPhase.Habituation:
                        return "Habituation Phase";
                    case ExperimentPhase.RampUp:
                        return "Heating Ramp";
                    case ExperimentPhase.RampDown:
                        return "Cooling Ramp";
                    case ExperimentPhase.Rest:
                        return "Rest phase";
                }
                return "Done";
            }
        }

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

        #endregion

        #region Methods

        /// <summary>
        /// Write pertinent information to info file
        /// </summary>
        /// <param name="infoWriter">The stream writer of the info file</param>
        protected override void WriteExperimentInfo(StreamWriter infoWriter)
        {
            base.WriteExperimentInfo(infoWriter);
            infoWriter.WriteLine();
            infoWriter.WriteLine("Power ramp experiment");
            infoWriter.WriteLine("Habituation length {0} [s]", _habitFrames / FrameRate);
            infoWriter.WriteLine("Ramp length {0} [s]", _rampFrames / FrameRate);
            infoWriter.WriteLine("Rest length {0} [s]", _restFrames / FrameRate);
            infoWriter.WriteLine("Base power {0} [mW]", BaseLaserPower);
            infoWriter.WriteLine("Peak power {0} [mW]", PeakLaserPower);
            infoWriter.WriteLine();
        }

        /// <summary>
        /// Write information to track file
        /// </summary>
        /// <param name="fish"></param>
        private void WriteTrackInfo(int frameNumber, BlobWithMoments fish)
        {
            if (fish != null)
            {
                _trackWriter.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}", frameNumber, fish.Centroid.x, fish.Centroid.y, fish.Angle, (int)_experimentPhase, _currentTrial, _laser.LaserPower);
            }
            else
            {
                _trackWriter.WriteLine("NaN\tNaN\tNaN\tNaN\t{0}\t{1}\tNaN", (int)_experimentPhase, _currentTrial);
            }
        }

        /// <summary>
        /// Switches phase to ramp-up marking the begin of a new trial
        /// </summary>
        private void SwitchToRampUp()
        {
            _experimentPhase = ExperimentPhase.RampUp;
            _currentPhaseFrame = 0;
            _currentTrial++;
        }

        /// <summary>
        /// Processes one frame during habituation
        /// </summary>
        private void RunHabituation()
        {
            //Here we simply count down the clock and then move to ramp up phase
            if (_currentPhaseFrame >= _habitFrames)
            {
                SwitchToRampUp();
            }
            else
                _currentPhaseFrame++;
        }

        /// <summary>
        /// Processes one frame during RampUp
        /// </summary>
        private void RunRampUp()
        {
            //compute laser power as linear ramp over phase
            _laser.LaserPower = ((double)_currentPhaseFrame / (double)_rampFrames) * (PeakLaserPower - BaseLaserPower) + BaseLaserPower;
            if (_currentPhaseFrame >= _rampFrames)
            {
                _currentPhaseFrame = 0;
                _experimentPhase = ExperimentPhase.RampDown;
            }
            else
                _currentPhaseFrame++;
        }

        /// <summary>
        /// Processes one frame during RampDown
        /// </summary>
        private void RunRampDown()
        {
            //compute laser power as linear ramp over phase
            _laser.LaserPower = (1 - (double)_currentPhaseFrame / (double)_rampFrames) * (PeakLaserPower - BaseLaserPower) + BaseLaserPower;
            if (_currentPhaseFrame >= _rampFrames)
            {
                _currentPhaseFrame = 0;
                _experimentPhase = ExperimentPhase.Rest;
            }
            else
                _currentPhaseFrame++;
        }

        /// <summary>
        /// Processes one frame during Rest
        /// </summary>
        private void RunRest()
        {
            _laser.LaserPower = BaseLaserPower;
            if (_currentPhaseFrame >= _restFrames)
            {
                if(_currentTrial < _nTrials)
                    SwitchToRampUp();
                else
                {
                    _laser.LaserPower = 0;
                    _experimentPhase = ExperimentPhase.Done;
                }
            }
            else
                _currentPhaseFrame++;
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
                if (_scanner != null)
                    _scanner.Hit(poi.GetValueOrDefault());
            }
            //Write track information including current phase, current laser power
            WriteTrackInfo(frameNumber, fish);
            //Write images
            ip.ippiSet_8u_C1R(0, _camRegion.Image, _camRegion.Stride, _camRegion.Size);
            if(fish != null)
                MainViewModel.CopyRegionImage(fish.Centroid, _camRegion, camImage);
            _imageWriter.WriteFrame(_camRegion);
            if (fish != null)
                MainViewModel.CopyRegionImage(fish.Centroid, _camRegion, Tracker.Background);
            _backgroundWriter.WriteFrame(_camRegion);
            //Depending on phase call process method to control laser power and switch phase if appropriate
            switch (_experimentPhase)
            {
                case ExperimentPhase.Habituation:
                    RunHabituation();
                    break;
                case ExperimentPhase.RampUp:
                    RunRampUp();
                    break;
                case ExperimentPhase.RampDown:
                    RunRampDown();
                    break;
                case ExperimentPhase.Rest:
                    RunRest();
                    break;
            }
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
