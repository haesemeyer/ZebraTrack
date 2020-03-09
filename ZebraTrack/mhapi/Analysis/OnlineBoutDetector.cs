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
using System.Threading;
using System.Windows.Threading;

using MHApi.Threading;

namespace MHApi.Analysis
{
    /// <summary>
    /// Class managing  bout detection on a separate thread
    /// for chunks of centroid data (quasi-online)
    /// </summary>
    /// <remarks>
    /// The class recieves centroid data from a tracker.
    /// Whenever a preset number of centroids has been recieved
    /// bout statistics on this centroid stretch are computed
    /// </remarks>
    public  class OnlineBoutDetector : IDisposable
    {
        //Functionality and Implementation:
        //Owner creates the object with suitable parameters for filters
        //and information on how many frames should be analyzed with each chunk.
        //The owner then regularly (f.e. every frame) passes new centroid data.
        //The centroid data gets filled into one of the centroid buffers which
        //have space for chunk-size elements. If the current buffer is filled
        //the trace gets analyzed on a separate thread while new data is now
        //being added to the other buffer.
        //At the same time the owner can listen to the NewCalcDoneSignal event
        //which will be signaled by the analysis thread when analysis is finished.

        #region Fields

        /// <summary>
        /// The (two) internal centroid buffers
        /// </summary>
        CentroidBuffer[] _centBuffers;

        /// <summary>
        /// The instant speed buffer used in processing
        /// </summary>
        InstantSpeedBuffer _isb;

        /// <summary>
        /// The buffer that should currently
        /// recieve new centroid data
        /// </summary>
        int _currentActiveBuffer;

        /// <summary>
        /// The number of centroids to wait for
        /// before analysis starts
        /// </summary>
        int _chunkSize;

        /// <summary>
        /// The number of centroids received
        /// since the last analysis started
        /// </summary>
        int _centroidsReceived;

        /// <summary>
        /// The window size for averaging
        /// in track smoothing
        /// </summary>
        int _smoothingWindowSize;

        /// <summary>
        /// The framerate for imaging - used
        /// to normalize speeds
        /// </summary>
        int _frameRate;

        /// <summary>
        /// For bout detection the absolute speed threshold
        /// </summary>
        float _speedThreshold;

        /// <summary>
        /// The minimum number of frames a valid bout can have
        /// </summary>
        int _minFramesPerBout;

        /// <summary>
        /// The maximum number of frames during which
        /// peakspeed can occur in a bout
        /// </summary>
        int _maxFramesAtPeak;

        /// <summary>
        /// Used to signal other threads that the next chunk
        /// has been analyzed
        /// </summary>
        AutoResetEvent _newCalcDoneSignal;

        /// <summary>
        /// Used internally to signal the analysis thread
        /// to start analysis
        /// </summary>
        AutoResetEvent _bufferReady;

        /// <summary>
        /// The thread handling analysis of centroid data
        /// </summary>
        Worker _analysisThread;

        /// <summary>
        /// Used to smoothen tracks before speed
        /// calculation and bout calling
        /// </summary>
        CoordinateSmoother _smoother;

        /// <summary>
        /// Used to compute instant speeds
        /// and call bouts
        /// </summary>
        MovementAnalyzer _mova;

        /// <summary>
        /// The bout summary of the last analysis
        /// </summary>
        BoutSummary _lastAnalysis;

        #endregion

        /// <summary>
        /// Creates a new online bout detector with default
        /// processing parameters for a 240Hz acquisition at ~20 pixels / fish
        /// </summary>
        /// <param name="chunkSize">The number of centroids to accumulate before processing</param>
        public OnlineBoutDetector(int chunkSize)
            : this(chunkSize, 240, 11, 18, 20, 5)
        { }

        /// <summary>
        /// Creates a new online bout detector with custom
        /// bout detection parameters
        /// </summary>
        /// <param name="chunkSize">The number of centroids to accumulate before processing</param>
        /// <param name="frameRate">The framerate of imaging</param>
        /// <param name="smoothingWindowSize">The size of the filtering window for centroid smoothing</param>
        /// <param name="speedThreshold">The absolute speed threshold in bout detection</param>
        /// <param name="minFramesPerBout">The minimum number of frames per bout</param>
        /// <param name="maxFramesAtPeak">The maximum number of peak frames in each bout</param>
        public OnlineBoutDetector(int chunkSize, int frameRate, int smoothingWindowSize, float speedThreshold, int minFramesPerBout, int maxFramesAtPeak)
        {
            if (frameRate < 1)
                throw new ArgumentOutOfRangeException("The class only works with framerates of 1Hz or larger");
            if (chunkSize < frameRate)
                throw new ArgumentOutOfRangeException("The chunksize has to be the framerate or larger.");
            _frameRate = frameRate;
            _chunkSize = chunkSize;
            _smoothingWindowSize = smoothingWindowSize;
            _speedThreshold = speedThreshold;
            _minFramesPerBout = minFramesPerBout;
            _maxFramesAtPeak = maxFramesAtPeak;
            _centBuffers = new CentroidBuffer[2];
            _centBuffers[0] = new CentroidBuffer(_chunkSize);
            _centBuffers[1] = new CentroidBuffer(_chunkSize);
            _isb = new InstantSpeedBuffer(_chunkSize);
            _smoother = new CoordinateSmoother(_chunkSize, _smoothingWindowSize);
            _mova = new MovementAnalyzer(_chunkSize);
            _currentActiveBuffer = 0;
            _centroidsReceived = 0;
            _bufferReady = new AutoResetEvent(false);
            _newCalcDoneSignal = new AutoResetEvent(false);
            //start our analysis thread
            _analysisThread = new Worker(AnalyzeBouts, true, 3000);
        }

        #region Properties

        /// <summary>
        /// Is signaled when the next chunk of data
        /// has been analyzed
        /// </summary>
        public AutoResetEvent NewCalcDoneSignal
        {
            get
            {
                return _newCalcDoneSignal;
            }
        }

        /// <summary>
        /// The number of centroids in each chunk. After
        /// recieving this number of centroids, analysis will
        /// start
        /// </summary>
        public int ChunkSize
        {
            get
            {
                return _chunkSize;
            }
        }

        /// <summary>
        /// The number of centroids received
        /// since the last analysis started
        /// </summary>
        public int CentroidsReceived
        {
            get
            {
                return _centroidsReceived;
            }
        }

        /// <summary>
        /// The result of the last conducted
        /// bout analysis
        /// </summary>
        public BoutSummary LastAnalysis
        {
            get
            {
                return _lastAnalysis;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// The thread procedure that performs on-demand bout analysis
        /// </summary>
        /// <param name="stop"></param>
        /// <param name="dispOwner"></param>
        void AnalyzeBouts(AutoResetEvent stop, Dispatcher dispOwner)
        {
            CentroidBuffer toProcess;//recieves handle of the buffer we want to analyze
            while (!stop.WaitOne(0))
            {
                //we don't do anaything unless we are signaled - however we should periodically return to make
                //sure that we can catch a stop event, even if we never get signaled for work
                if (_bufferReady.WaitOne(500))
                {
                    //process chunk
                    if (_currentActiveBuffer == 0)//we need to process buffer 1 as 0 receives centroids at the moment
                    {
                        toProcess = _centBuffers[1];
                    }
                    else//process buffer 0
                    {
                        toProcess = _centBuffers[0];
                    }
                    _smoother.SmoothenTrack(toProcess, _smoothingWindowSize);
                    _mova.ComputeInstantSpeeds(toProcess, _frameRate, _isb);
                    _lastAnalysis = _mova.AnalyzeBouts(_isb, _speedThreshold, _minFramesPerBout, _maxFramesAtPeak, _frameRate);
                    //analysis done, communicate to the outside by setting the appropriate event
                    _newCalcDoneSignal.Set();
                }
            }
        }

        /// <summary>
        /// Adds a new centroid to the centroid stream
        /// </summary>
        /// <param name="centroid">The centroid to add</param>
        /// <returns>True if analysis has been started</returns>
        public bool AddCentroid(ipp.IppiPoint_32f centroid)
        {
            bool retval = false;
            
            _centBuffers[_currentActiveBuffer][_centroidsReceived++] = centroid;//add centroid, post increment, our buffer is 0-based indexed

            if (_centroidsReceived == _chunkSize) //we need to switch buffers and start analysis
            {
                _currentActiveBuffer = (_currentActiveBuffer + 1) % 2;//change active buffer number - AT THIS POINT WE JUST ASSUME THAT ANALYSIS HAS FINISHED!!!
                _centroidsReceived = 0;//reset number of received centroids
                _bufferReady.Set();//kick off analysis thread
                retval = true;//we started analysis so we return true
            }

            return retval;
        }

        #endregion


        #region IDisposable Members

        public bool IsDisposed {get; private set;}

        public void Dispose() {
            if (IsDisposed)
                return;
            if (_analysisThread != null)
            {
                _analysisThread.Dispose();
                _analysisThread = null;
            }
            if (_smoother != null)
            {
                _smoother.Dispose();
                _smoother = null;
            }
            if (_mova != null)
            {
                _mova.Dispose();
                _mova = null;
            }
            if (_isb != null)
            {
                _isb.Dispose();
                _isb = null;
            }
            IsDisposed = true;
        }

        ~OnlineBoutDetector() {
            Dispose();
        }
        #endregion

    }
}
