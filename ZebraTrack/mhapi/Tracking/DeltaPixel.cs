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
    public unsafe class DeltaPixel : IDisposable
    {

        #region Members

        /// <summary>
        /// The previous frame
        /// </summary>
        Image8 _imPrevious;

        /// <summary>
        /// The absolute difference btw. the current
        /// and the previous frame
        /// </summary>
        Image8 _imDelta;

        /// <summary>
        /// Thresholded version of _imDelta
        /// </summary>
        Image8 _imThresh;

        /// <summary>
        /// Threshold to apply to identify
        /// "true" delta pixels but suppress camera noise
        /// </summary>
        byte _threshold;

        /// <summary>
        /// True for the first frame supplied
        /// to the delta-pixel class since we don't want
        /// to report any positives for the first frame
        /// </summary>
        bool _isFirst;

        #endregion

        #region Properties

        /// <summary>
        /// Threshold to apply to identify
        /// "true" delta pixels but suppress camera noise
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

        #endregion

        #region Construction

        /// <summary>
        /// Constructs a new DeltaPixel class
        /// </summary>
        /// <param name="threshold">The threshold to use to separate imaging noise from signal</param>
        /// <param name="imageSize">The size of the images that will be supplied</param>
        public DeltaPixel(byte threshold, IppiSize imageSize)
        {
            _threshold = threshold;
            _imPrevious = new Image8(imageSize);
            _imDelta = new Image8(imageSize);
            _imThresh = new Image8(imageSize);
            _isFirst = true;
            //blank images
            ip.ippiSet_8u_C1R(0, _imPrevious.Image, _imPrevious.Stride, _imPrevious.Size);
            ip.ippiSet_8u_C1R(0, _imDelta.Image, _imDelta.Stride, _imDelta.Size);
            ip.ippiSet_8u_C1R(0, _imThresh.Image, _imThresh.Stride, _imThresh.Size);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Differences the current image with the previous one and computes the number of pixels
        /// above the set difference threshold
        /// </summary>
        /// <param name="imCurrent">The current frame to difference with the previous</param>
        /// <returns>Number of pixels above threshold in the delta frame</returns>
        public int ComputeNumDeltaPixels(Image8 imCurrent)
        {
            double nPixels = 0;
            if (!_isFirst)
            {
                //form difference image
                cv.ippiAbsDiff_8u_C1R(imCurrent.Image, imCurrent.Stride, _imPrevious.Image, _imPrevious.Stride, _imDelta.Image, _imDelta.Stride, _imDelta.Size);
                //threshold image
                BWImageProcessor.Im2Bw(_imDelta, _imThresh, new IppiROI(0, 0, imCurrent.Width, imCurrent.Height), _threshold);
                //count pixels
                ip.ippiSum_8u_C1R(_imThresh.Image, _imThresh.Stride, _imThresh.Size, &nPixels);
            }
            else
                _isFirst = false;
            //copy current image to previous image buffer
            ip.ippiCopy_8u_C1R(imCurrent.Image, imCurrent.Stride, _imPrevious.Image, _imPrevious.Stride, imCurrent.Size);
            //return pixel count - divide sum by 255 since threshold sets al values to 255
            return (int)(nPixels / 255);
        }

        /// <summary>
        /// Differences the current image with the previous one and computes the number of pixels
        /// above the set difference threshold in the selected ROI
        /// </summary>
        /// <param name="imCurrent">The current frame to difference with the previous</param>
        /// <param name="roi">The ROI in which to perform the compuation</param>
        /// <returns>Number of pixels above threshold in the delta frame</returns>
        public int ComputeNumDeltaPixels(Image8 imCurrent, IppiROI roi)
        {
            double nPixels = 0;
            if (!_isFirst)
            {
                //form difference image
                cv.ippiAbsDiff_8u_C1R(imCurrent[roi.TopLeft], imCurrent.Stride, _imPrevious[roi.TopLeft], _imPrevious.Stride, _imDelta[roi.TopLeft], _imDelta.Stride, roi.Size);
                //threshold image
                BWImageProcessor.Im2Bw(_imDelta, _imThresh, roi, _threshold);
                //count pixels
                ip.ippiSum_8u_C1R(_imThresh[roi.TopLeft], _imThresh.Stride, roi.Size, &nPixels);
            }
            else
                _isFirst = false;
            //copy current image to previous image buffer
            ip.ippiCopy_8u_C1R(imCurrent[roi.TopLeft], imCurrent.Stride, _imPrevious[roi.TopLeft], _imPrevious.Stride, roi.Size);
            //return pixel count - divide sum by 255 since threshold sets al values to 255
            return (int)nPixels / 255;
        }

        #endregion

        #region Cleanup

        public bool IsDisposed { get; private set; }

        protected void Dispose(bool IsDisposing)
        {
            if (_imPrevious != null)
            {
                _imPrevious.Dispose();
                _imPrevious = null;
            }
            if (_imDelta != null)
            {
                _imDelta.Dispose();
                _imDelta = null;
            }
            if (_imThresh != null)
            {
                _imThresh.Dispose();
                _imThresh = null;
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
            }
        }

        ~DeltaPixel()
        {
            if (!IsDisposed)
                System.Diagnostics.Debug.WriteLine("Forgot to dispose delta pixel class!");
            Dispose(false);
        }

        #endregion

    }
}
