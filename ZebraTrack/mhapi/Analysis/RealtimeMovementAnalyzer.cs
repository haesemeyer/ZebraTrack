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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

using ipp;

namespace MHApi.Analysis
{

    /// <summary>
    /// The return value of the realtime point processing
    /// Contains information about position, speed and any bout
    /// that has been completed at this time-point
    /// </summary>
    public struct PointAnalysis
    {
        /// <summary>
        /// The bout completed at this time
        /// or null if no bout was finished
        /// </summary>
        public Bout? CompletedBout;

        /// <summary>
        /// The original coordinate passed
        /// to the analysis
        /// </summary>
        public IppiPoint OriginalCoordinate;

        /// <summary>
        /// The track-smoothened coordinate
        /// </summary>
        public IppiPoint_32f SmoothenedCoordinate;

        /// <summary>
        /// The current instant speed in 
        /// pixels/second
        /// </summary>
        public float InstantSpeed;

        public PointAnalysis(Bout bout, IppiPoint coordinate, IppiPoint_32f smoothenedCoordinate, float instantSpeed)
        {
            CompletedBout = bout;
            OriginalCoordinate = coordinate;
            SmoothenedCoordinate = smoothenedCoordinate;
            InstantSpeed = instantSpeed;
        }

        public PointAnalysis(IppiPoint coordinate, IppiPoint_32f smoothenedCoordinate, float instantSpeed)
        {
            CompletedBout = null;
            OriginalCoordinate = coordinate;
            SmoothenedCoordinate = smoothenedCoordinate;
            InstantSpeed = instantSpeed;
        }
    }

    /// <summary>
    /// Class to smoothen tracks, compute instant speeds
    /// and detect bouts in realtime.
    /// </summary>
    public unsafe class RealtimeMovementAnalyzer : IDisposable
    {
        /// <summary>
        /// Collects information about the current state of bout detection.
        /// Whether we are currently analyzing a putative bout and what its
        /// characteristics are
        /// </summary>
        private struct BoutDetectionState
        {
            /// <summary>
            /// If true, signals that the fish
            /// may currently perform a bout
            /// </summary>
            public bool InPutativeBout;

            /// <summary>
            /// Counter of frames where speed
            /// is above threshold in the put. bout
            /// </summary>
            public int FramesInBout;

            /// <summary>
            /// The encountered peakspeed of the bout
            /// </summary>
            public float PeakSpeed;

            /// <summary>
            /// Starting frame of putative bout
            /// </summary>
            public int StartFrame;

            /// <summary>
            /// The currently believed peakframe
            /// </summary>
            public int PeakFrame;

            /// <summary>
            /// Last frame of putative bout
            /// </summary>
            public int EndFrame;

            /// <summary>
            /// Total displacememt of
            /// putative bout
            /// </summary>
            public float TotalDisplacement;

            /// <summary>
            /// The total number of frames at
            /// peakspeed in the putative bout
            /// </summary>
            public int FramesAtPeak;

            /// <summary>
            /// The (smoothened) position where
            /// the current putative bout started
            /// </summary>
            public IppiPoint_32f StartingCoordinate;

            /// <summary>
            /// Resets all values to default state at the start
            /// of a putative bout
            /// </summary>
            public void StartBout(float currentSpeed, int currentFrame, int frameRate)
            {
                InPutativeBout = true;
                FramesInBout = 1;
                PeakSpeed = currentSpeed;
                StartFrame = currentFrame;
                PeakFrame = currentFrame;
                EndFrame = currentFrame;
                TotalDisplacement = currentSpeed / frameRate;
                FramesAtPeak = 1;
            }
        }

        #region Fields

        /// <summary>
        /// The coordinate filter taps
        /// </summary>
        float* _taps;

        /// <summary>
        /// The (optional) speed filter taps
        /// </summary>
        float* _spdTaps;

        /// <summary>
        /// Initializer for our coordinate delay lines
        /// (will be 0)
        /// </summary>
        float* _dlyLines;

        /// <summary>
        /// Initializer for our (optional)
        /// speed delay lines
        /// </summary>
        float* _spdLines;

        /// <summary>
        /// The number of taps in our coordinate filter
        /// </summary>
        int _tapLength;

        /// <summary>
        /// The number of taps in our (optional) speed filter
        /// </summary>
        int _spdTapLength;

        /// <summary>
        /// Pointer to the filter state structure for our
        /// FIR filter of the x-coordinates
        /// </summary>
        IppsFIRState_32f* _filterStateX;

        /// <summary>
        /// The buffer in memory for the actual
        /// x-coordinate filter state structure
        /// </summary>
        byte* _stateMemX;

        /// <summary>
        /// Pointer to the filter state structure
        /// for our FIR filter of the y-coordinates
        /// </summary>
        IppsFIRState_32f* _filterStateY;

        /// <summary>
        /// The buffer in memory for the actual
        /// y-coordinate filter state structure
        /// </summary>
        byte* _stateMemY;

        /// <summary>
        /// Pointer to the filter state structure
        /// for our FIR filter of the instant speeds
        /// </summary>
        IppsFIRState_32f* _filterStateSpd;

        /// <summary>
        /// The buffer in memory for the actual
        /// speed filter state structure
        /// </summary>
        byte* _stateMemSpd;

        /// <summary>
        /// The index of the current
        /// coordinate
        /// </summary>
        int _index;

        /// <summary>
        /// The last smoothened
        /// x-position (for speed calculation)
        /// </summary>
        float _lastX;

        /// <summary>
        /// The last smoothened y-position
        /// (for speed calculation)
        /// </summary>
        float _lastY;

        /// <summary>
        /// The acquisition framerate
        /// to relate displacement to speed
        /// </summary>
        int _frameRate;

        /// <summary>
        /// The absolute instant speed
        /// threshold for bout detection
        /// </summary>
        float _speedThreshold;

        /// <summary>
        /// The minimum number of consecutive frames
        /// above speed threshold required for a bout
        /// </summary>
        int _minFramesPerBout;

        /// <summary>
        /// The maximum number of frames at peakspeed
        /// allowed for a bout
        /// </summary>
        int _maxFramesAtPeak;

        /// <summary>
        /// Keeps track of the current state of our
        /// bout detector
        /// </summary>
        BoutDetectionState _boutDetectionState;

        #endregion

        #region Constructor

        public RealtimeMovementAnalyzer(int frameRate, int smoothingWindowSize, float speedThreshold, int minFramesPerBout, int maxFramesAtPeak)
        {
            _frameRate = frameRate;
            _tapLength = smoothingWindowSize;
            _speedThreshold = speedThreshold;
            _minFramesPerBout = minFramesPerBout;
            _maxFramesAtPeak = maxFramesAtPeak;
            _index = 0;
            _lastX = 0;
            _lastY = 0;
            //don't use instant speed filtering
            _spdTapLength = 0;
            //allocate memory for taps and delay lines
            _taps = (float*)Marshal.AllocHGlobal(_tapLength * 4);
            _dlyLines = (float*)Marshal.AllocHGlobal(_tapLength * 4);
            //set filter taps - boxcar filter
            int i = 0;
            while (i < _tapLength)
                _taps[i++] = 1 / (float)_tapLength;
            //set delay lines to 0
            sp.ippsZero_32f(_dlyLines, _tapLength);
            //get memory requirements and allocate state structures
            int size = 0;
            sp.ippsFIRGetStateSize_32f(_tapLength, &size);
            _stateMemX = (byte*)Marshal.AllocHGlobal(size);
            _stateMemY = (byte*)Marshal.AllocHGlobal(size);
            //initialize state structures
            fixed (IppsFIRState_32f** ppState = &_filterStateX)
            {
                sp.ippsFIRInit_32f(ppState, _taps, _tapLength, _dlyLines, _stateMemX);
            }
            fixed (IppsFIRState_32f** ppState = &_filterStateY)
            {
                sp.ippsFIRInit_32f(ppState, _taps, _tapLength, _dlyLines, _stateMemY);
            }
            _boutDetectionState = new BoutDetectionState();
        }

        public RealtimeMovementAnalyzer(int frameRate, int smoothingWindowSize, int speedSmoothingWindowSize, float speedThreshold, int minFramesPerBout, int maxFramesAtPeak)
            :this(frameRate,smoothingWindowSize,speedThreshold,minFramesPerBout,maxFramesAtPeak)
        {
            _spdTapLength = speedSmoothingWindowSize;
            //allocate memory for speed taps and delay lines
            _spdTaps = (float*)Marshal.AllocHGlobal(_spdTapLength * 4);
            _spdLines = (float*)Marshal.AllocHGlobal(_spdTapLength * 4);
            //allocate memory for taps and delay lines
            //set filter taps - boxcar filter
            int i = 0;
            while (i < _spdTapLength)
                _spdTaps[i++] = 1 / (float)_spdTapLength;
            //set speed delay lines to 0
            sp.ippsZero_32f(_spdLines, _spdTapLength);
            //get memory requirements and allocate state structure
            int size = 0;
            sp.ippsFIRGetStateSize_32f(_spdTapLength, &size);
            _stateMemSpd = (byte*)Marshal.AllocHGlobal(size);
            //initialize state structure
            fixed (IppsFIRState_32f** ppState = &_filterStateSpd)
            {
                sp.ippsFIRInit_32f(ppState,_spdTaps, _spdTapLength, _spdLines, _stateMemSpd);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Resets the state of the analyzer.
        /// Delay lines get reinitialized to reset the filter
        /// and bout calling is stopped.
        /// </summary>
        public void Reset()
        {
            //reset our coordinate index and our last positions
            _index = 0;
            _lastX = 0;
            _lastY = 0;
            //re-blank the delay lines of the FIR filters
            sp.ippsFIRSetDlyLine_32f(_filterStateX, _dlyLines);
            sp.ippsFIRSetDlyLine_32f(_filterStateY, _dlyLines);
            if (_spdTapLength > 0)
                sp.ippsFIRSetDlyLine_32f(_filterStateSpd, _spdLines);
        }

        /// <summary>
        /// Processes the next point, filtering its position
        /// computing speeds and detecting bouts
        /// </summary>
        /// <param name="coordinate">The coordinate to analyze</param>
        /// <returns>The analysis of the given coordinate</returns>
        public PointAnalysis ProcessNextPoint(IppiPoint coordinate)
        {
            //set up locals for processing
            float x = coordinate.x;
            float y = coordinate.y;
            float xSmooth, ySmooth, speed;
            xSmooth = ySmooth = speed = 0;
            //optional filtered speed
            float spdSmooth = 0;
            //filter x-coordinate
            sp.ippsFIR_32f(&x, &xSmooth, 1, _filterStateX);
            //filter y-coordinate
            sp.ippsFIR_32f(&y, &ySmooth, 1, _filterStateY);
            //compute instant speed
            speed = (float)Math.Sqrt((xSmooth - _lastX) * (xSmooth - _lastX) + (ySmooth - _lastY) * (ySmooth - _lastY));
            speed *= 240;
            //if requested, also filter the instant-speed value
            if (_spdTapLength > 0)
                sp.ippsFIR_32f(&speed, &spdSmooth, 1, _filterStateSpd);
            else
                spdSmooth = speed;
            //update last coordinates
            _lastX = xSmooth;
            _lastY = ySmooth;

            //Bout detection
            if (_boutDetectionState.InPutativeBout)//we are currently adding frames to a new putative bout
            {
                if (spdSmooth < _speedThreshold)//putative bout is finished
                {
                    _boutDetectionState.InPutativeBout = false;//reset
                    if (_boutDetectionState.FramesInBout >= _minFramesPerBout && _boutDetectionState.FramesAtPeak <= _maxFramesAtPeak)//bout was valid, fill bout structure and return it
                    {
                        Bout bout = new Bout();
                        bout.BoutEnd = _index-1;
                        bout.BoutStart = _boutDetectionState.StartFrame;
                        bout.BoutPeak = _boutDetectionState.PeakFrame;
                        if (bout.BoutPeak == bout.BoutStart)
                            bout.BoutStart--;//don't start bout on peakframe
                        bout.PeakSpeed = _boutDetectionState.PeakSpeed;
                        bout.TotalDisplacement = _boutDetectionState.TotalDisplacement;
                        bout.StartingCoordinate = _boutDetectionState.StartingCoordinate;
                        bout.FinishCoordinate = new IppiPoint_32f(xSmooth, ySmooth);
                        //don't forget to update the index before we return
                        _index++;
                        return new PointAnalysis(bout, coordinate, new IppiPoint_32f(xSmooth, ySmooth), spdSmooth);
                    }
                }
                else//putative bout continues
                {
                    _boutDetectionState.FramesInBout++;
                    _boutDetectionState.TotalDisplacement += spdSmooth / _frameRate;
                    if (spdSmooth > _boutDetectionState.PeakSpeed)//new peak-speed detected
                    {
                        _boutDetectionState.PeakSpeed = spdSmooth;
                        _boutDetectionState.PeakFrame = _index;
                        _boutDetectionState.FramesAtPeak = 1;//reset
                    }
                    else if (spdSmooth == _boutDetectionState.PeakSpeed)//maybe not the best criterion - equality of float values very unlikely
                        _boutDetectionState.FramesAtPeak++;
                }
            }
            else//currently the fish is stationary
            {
                if (spdSmooth > _speedThreshold)//we should start a new putative bout
                {
                    _boutDetectionState.StartBout(spdSmooth, _index, _frameRate);
                    _boutDetectionState.StartingCoordinate.x = xSmooth;
                    _boutDetectionState.StartingCoordinate.y = ySmooth;
                }
            }
            //advance index
            _index++;
            //return our analysis
            return new PointAnalysis(coordinate, new IppiPoint_32f(xSmooth, ySmooth), spdSmooth);
        }

        #endregion


        #region IDisposable Members

        public bool IsDisposed {get; private set;}

        public void Dispose() {
            if (IsDisposed)
                return;
            if (_taps != null)
            {
                Marshal.FreeHGlobal((IntPtr)_taps);
                _taps = null;
            }
            if (_dlyLines != null)
            {
                Marshal.FreeHGlobal((IntPtr)_dlyLines);
                _dlyLines = null;
            }
            if (_stateMemX != null)
            {
                Marshal.FreeHGlobal((IntPtr)_stateMemX);
                _stateMemX = null;
            }
            if (_stateMemY != null)
            {
                Marshal.FreeHGlobal((IntPtr)_stateMemY);
                _stateMemY = null;
            }
            if (_spdTaps != null)
            {
                Marshal.FreeHGlobal((IntPtr)_spdTaps);
                _spdTaps = null;
            }
            if (_spdLines != null)
            {
                Marshal.FreeHGlobal((IntPtr)_spdLines);
                _spdLines = null;
            }
            if (_stateMemSpd != null)
            {
                Marshal.FreeHGlobal((IntPtr)_stateMemSpd);
                _stateMemSpd = null;
            }
            IsDisposed = true;
        }

        ~RealtimeMovementAnalyzer() {
            Dispose();
        }
        #endregion
    }
}
