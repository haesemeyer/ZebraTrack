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

using MHApi.Imaging;
using MHApi.DrewsClasses;

using ipp;

namespace MHApi.Tracking
{
    /// <summary>
    /// Fish-tail tracker using in-frame
    /// background differencing
    /// </summary>
    public unsafe class TailTracker : IDisposable
    {
        /// <summary>
        /// Represents the tracking result
        /// for one tail segment giving its angle relative
        /// to the previous segment as well as its end-coordinate
        /// </summary>
        public struct TailSegment
        {

            /// <summary>
            /// The delta-angle relative
            /// to the previous segment
            /// </summary>
            public double DeltaAngle;

            /// <summary>
            /// The cartesian tail coordinate
            /// identified for this tail segment
            /// </summary>
            public IppiPoint Coordinate;

            
            /// <summary>
            /// Constructs a new tailpoit
            /// </summary>
            /// <param name="angle">The angle of the segment on screen - 0degrees facing down</param>
            /// <param name="deltaAngle">The delta angle to the previous segment</param>
            /// <param name="distance">The distance from tailstart</param>
            /// <param name="coordinate">The cartesian coordinate of the tailpoint</param>
            public TailSegment(double deltaAngle, IppiPoint coordinate)
            {
                DeltaAngle = deltaAngle;
                Coordinate = coordinate;
            }
        }

        

        #region Members

        /// <summary>
        /// The image size this tracker
        /// will process
        /// </summary>
        IppiSize _imageSize;

        /// <summary>
        /// The region in which we expect to find the
        /// tail excluding morphology border pixels
        /// </summary>
        IppiROI _trackRegionInner;

        /// <summary>
        /// The region in which we expect to find the
        /// tail including morphology border pixels
        /// </summary>
        IppiROI _trackRegionOuter;

        /// <summary>
        /// Thread-safety of region updates
        /// </summary>
        object _regionLock = new object();

        /// <summary>
        /// The computed background
        /// </summary>
        Image8 _background;

        /// <summary>
        /// Our foreground
        /// </summary>
        Image8 _foreground;

        /// <summary>
        /// The thresholded foreground
        /// </summary>
        Image8 _thresholded;

        /// <summary>
        /// Processing intermediate buffer
        /// </summary>
        Image8 _calc1;

        /// <summary>
        /// The threshold used on the foreground
        /// to isolate the tail
        /// </summary>
        byte _threshold;

        /// <summary>
        /// The morphology object for background creation
        /// </summary>
        Morphology _strel;

        /// <summary>
        /// Morphology object for small whole filling
        /// </summary>
        Morphology _strel3x3;

        /// <summary>
        /// If true, indicates that the tail is
        /// brighter than the background
        /// </summary>
        bool _lightOnDark;

        /// <summary>
        /// The size of the closing mask
        /// </summary>
        int _morphSize;

        /// <summary>
        /// The number of tail-segments to track
        /// </summary>
        int _nSegments;

        /// <summary>
        /// The start of the tail
        /// </summary>
        IppiPoint _tailStart;

        /// <summary>
        /// The end of the tail
        /// </summary>
        IppiPoint _tailEnd;

        /// <summary>
        /// Framerate of acquisition - used to
        /// perform background calculation once a second
        /// </summary>
        int _frameRate;

        /// <summary>
        /// The current framenumber
        /// </summary>
        uint _frameNumber;

        /// <summary>
        /// The angles on the full scan-circle
        /// that the probe for tail-presence
        /// </summary>
        double[] _scanAngles;

        /// <summary>
        /// For each _scanAngle the corresponding
        /// coordinate offset in x and y
        /// </summary>
        IppiPoint[] _coordinateOffsets;

        /// <summary>
        /// The delta angle between two consecutive
        /// _pointsToScan
        /// </summary>
        double _angleStep;

        object _scanPointLock = new object();

        /// <summary>
        /// During tracking used to store all
        /// positive angles that contain foreground
        /// </summary>
        int* _angleStore;

        /// <summary>
        /// Whenever coordinates or parameters change
        /// we want to refresh the background
        /// </summary>
        bool _bgValid;

        /// <summary>
        /// The tail angle at which we start to scan
        /// previous segment angle -90 degrees
        /// </summary>
        const double ScanStartOffset = -1 * Math.PI / 2;

        /// <summary>
        /// We scan over a whole 180 degrees
        /// centered around the previous segment angle
        /// </summary>
        const double SweepSize = Math.PI;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new tail tracker
        /// </summary>
        /// <param name="regionToTrack">The ROI in which we should track the tail</param>
        /// <param name="tailStart">The designated starting point of the tail</param>
        /// <param name="tailEnd">The designated end point of the tail</param>
        public TailTracker(IppiSize imageSize, IppiPoint tailStart, IppiPoint tailEnd) : this(imageSize,tailStart,tailEnd,5){ }

        /// <summary>
        /// Creates a new tail tracker
        /// </summary>
        /// <param name="regionToTrack">The ROI in which we should track the tail</param>
        /// <param name="tailStart">The designated starting point of the tail</param>
        /// <param name="tailEnd">The designated end point of the tail</param>
        /// <param name="nsegments">The number of tail segments to track btw. start and end</param>
        public TailTracker(IppiSize imageSize, IppiPoint tailStart, IppiPoint tailEnd, int nsegments)
        {
            _threshold = 20;
            _morphSize = 8;
            _frameRate = 200;
            _strel = Morphology.GenerateDiskMask(_morphSize, imageSize);
            _strel3x3 = Morphology.Generate3x3Mask(imageSize);
            _nSegments = 5;
            _imageSize = imageSize;
            _tailStart = tailStart;
            _tailEnd = tailEnd;
            //set up our track regions based on the tail positions
            DefineTrackRegions();
            NSegments = nsegments;
            InitializeImageBuffers();//set up image buffers
            InitializeScanPoints();//create scan points appropriate for the tail parameters
            //Initialize our angle store for tracking (size never changes)
            _angleStore = (int*)System.Runtime.InteropServices.Marshal.AllocHGlobal(900 * 4);
        }

        #endregion

        #region Properties

        /// <summary>
        /// The threshold used on the foreground
        /// to isolate the tail
        /// </summary>
        public byte Threshold
        {
            get
            {
                return _threshold;
            }
            set
            {
                _threshold = value;
            }
        }

        /// <summary>
        /// The size of the morphology mask used
        /// for the background generating closing operation
        /// </summary>
        public int MorphSize
        {
            get
            {
                return _morphSize;
            }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException("MorphSize");
                //If the size changed, we need to recreate our
                //structuring element and redefine track regions
                lock (_regionLock)
                {
                    if (value != _morphSize)
                    {
                        if (_strel != null)
                            _strel.Dispose();
                        _strel = Morphology.GenerateDiskMask(value, _imageSize);
                        DefineTrackRegions();
                    }
                    _morphSize = value;
                }
            }
        }

        /// <summary>
        /// The number of tail-segments to track
        /// </summary>
        public int NSegments
        {
            get
            {
                return _nSegments;
            }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException("NSegments");
                _nSegments = value;
                //redefine scan points
                InitializeScanPoints();
            }
        }

        /// <summary>
        /// The start of the tail in absolute coordinates
        /// NOT in track region coordinates!
        /// </summary>
        public IppiPoint TailStart
        {
            get
            {
                return _tailStart;
            }
            set
            {
                if (value.x < 0|| value.x >= _imageSize.width || value.y < 0 || value.y >= _imageSize.height)
                    throw new ArgumentOutOfRangeException("TailStart");
                _tailStart = value;
                //redefine track region
                DefineTrackRegions();
                //redefine scan points
                InitializeScanPoints();
            }
        }

        /// <summary>
        /// The end of the tail in absolute coordinates
        /// NOT in track region coordinates
        /// </summary>
        public IppiPoint TailEnd
        {
            get
            {
                return _tailEnd;
            }
            set
            {
                if (value.x < 0 || value.x >= _imageSize.width || value.y < 0 || value.y >= _imageSize.height)
                    throw new ArgumentOutOfRangeException("TailEnd");
                _tailEnd = value;
                //redefine track regions
                DefineTrackRegions();
                //redefine scan points
                InitializeScanPoints();
            }
        }

        /// <summary>
        /// If true indicates that the tail is
        /// lighter than the background
        /// </summary>
        public bool LightOnDark
        {
            get
            {
                return _lightOnDark;
            }
            set
            {
                _lightOnDark = value;
                _bgValid = false;
            }
        }

        /// <summary>
        /// The current background image
        /// </summary>
        public Image8 Background
        {
            get
            {
                return _background;
            }
        }

        /// <summary>
        /// The current foreground image
        /// </summary>
        public Image8 Foreground
        {
            get
            {
                return _foreground;
            }
        }

        /// <summary>
        /// The current thresholded foreground
        /// </summary>
        public Image8 Thresholded
        {
            get
            {
                return _thresholded;
            }
        }

        /// <summary>
        /// The acquisition framerate.
        /// Used to clock background updates
        /// </summary>
        public int FrameRate
        {
            get
            {
                return _frameRate;
            }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException("FrameRate");
                _frameRate = value;
            }
        }

        /// <summary>
        /// The length of each tail segment
        /// in pixels
        /// </summary>
        public double SegmentLength
        {
            get
            {
                return Distance.Euclidian(TailEnd, TailStart) / NSegments;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes all internal buffers based on the
        /// size of the current tracking region
        /// </summary>
        protected virtual void InitializeImageBuffers()
        {
            //create images
            _background = new Image8(_imageSize);
            _foreground = new Image8(_imageSize);
            _thresholded = new Image8(_imageSize);
            _calc1 = new Image8(_imageSize);
            //blank foreground - to ensure that the border is always black!
            ip.ippiSet_8u_C1R(0, _foreground.Image, _foreground.Stride, _imageSize);
        }

        /// <summary>
        /// Based on the current segment length computes the
        /// scanpoint offsets for each angle used during tracking
        /// </summary>
        protected virtual void InitializeScanPoints()
        {
            //Compute the angle step. Ideally we want to scan every pixel
            //on a 360 degree circle around the current point with radius
            //SegmentLength - so set angle step and NAngles to roughly
            //give us this coverage

            bool Up = false;//true indicates both a vertical up and a horizontal left facing tail

            //TODO: Experimental support for horizontal and upward facing tails added - however, left/right might be wrong!
            if (!TailIsVertical())
            {
                System.Diagnostics.Debug.WriteLine("Warning: Horizontal tail implementation is experimental.");
                if (TailStart.x > TailEnd.x)
                    Up = true;
            }
            else if (TailStart.y > TailEnd.y)
            {
                Up = true;
            }

            int circumference = (int)Math.Floor(2 * Math.PI * SegmentLength);
            _angleStep = 2 * Math.PI / circumference;
            
            
            lock (_scanPointLock)
            {
                //Create angle and offset arrays
                _scanAngles = new double[circumference];
                _coordinateOffsets = new IppiPoint[circumference];

                
                //establish scan angles and offsets:
                //loop over sweep angles - points are assigned correctly for downward facing tail
                //use hack for horizontal tail (swap x and y) as well as for upward facing tail (invert both x and y in place)
                for (int j = 0; j < _scanAngles.Length; j++)
                {
                    //facing down (up, left, right) is centered in the array
                    _scanAngles[j] = j * _angleStep - Math.PI;

                    int offX = (int)Math.Floor(SegmentLength * Math.Sin(_scanAngles[j]));
                    int offY = (int)Math.Floor(SegmentLength * Math.Cos(_scanAngles[j]));
                    if (Up)//rotate coordinates by 180
                    {
                        offX *= -1;
                        offY *= -1;
                    }
                    if(!TailIsVertical())//swap role of x and y coordinates
                    {
                        int o = offX;
                        offX = offY;
                        offY = o;
                    }
                    _coordinateOffsets[j] = new IppiPoint(offX, offY);
                }
            }//release lock
        }

        /// <summary>
        /// Uses tail start and end positions as well
        /// as the image size to define the track region sizes
        /// </summary>
        private void DefineTrackRegions()
        {
            lock (_regionLock)
            {
                int startx, starty, width, height;
                //determine if tail is horizontal or vertical
                if (TailIsVertical())
                {
                    starty = TailStart.y < TailEnd.y ? TailStart.y : TailEnd.y;
                    height = Math.Abs(TailEnd.y - TailStart.y);
                    startx = (int)((TailStart.x + TailEnd.x) / 2 - height);//ROI is centered around the tail
                    width = 2 * height;
                }
                else
                {
                    startx = TailStart.x < TailEnd.x ? TailStart.x : TailEnd.x;
                    width = Math.Abs(TailEnd.x - TailStart.x);
                    starty = (int)((TailStart.y + TailEnd.y) / 2 - width);
                    height = 2 * width;
                }
                //trim to fit into image dimensions
                if (startx < 0)
                    startx = 0;
                if (starty < 0)
                    starty = 0;
                if (startx + width > _imageSize.width)
                    width = _imageSize.width - startx + 1;
                if (starty + height > _imageSize.height)
                    height = _imageSize.height - starty + 1;
                //compute and trim outer coordinates - morphological operations need to leave
                //border pixels and don't compute anything useful within the border for those
                //border pixels - therefore we leave a border twice the size of our structuring
                //element
                int outerx = startx - 2 * _strel.MaskWidth;
                int outery = starty - 2 * _strel.MaskHeight;
                int outerw = width + 2 * _strel.MaskWidth;
                int outerh = height + 2 * _strel.MaskHeight;
                if (outerx < 0)
                    outerx = 0;
                if (outery < 0)
                    outery = 0;
                if (outerx + outerw > _imageSize.width)
                    outerw = _imageSize.width - outerx + 1;
                if (outery + outerh > _imageSize.height)
                    outerh = _imageSize.height - outery + 1;
                //create regions
                _trackRegionInner = new IppiROI(startx, starty, width, height);
                _trackRegionOuter = new IppiROI(outerx, outery, outerw, outerh);
            }
            _bgValid = false;
        }

        /// <summary>
        /// Uses tailstart and end to determine
        /// tail orientation
        /// </summary>
        /// <returns>True if the tail runs in vertical orientation</returns>
        private bool TailIsVertical()
        {
            var distX = Math.Abs(TailEnd.x - TailStart.x);
            var distY = Math.Abs(TailEnd.y - TailStart.y);
            return (distY > distX);
        }

        /// <summary>
        /// Forces the tracker to update the background
        /// at the next frame
        /// </summary>
        public void ForceBGUpdate()
        {
            _bgValid = false;
        }

        /// <summary>
        /// Tracks the tailsegments on the supplied image and returns
        /// the angles and distances of each segment from the tailstart
        /// </summary>
        /// <param name="image">The image on which to id the tail</param>
        /// <returns>NSegments number of TailPoints</returns>
        public TailSegment[] TrackTail(Image8 image)
        {
            lock (_regionLock)
            {
                //CURRENTLY ONLY DOWNWARD FACING TAILS ARE PROPERLY VERIFIED!!!
                //Generate background by morphology operation - 10 times a second or whenever our coordinates changed
                //in the default case where the tail is darker than the background, using closing operation otherwise opening
                if (_frameNumber % (_frameRate/10) == 0 || !_bgValid)
                {
                    if (!_bgValid)
                    {
                        //if our regions changed, reblank our images!
                        ip.ippiSet_8u_C1R(0, _background.Image, _background.Stride, _background.Size);
                        ip.ippiSet_8u_C1R(0, _foreground.Image, _foreground.Stride, _foreground.Size);
                        ip.ippiSet_8u_C1R(0, _thresholded.Image, _thresholded.Stride, _thresholded.Size);
                    }
                    if (_lightOnDark)
                        _strel.Open(image, _background, _trackRegionOuter);
                    else
                        _strel.Close(image, _background, _trackRegionOuter);
                    _bgValid = true;
                }
                //Compute foreground
                IppHelper.IppCheckCall(cv.ippiAbsDiff_8u_C1R(_background[_trackRegionInner.TopLeft], _background.Stride, image[_trackRegionInner.TopLeft], image.Stride, _foreground[_trackRegionInner.TopLeft], _foreground.Stride, _trackRegionInner.Size));
                //Threshold
                BWImageProcessor.Im2Bw(_foreground, _calc1, _trackRegionInner, _threshold);
                //Fill small holes
                _strel3x3.Close(_calc1, _thresholded, _trackRegionOuter);
            }
            //Tracking concept: We track the angle of each segment end (TailStart+SegmentLength:TailEnd)
            //as the angular displacement from the previous segment end
            //To do this we define one full-circle with radius SegmentLength and an angle step corresponding
            //to ~1 Pixel. Then for each angle we pre-compute in InitializeScanPoints the corresponding x
            //and y offsets. From the full circle set we track -90 to +90 degrees around the angle of the
            //previous segment - for the initial segment that angle will be 0. For each segment we will return
            //the segment angle and its associated end-point coordinate
            var retval = new TailSegment[_nSegments];
            lock (_scanPointLock)
            {
                //The index of the absolute angle of the previous segment in our angle sweep array
                //determines which angles we sweep over for the next segment
                int prevAngleIndex = _scanAngles.Length / 2;
                int nAnglesPerHalfPi = prevAngleIndex / 2;//this is the number of entries in the array that we have to walk to cover 90 degrees
                //we scan beginning with one segment length away from tail start and then walk
                //down the tail rather than using circles fixed arount the tailstart
                IppiPoint prevSegmentEnd = TailStart;
                
                //loop over tail segments
                for (int i = 0; i < _nSegments; i++)
                {
                    //loop over scan-points from -pi/2 to +pi/2 (i.e. nAnglesPerHalfPi) around previous segment angle
                    //interpreting the scan points as offsets around the previous tail segment
                    //end point
                    //then find tail angle of this segment
                    int pointsFound = 0;//the number of non-zero pixels identified
                    for (int j = -1 * nAnglesPerHalfPi; j < nAnglesPerHalfPi + 1; j++)
                    {
                        //Determine the index to scan - usually this will simply be "prevAngleIndex+j" however we
                        //have to loop around our angle array properly in case we screen more extreme angles
                        int currIndex = prevAngleIndex + j;
                        if (currIndex >= _scanAngles.Length)
                            currIndex = currIndex % _scanAngles.Length;
                        else if (currIndex < 0)
                            currIndex = currIndex + _scanAngles.Length;

                        //If current point is outside of the image, ignore it
                        IppiPoint pt = new IppiPoint(_coordinateOffsets[currIndex].x + prevSegmentEnd.x, _coordinateOffsets[currIndex].y + prevSegmentEnd.y);
                        if (pt.x < 0 || pt.y < 0 || pt.x >= _imageSize.width || pt.y >= _imageSize.height)
                            continue;
                        //if the value at the current point >0 we mark that angle as valid
                        //by storing the index in our anglestore
                        if (*_thresholded[pt] > 0)
                        {
                            _angleStore[pointsFound] = currIndex;
                            pointsFound++;
                        }
                    }
                    //find the median point in our angle store if we have more than 2 points stored
                    //the value in our angle store will directly give us the absolute screen angle of the segment
                    //as well as the coordinate offset which we can used together with the previous segment endpoint
                    //to compute the tail segment end coordinate - to get the delta angle we need to get the difference
                    //between the stored index and the previously stored index, prevAngleIndex
                    //after computing the appropriate return values we update prevAngleIndex with the index from the angle store
                    //and prevSegmentEnd with the returned coordinate of the current tail segment
                    double deltaTailAngle;
                    IppiPoint coordinate;
                    int pos;//the index in the angle store that we determine to be the tail-center

                    if (pointsFound == 0)//we have lost the tail - no reason to keep tracking - fill remaining positions in array with NaNs for their angle
                    {
                        for (int k = i; k < _nSegments; k++)
                        {
                            deltaTailAngle = double.NaN;
                            coordinate = new IppiPoint();
                            retval[k] = new TailSegment(deltaTailAngle, coordinate);
                        }
                        break;
                    }
                    else if (pointsFound < 3)
                    {
                        pos = _angleStore[0];
                    }
                    else
                    {
                        //we want the angle at the median position of valid points
                        //we don't compute intermediate positions however (we should be able to afford this since our angle step is so small), so in case
                        //of an even number of points, the top of the lower half currently
                        //wins - since array indexing is zero based, we have to subtract 1
                        //from the computed median position
                        pos = _angleStore[(int)(Math.Ceiling(pointsFound / 2.0) - 1)];
                    }
                    coordinate = new IppiPoint(prevSegmentEnd.x + _coordinateOffsets[pos].x, prevSegmentEnd.y + _coordinateOffsets[pos].y);
                    deltaTailAngle = (pos - prevAngleIndex) * _angleStep;
                    //the condition above of wrapping around the "angle circle":if (currIndex >= _scanAngles.Length)....
                    //results in one very large (close to +360 or -360) angle of opposite sign at the switch point
                    //however, since we usually only scan over offsets from +90 to -90 this case is easy to spot
                    //and remedy:
                    if (deltaTailAngle > 90)
                        deltaTailAngle = deltaTailAngle - 360;//this should create the appropriate tail angle btw. 0 and -90
                    else if (deltaTailAngle < -90)
                        deltaTailAngle = 360 + deltaTailAngle;//this should create the appropriate tail angle btw. 0 and +90
                    prevAngleIndex = pos;
                    prevSegmentEnd = coordinate;
                    retval[i] = new TailSegment(deltaTailAngle, coordinate);
                }//loop over tail segments
            }
            _frameNumber++;
            return retval;
        }



        #endregion

        #region Cleanup

        public bool IsDisposed { get; private set; }

        protected void Dispose(bool IsDisposing)
        {
            if (_background != null)
            {
                _background.Dispose();
                _background = null;
            }
            if (_foreground != null)
            {
                _foreground.Dispose();
                _foreground = null;
            }
            if (_thresholded != null)
            {
                _thresholded.Dispose();
                _thresholded = null;
            }
            if (_calc1 != null)
            {
                _calc1.Dispose();
                _calc1 = null;
            }
            if (_strel != null)
            {
                _strel.Dispose();
                _strel = null;
            }
            if(_strel3x3 != null)
            {
                _strel3x3.Dispose();
                _strel = null;
            }
            if (_angleStore != null)
            {
                System.Runtime.InteropServices.Marshal.FreeHGlobal((IntPtr)_angleStore);
                _angleStore = null;
            }
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;
            else
            {
                Dispose(true);
                IsDisposed = true;
                GC.SuppressFinalize(this);
            }
        }

        ~TailTracker()
        {
            if (!IsDisposed)
                System.Diagnostics.Debug.WriteLine("Forgot to dispose tail tracker!");
            Dispose(false);
        }

        #endregion

       
    }
}
