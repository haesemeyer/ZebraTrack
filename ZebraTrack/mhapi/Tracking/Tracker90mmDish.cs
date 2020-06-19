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
using System.Runtime.InteropServices;

using ipp;

using MHApi.Imaging;
using MHApi.DrewsClasses;

namespace MHApi.Tracking
{
    public unsafe class Tracker90mmDish : IDisposable
    {
        #region Members

        /// <summary>
        /// Our background model
        /// </summary>
        SelectiveUpdateBGModel _bgModel;

        /// <summary>
        /// Internal volatile buffer used for image calculations
        /// </summary>
        Image8 _calc;

        /// <summary>
        /// The image after foreground subtraction and thresholding
        /// </summary>
        Image8 _foreground;

        /// <summary>
        /// The image after background subtraction
        /// </summary>
        Image8 _bgSubtracted;

        /// <summary>
        /// Buffer used internally by ipp's label markers
        /// function
        /// </summary>
        byte* _markerBuffer;

        /// <summary>
        /// Pointer to the momentState structure used internally by ipp
        /// </summary>
        IppiMomentState_64f* _momentState;

        /// <summary>
        /// The current frame index
        /// </summary>
        int _frame;

        /// <summary>
        /// The threshold used to extract the foreground
        /// </summary>
        byte _threshold;

        /// <summary>
        /// The minimum area a blob can have and still
        /// be considered a fish
        /// </summary>
        int _minArea;

        /// <summary>
        /// The maximum area a blob can have and still
        /// be considered a fish
        /// </summary>
        int _maxAllowedArea;

        /// <summary>
        /// The mininum eccentricity a blob
        /// can have and still be considered a fish
        /// </summary>
        double _minEccentricity;

        /// <summary>
        /// If a blob has an area larger than this it
        /// will be used to update knowledge about the 
        /// tracking region
        /// </summary>
        int _fullTrustMinArea;

        /// <summary>
        /// The number of frames to use in our background model
        /// </summary>
        int _framesInBackground;

        /// <summary>
        /// The number of frames before we actually start tracking
        /// </summary>
        int _framesInitialBackground;

        /// <summary>
        /// Determines every how many frames the background
        /// will be updated
        /// </summary>
        int _bgUpdateEvery;

        /// <summary>
        /// The fish extracted from the previous frame
        /// or null, if no trustworthy fish was found
        /// </summary>
        BlobWithMoments _previousFish;

        /// <summary>
        /// The center of the dish which is used
        /// to discriminate "real fish" from "reflections"
        /// </summary>
        IppiPoint _dishCenter;

        /// <summary>
        /// The number of pixels around the fish
        /// in which we look for the fish in the
        /// next frame
        /// </summary>
        int _searchRegionSize;

        /// <summary>
        /// If true, opening operation on the image
        /// will be used to mitigate bright ray artefact
        /// </summary>
        bool _removeCMOSISBrightLineArtefact;

        /// <summary>
        /// Morphology object for noise reductions
        /// </summary>
        Morphology _strel3x3;

        /// <summary>
        /// The ROI representing the whole image
        /// </summary>
        readonly IppiROI _imageROI;

        /// <summary>
        /// Top left and bottom right corner of search region
        /// </summary>
        int tl_x, tl_y, br_x, br_y;

        /// <summary>
        /// Search region around previous fish location
        /// </summary>
        IppiROI _searchRegion = new IppiROI(0, 0, 0, 0);

        /// <summary>
        /// During tracking holds all potential fish-blobs
        /// detected within an image / search region
        /// </summary>
        BlobWithMoments[] _blobsDetected;

        /// <summary>
        /// Region for bounding box determination
        /// </summary>
        IppiSize _bboxScan = new IppiSize();

        /// <summary>
        /// Region for median filtering
        /// </summary>
        IppiSize _mFiltSize = new IppiSize();

        #endregion

        #region Properties

        /// <summary>
        /// If true, opening operation on the image
        /// will be used to mitigate bright ray artefact
        /// </summary>
        public bool RemoveCMOSISBrightLineArtefact
        {
            get
            {
                return _removeCMOSISBrightLineArtefact;
            }
            set
            {
                _removeCMOSISBrightLineArtefact = value;
            }
        }

        /// <summary>
        /// The current background subtracted image
        /// </summary>
        public Image8 BGSubtracted
        {
            get
            {
                return _bgSubtracted;
            }
        }

        /// <summary>
        /// The current foreground after thresholding
        /// and marker labeling
        /// </summary>
        public Image8 Foreground
        {
            get
            {
                return _foreground;
            }
        }

        /// <summary>
        /// The current background
        /// </summary>
        public Image8 Background
        {
            get
            {
                return _bgModel.Background;
            }
        }

        /// <summary>
        /// The threshold used to extract the foreground
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
        /// The minimum area a blob can have and
        /// still be considered a fish
        /// </summary>
        public int MinArea
        {
            get
            {
                return _minArea;
            }
            set
            {
                _minArea = value;
            }
        }

        /// <summary>
        /// The maximum area a blob can have and
        /// still be considered a fish
        /// </summary>
        public int MaxAllowedArea
        {
            get
            {
                return _maxAllowedArea;
            }
            set
            {
                _maxAllowedArea = value;
            }
        }

        /// <summary>
        /// The minimum eccentricity a blob can
        /// have and still be considered a fish
        /// </summary>
        public double MinEccentricity
        {
            get
            {
                return _minEccentricity;
            }
            set
            {
                _minEccentricity = value;
            }
        }

        /// <summary>
        /// If a blob has an area larger than this it
        /// will be used to update knowledge about the 
        /// tracking region
        /// </summary>
        public int FullTrustMinArea
        {
            get
            {
                return _fullTrustMinArea;
            }
            set
            {
                _fullTrustMinArea = value;
            }
        }

        /// <summary>
        /// The number of frames it takes for our background to approximate
        /// the foreground at 63.2% (since exponential decay of pixels
        /// into the background)
        /// It takes 4.6 times as many frames to reach 99% of the foreground!!
        /// </summary>
        public int FramesInBackground
        {
            get
            {
                return _framesInBackground;
            }
            set
            {
                if (_frame > 0)
                    throw new InvalidOperationException("Can't update frames in background during tracking");
                _framesInBackground = value;
            }
        }

        /// <summary>
        /// The number of frames before we start tracking
        /// </summary>
        public int FramesInitialBackground
        {
            get
            {
                return _framesInitialBackground;
            }
            set
            {
                if (_frame > 0)
                    throw new InvalidOperationException("Can't update initial background frame number during tracking");
                _framesInitialBackground = value;
            }
        }

        /// <summary>
        /// Background will only be updated every BGUpdateEvery frames
        /// </summary>
        public int BGUpdateEvery
        {
            get
            {
                return _bgUpdateEvery;
            }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(BGUpdateEvery), "Has to be 1 or larger");
                _bgUpdateEvery = value;
            }
        }

        /// <summary>
        /// The current frame
        /// </summary>
        public int Frame
        {
            get
            {
                return _frame;
            }
        }

        /// <summary>
        /// The center of the dish. If two equally sized object are
        /// found in the foreground the object that is closer to this
        /// point is presumed to be the fish even if it is smaller.
        /// </summary>
        public IppiPoint DishCenter
        {
            get
            {
                return _dishCenter;
            }
            set
            {
                _dishCenter = value;
            }
        }

        /// <summary>
        /// The size (width and height) of the region used
        /// to search for the fish in subsequent frames
        /// </summary>
        public int SearchRegionSize
        {
            get
            {
                return _searchRegionSize;
            }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException("SearchRegionSize", "Search region size has to be greater than 0!");
                _searchRegionSize = value;
            }
        }

        #endregion

        #region Constructor

        public Tracker90mmDish(int imageWidth, int imageHeight, IppiPoint dishCenter)
        {
            _foreground = new Image8(imageWidth, imageHeight);
            _bgSubtracted = new Image8(imageWidth, imageHeight);
            _calc = new Image8(imageWidth, imageHeight);
            _dishCenter = dishCenter;
            int bufferSize = 0;
            IppHelper.IppCheckCall(cv.ippiLabelMarkersGetBufferSize_8u_C1R(new IppiSize(imageWidth, imageHeight), &bufferSize));
            _markerBuffer = (byte*)Marshal.AllocHGlobal(bufferSize);
            int momentSize = 0;
            IppHelper.IppCheckCall(ip.ippiMomentGetStateSize_64f(IppHintAlgorithm.ippAlgHintNone, &momentSize));
            _momentState = (IppiMomentState_64f*)Marshal.AllocHGlobal(momentSize);
            //let ipp decide whether to give accurate or fast results
            IppHelper.IppCheckCall(ip.ippiMomentInit_64f(_momentState, IppHintAlgorithm.ippAlgHintNone));
            _frame = 0;
            //populate tracking parameters with default values
            _threshold = 6;
            _minArea = 11;
            _maxAllowedArea = 120;
            _minEccentricity = 0.3;
            _fullTrustMinArea = 20;
            _imageROI = new IppiROI(0, 0, imageWidth, imageHeight);
            _searchRegionSize = 90;
            _removeCMOSISBrightLineArtefact = false;
            _strel3x3 = Morphology.Generate3x3Mask(_foreground.Size);
            //The following calculation for FramesInBackground means that after ~30s of movie
            //a stationary object will have dissappeared into the background (at 63% level)
            FramesInBackground = (int)((30 * 250));
            FramesInitialBackground = 2 * 30 * 250;
            BGUpdateEvery = 2;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Given an image identifies a fish in it and returns
        /// it's properties such as position and heading
        /// </summary>
        /// <param name="image">The image to be tracked.</param>
        /// <returns>A blob representation of the fish</returns>
        public BlobWithMoments Track(Image8 image)
        {
            if (IsDisposed)
                throw new ObjectDisposedException("Tracker90mmDish", "Can't track after disposal!");

            BlobWithMoments currentFish = null;

            if (RemoveCMOSISBrightLineArtefact)
            {
                //Do as two step to get information back into image
                _strel3x3.Erode(image, _calc, _imageROI);
                _strel3x3.Dilate(_calc, image, _imageROI);
            }

            if (_frame > FramesInitialBackground)
            {
                //if _previousFish is present we only check an area around it
                if (_previousFish != null)
                {
                    //compute search region
                    //top-left
                    tl_x = _previousFish.Centroid.x - _searchRegionSize;
                    tl_y = _previousFish.Centroid.y - _searchRegionSize;
                    tl_x = tl_x < 0 ? 0 : tl_x;
                    tl_y = tl_y < 0 ? 0 : tl_y;
                    //bottom-right
                    br_x = _previousFish.Centroid.x + _searchRegionSize;
                    br_y = _previousFish.Centroid.y + _searchRegionSize;
                    br_x = br_x >= image.Width ? image.Width - 1 : br_x;
                    br_y = br_y >= image.Height ? image.Height - 1 : br_y;
                    //update search region
                    _searchRegion.X = tl_x;
                    _searchRegion.Y = tl_y;
                    _searchRegion.Width = br_x - tl_x + 1;
                    _searchRegion.Height = br_y - tl_y + 1;
                    //extract fish within region
                    currentFish = ExtractFish(image, _searchRegion);
                }
                else
                    currentFish = ExtractFish(image, _imageROI);
            }//If We are past initial background frames

            //create/update background and advance frame counter
            if (_frame == 0)
            {
                //Blank images - necessary specifically because of the median filter step
                //which otherwise will leave pixels in _bgSubtracted un-initialized
                ip.ippiSet_8u_C1R(0, Foreground.Image, Foreground.Stride, Foreground.Size);
                ip.ippiSet_8u_C1R(0, _calc.Image, _calc.Stride, _calc.Size);
                ip.ippiSet_8u_C1R(0, _bgSubtracted.Image, _bgSubtracted.Stride, _bgSubtracted.Size);
                _bgModel = new SelectiveUpdateBGModel(image, 1.0f / FramesInBackground);
            }
            else
            {
                //update background every nth frame only
                if (_frame % BGUpdateEvery == 0)
                {
                    if (currentFish == null)
                        _bgModel.UpdateBackground(image);
                    else
                        _bgModel.UpdateBackground(image, currentFish);

                }
            }
            //Update knowledge about previous fish
            //if current fish is trustworthy
            if (currentFish != null && currentFish.Area > FullTrustMinArea)
                _previousFish = currentFish;
            else
                _previousFish = null;

            _frame++;
            return currentFish;
        }

        /// <summary>
        /// Implements a "greater than" threshold like MATLABS
        /// im2bw function
        /// </summary>
        /// <param name="imIn">The image to threshold</param>
        /// <param name="imThresh">The image after thresholding</param>
        /// <param name="region">The ROI in which to perform the operation</param>
        /// <param name="threshold">The threshold to apply</param>
        void Im2Bw(Image8 imIn, Image8 imThresh, IppiROI region)
        {
            IppHelper.IppCheckCall(ip.ippiCompareC_8u_C1R(imIn[region.TopLeft], imIn.Stride, _threshold, imThresh[region.TopLeft], imThresh.Stride, region.Size, IppCmpOp.ippCmpGreater));
        }

        /// <summary>
        /// Extracts a fish (candidate) from an image by performing background subtraction, noise filtering, thresholding and closing to obtain a foreground
        /// followed by marker extraction.
        /// </summary>
        /// <param name="im">The image to extract the fish from</param>
        /// <param name="region">The ROI to search</param>
        /// <returns>The most likely fish blob or null if no suitable candidate was found</returns>
        BlobWithMoments ExtractFish(Image8 im, IppiROI region)
        {
            int nMarkers = 0;


            //Perform background subtraction - CASH BACKGROUND IMAGE POINTER - otherwise we actually do the whole costly
            //32f conversion twice - once for accessing the actual image, and once for accessing the stride...
            var bg = _bgModel.Background;
            IppHelper.IppCheckCall(cv.ippiAbsDiff_8u_C1R(im[region.TopLeft], im.Stride, bg[region.TopLeft], bg.Stride, _calc[region.TopLeft], _calc.Stride, region.Size));
            //remove noise via median filtering
            _mFiltSize.width = region.Width - 2;
            _mFiltSize.height = region.Height - 2;
            IppHelper.IppCheckCall(ip.ippiFilterMedianWeightedCenter3x3_8u_C1R(_calc[region.X + 1, region.Y + 1], _calc.Stride,
                _bgSubtracted[region.X + 1, region.Y + 1], _bgSubtracted.Stride, _mFiltSize, 1));
            //Threshold and close
            Im2Bw(_bgSubtracted, _foreground, region);
            //Do as two step to get information back into _foreground
            _strel3x3.Dilate(_foreground, _calc, region);
            _strel3x3.Erode(_calc, _foreground, region);
            //Label connected components
            IppHelper.IppCheckCall(cv.ippiLabelMarkers_8u_C1IR(_foreground[region.TopLeft], _foreground.Stride, region.Size, 1, 254, IppiNorm.ippiNormInf, &nMarkers, _markerBuffer));
            //loop over returned markers and use ipp to extract blobs
            if (nMarkers > 0)
            {
                if (nMarkers > 254)
                    nMarkers = 254;
                //create or update our intermediate blob storage to store the required number of marker representations
                if (_blobsDetected == null || _blobsDetected.Length < nMarkers)
                    _blobsDetected = new BlobWithMoments[nMarkers];

                for (int i = 1; i <= nMarkers; i++)
                {
                    //label all pixels with the current marker as 255 and others as 0
                    IppHelper.IppCheckCall(ip.ippiCompareC_8u_C1R(_foreground[region.TopLeft], _foreground.Stride, (byte)i, _calc[region.TopLeft], _calc.Stride, region.Size, IppCmpOp.ippCmpEq));
                    //calculate image moments
                    IppHelper.IppCheckCall(ip.ippiMoments64f_8u_C1R(_calc[region.TopLeft], _calc.Stride, region.Size, _momentState));
                    //retrieve moments
                    double m00 = 0;
                    double m10 = 0;
                    double m01 = 0;
                    double m20 = 0;
                    double m02 = 0;
                    double m11 = 0;
                    double m30 = 0;
                    double m03 = 0;
                    double m21 = 0;
                    double m12 = 0;
                    ip.ippiGetSpatialMoment_64f(_momentState, 0, 0, 0, region.TopLeft, &m00);
                    //since our input image is not 0s and 1s but 0s and 255s we have to divide by 255 in order to re-normalize our moments
                    //System.Diagnostics.Debug.Assert(m00 % 255 == 0, "M00 was not a multiple of 255");
                    m00 /= 255;
                    //only retrieve other moments if this is a "fish candidate"
                    if (m00 >= MinArea)
                    {
                        ip.ippiGetSpatialMoment_64f(_momentState, 1, 0, 0, region.TopLeft, &m10);
                        m10 /= 255;
                        ip.ippiGetSpatialMoment_64f(_momentState, 0, 1, 0, region.TopLeft, &m01);
                        m01 /= 255;
                        ip.ippiGetSpatialMoment_64f(_momentState, 2, 0, 0, region.TopLeft, &m20);
                        m20 /= 255;
                        ip.ippiGetSpatialMoment_64f(_momentState, 0, 2, 0, region.TopLeft, &m02);
                        m02 /= 255;
                        ip.ippiGetSpatialMoment_64f(_momentState, 1, 1, 0, region.TopLeft, &m11);
                        m11 /= 255;
                        ip.ippiGetSpatialMoment_64f(_momentState, 3, 0, 0, region.TopLeft, &m30);
                        m30 /= 255;
                        ip.ippiGetSpatialMoment_64f(_momentState, 0, 3, 0, region.TopLeft, &m03);
                        m03 /= 255;
                        ip.ippiGetSpatialMoment_64f(_momentState, 2, 1, 0, region.TopLeft, &m21);
                        m21 /= 255;
                        ip.ippiGetSpatialMoment_64f(_momentState, 1, 2, 0, region.TopLeft, &m12);
                        m12 /= 255;
                        if (_blobsDetected[i - 1] == null)
                            _blobsDetected[i - 1] = new BlobWithMoments((long)m00, (long)m10, (long)m01,
                                (long)m20, (long)m11, (long)m02, (long)m30, (long)m03, (long)m21, (long)m12);
                        else
                            _blobsDetected[i - 1].UpdateBlob((long)m00, (long)m10, (long)m01, (long)m20,
                                (long)m11, (long)m02, (long)m30, (long)m03, (long)m21, (long)m12);
                        //Determine bounding box of the blob. The following seems kinda retarded as Ipp must already
                        //have obtained that information before so maybe there is some way to actually retrieve it??
                        //Do linescans using ipp's sum function starting from the blobs centroid until we hit a line
                        //the sum of which is 0


                        int xStart, xEnd, yStart, yEnd;
                        double sum = 1;
                        IppiPoint centroid = _blobsDetected[i - 1].Centroid;
                        xStart = centroid.x - 1;
                        xEnd = centroid.x + 1;
                        yStart = centroid.y - 1;
                        yEnd = centroid.y + 1;
                        //in the following loops we PRE-increment, whence we stop the loop if we are at one coordinate short of the ends
                        //find xStart
                        _bboxScan.width = 1;
                        _bboxScan.height = region.Height;
                        while (sum > 0 && xStart > (region.X + 4))
                        {

                            xStart -= 5;
                            IppHelper.IppCheckCall(ip.ippiSum_8u_C1R(_calc[xStart, region.Y], _calc.Stride, _bboxScan, &sum));
                        }
                        xStart += 1;//we have a sum of 0, so go back one line towards the centroid
                        //find xEnd
                        sum = 1;
                        while (sum > 0 && xEnd < region.X + region.Width - 5)
                        {
                            xEnd += 5;
                            IppHelper.IppCheckCall(ip.ippiSum_8u_C1R(_calc[xEnd, region.Y], _calc.Stride, _bboxScan, &sum));
                        }
                        xEnd -= 1;//we have sum of 0, so go back one line towards the centroid
                        //find yStart - we can limit our x-search-space as we already have those boundaries
                        _bboxScan.width = xEnd - xStart + 1;
                        _bboxScan.height = 1;
                        sum = 1;
                        while (sum > 0 && yStart > (region.Y + 4))
                        {
                            yStart -= 5;
                            IppHelper.IppCheckCall(ip.ippiSum_8u_C1R(_calc[xStart, yStart], _calc.Stride, _bboxScan, &sum));
                        }
                        yStart += 1;
                        //find yEnd - again limit summation to x-search-space
                        sum = 1;
                        while (sum > 0 && yEnd < region.Y + region.Height - 5)
                        {
                            yEnd += 5;
                            IppHelper.IppCheckCall(ip.ippiSum_8u_C1R(_calc[xStart, yEnd], _calc.Stride, _bboxScan, &sum));
                        }
                        yEnd -= 1;
                        _blobsDetected[i - 1].UpdateBoundingBox(xStart, yStart, xEnd - xStart + 1, yEnd - yStart + 1);
                    }
                    else
                    {
                        if (_blobsDetected[i - 1] == null)
                            _blobsDetected[i - 1] = new BlobWithMoments();
                        else
                            _blobsDetected[i - 1].ResetBlob();
                    }
                }
            }
            else
                return null;
            //decide which of the detected objects is the fish
            //usually we pick the larger blob - however to avoid
            //tracking reflections on the wall, if the largest blob
            //and second largest blob are of comparable size
            //we pick the one which is closer to the center (as reflections
            //are always more eccentric)
            long maxArea = 0;
            long secondMaxArea = 0;
            int maxIndex = -1;
            int secondMaxIndex = -1;
            for (int i = 0; i < nMarkers; i++)
            {
                if (_blobsDetected[i] == null)
                    break;
                //Note down the largest and second-largest blob - but only if those blobs aren't larger than the maxArea and if they eccentricity is at least MinEccentricity
                //this comparison allows that if we find two exactly same-sized blobs to consider both but to not consider any further blobs of this size (which we hopefully never have anyways)
                //Eccentricity and MaxAllowedArea checks removed at this point as they were mainly concieved to not track the laser.
                if (_blobsDetected[i].Area >= maxArea && _blobsDetected[i].Area > secondMaxArea /*&& blobsDetected[i].Area<=MaxAllowedArea && blobsDetected[i].Eccentricity>=MinEccentricity*/)
                {
                    secondMaxArea = maxArea;
                    maxArea = _blobsDetected[i].Area;
                    secondMaxIndex = maxIndex;
                    maxIndex = i;
                }
            }

            if (maxArea < MinArea)
                return null;
            else
            {
                //if our second-largest blob is at least two-thirds the size
                //of the largest blob we also consider distance and swap accordingly
                if ((float)secondMaxArea * 1.5 >= (float)maxArea)
                {
                    double distMax, distSecondMax;
                    distMax = Distance.Euclidian(_blobsDetected[maxIndex].Centroid, DishCenter);
                    distSecondMax = Distance.Euclidian(_blobsDetected[secondMaxIndex].Centroid, DishCenter);
                    if (distMax > distSecondMax)
                        maxIndex = secondMaxIndex;
                }
                return _blobsDetected[maxIndex];
            }
        }

        #endregion

        #region Cleanup

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (IsDisposed)
                return;
            Dispose(true);
            GC.SuppressFinalize(this);
            IsDisposed = true;
        }

        private void Dispose(bool disposing)
        {
            if (_bgModel != null)
            {
                _bgModel.Dispose();
                _bgModel = null;
            }
            if (_calc != null)
            {
                _calc.Dispose();
                _calc = null;
            }
            if (_foreground != null)
            {
                _foreground.Dispose();
                _foreground = null;
            }
            if (_bgSubtracted != null)
            {
                _bgSubtracted.Dispose();
                _bgSubtracted = null;
            }
            if (_markerBuffer != null)
            {
                Marshal.FreeHGlobal((IntPtr)_markerBuffer);
                _markerBuffer = null;
            }
            if (_momentState != null)
            {
                Marshal.FreeHGlobal((IntPtr)_momentState);
                _momentState = null;
            }
            if(_strel3x3 != null)
            {
                _strel3x3.Dispose();
                _strel3x3 = null;
            }
        }

        ~Tracker90mmDish()
        {
            if (!IsDisposed)
            {
                System.Diagnostics.Debug.WriteLine("Tracker not properly disposed");
                Dispose(false);
            }
        }

        #endregion
    }
}
