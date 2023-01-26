/*
Copyright 2022 Martin Haesemeyer
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
using MHApi.RND;
using MHApi.Threading;
using ZebraTrack.ViewModels;

namespace ZebraTrack.Experiments
{
    unsafe class Plaid : TrackingExperiment
    {
        public enum ExperimentPhase { Habituation = 0, Pre = 1, Plaid = 9, Done = 4 };

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
        AsyncTiffWriter _imageWriter;

        /// <summary>
        /// Writes fish region background image
        /// </summary>
        AsyncTiffWriter _backgroundWriter;

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
        /// Laser power in the trough of plaid and during pre-condition
        /// </summary>
        double _troughLaserPower;

        /// <summary>
        /// Laser power at the peak of the plaid
        /// </summary>
        double _peakLaserPower;

        /// <summary>
        /// The offset of the plaid in x-direction
        /// </summary>
        int _plaidOffsetX;

        /// <summary>
        /// The offset of the plaid in y-direction
        /// </summary>
        int _plaidOffsetY;

        /// <summary>
        /// The total number of frames during habituation
        /// </summary>
        int _habitFrames;

        /// <summary>
        /// The total number of frames in the pre-phase
        /// </summary>
        int _preFrames;

        /// <summary>
        /// The total number of frames in the plaid phase
        /// </summary>
        int _plaidFrames;

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

        public Plaid(int habitSeconds, int preSeconds, int plaidSeconds, int nTrials, int frameRate, int pxPERmm, string folder, string name, string fishID) :
            base(folder, name, fishID, habitSeconds + (preSeconds + plaidSeconds) * nTrials, frameRate, pxPERmm)
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
            _preFrames = preSeconds * FrameRate;
            _plaidFrames = plaidSeconds * FrameRate;
            _habitFrames = habitSeconds * FrameRate;
            _nTrials = nTrials;
            _currentPhaseFrame = 0;
            _currentTrial = 0;//Start at 0 during habituation will be pre-incremented as we enter pre-phase and will be checked for completing at end of plaid phase
            //This is a closed-loop experiment - try to stay fully up-to-date
            SuggestedBufferSeconds = 0;
            //Start in habituation phase
            _experimentPhase = ExperimentPhase.Habituation;
            //Randomly set our plaid offsets
            var rnd = ThreadSafeRandom.ThisThreadsRandom;
            _plaidOffsetX = rnd.Next(1000);
            _plaidOffsetY = rnd.Next(1000);
        }

        #region Properties

        /// <summary>
        /// Laser power at the trough of the plaid and during pre phase
        /// </summary>
        public double TroughLaserPower
        {
            get
            {
                return _troughLaserPower;
            }
            set
            {
                if (value >= 0 && value <= 8000)
                    _troughLaserPower = value;
                else
                    throw new ArgumentOutOfRangeException(nameof(TroughLaserPower), "Laser power has to be between 0 and 8000 mW");
            }
        }

        /// <summary>
        /// Laser power at the peak of the plaid
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

        /// <summary>
        /// The period (peak-to-peak distance) of the plaid in mm
        /// </summary>
        public double PlaidPeriodMM { get; set; }

        /// <summary>
        /// The period (peak-to-peak distance) of the plaid in pixels
        /// </summary>
        public double PlaidPeriodPx
        {
            get
            {
                return PlaidPeriodMM * PixelsPermm;
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
                    case ExperimentPhase.Pre:
                        return "Pre Gradient";
                    case ExperimentPhase.Plaid:
                        return "Plaid";
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
            infoWriter.WriteLine("Thermal plaid experiment");
            infoWriter.WriteLine("Habituation length {0} [s]", _habitFrames / FrameRate);
            infoWriter.WriteLine("Pre gradient length {0} [s]", _preFrames / FrameRate);
            infoWriter.WriteLine("Plaid length {0} [s]", _plaidFrames / FrameRate);
            infoWriter.WriteLine("Trough power {0} [mW]", TroughLaserPower);
            infoWriter.WriteLine("Peak power {0} [mW]", PeakLaserPower);
            infoWriter.WriteLine("Plaid period {0} [mm]", PlaidPeriodMM);
            infoWriter.WriteLine("Plaid period {0} [px]", PlaidPeriodPx);
            infoWriter.WriteLine("Plaid offset X {0} [px]", _plaidOffsetX);
            infoWriter.WriteLine("Plaid offset Y {0} [px]", _plaidOffsetY);
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
        /// Switches phase to pre-gradient marking the begin of a new trial
        /// </summary>
        private void SwitchToPrePhase()
        {
            _experimentPhase = ExperimentPhase.Pre;
            _currentPhaseFrame = 0;
            _currentTrial++;
        }

        /// <summary>
        /// Processes one frame during habituation
        /// </summary>
        private void RunHabituation()
        {
            //Here we simply count down the clock and then move to pre phase
            if (_currentPhaseFrame >= _habitFrames)
            {
                SwitchToPrePhase();
            }
            else
                _currentPhaseFrame++;
        }

        /// <summary>
        /// Processes one frame during Pre-phase
        /// </summary>
        private void RunPrePhase()
        {
            //flat power corresponding to center power
            _laser.LaserPower = TroughLaserPower;
            if (_currentPhaseFrame >= _preFrames)
            {
                _currentPhaseFrame = 0;
                _experimentPhase = ExperimentPhase.Plaid;
            }
            else
                _currentPhaseFrame++;
        }

        /// <summary>
        /// Processes one frame during the plaid phase
        /// </summary>
        private void RunPlaid(BlobWithMoments fish)
        {
            //compute laser power based on plaid
            if (fish != null)
            {
                // calculate plaid function in -1/1 range
                double plaid_factor = 0.5 * Math.Sin(2 * Math.PI * fish.xc / PlaidPeriodPx) + 0.5 * Math.Sin(2 * Math.PI * fish.yc / PlaidPeriodPx);
                double plaid_average = (PeakLaserPower - TroughLaserPower) / 2 + TroughLaserPower; //Power at plaid_factor = 0
                double plaid_amplitude = (PeakLaserPower - TroughLaserPower) / 2;
                double power = plaid_average + plaid_factor * plaid_amplitude;
                if (power < 0)
                    power = 0;
                _laser.LaserPower = power;
            }
            if (_currentPhaseFrame >= _plaidFrames)
            {
                if (_currentTrial < _nTrials)
                    SwitchToPrePhase();
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
            _lastFrame++;
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
            //Write track information including current phase, current laser power
            WriteTrackInfo(frameNumber, fish);
            //Write images
            ip.ippiSet_8u_C1R(0, _camRegion.Image, _camRegion.Stride, _camRegion.Size);
            if (fish != null)
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
                case ExperimentPhase.Pre:
                    RunPrePhase();
                    break;
                case ExperimentPhase.Plaid:
                    RunPlaid(fish);
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
                if (_imageWriter != null)
                {
                    _imageWriter.Dispose();
                    _imageWriter = null;
                }
                if (_backgroundWriter != null)
                {
                    _backgroundWriter.Dispose();
                    _backgroundWriter = null;
                }
                if (_laser != null)
                {
                    _laser.Dispose();
                }
                if (_scanner != null)
                {
                    _scanner.Dispose();
                }
                if (_camRegion != null)
                {
                    _camRegion.Dispose();
                    _camRegion = null;
                }
            }
        }

        #endregion
    }
}
