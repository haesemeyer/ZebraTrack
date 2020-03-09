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

using MHApi.DrewsClasses;

namespace MHApi.Imaging
{
    /// <summary>
    /// Provides a circular mask image with a value 1 center
    /// and a black (0) surround
    /// </summary>
    public unsafe class CircularMask : IDisposable
    {
        #region Fields

        /// <summary>
        /// The actual representation of the mask
        /// </summary>
        Image8 _mask;

        /// <summary>
        /// Indicates if the current content of _mask
        /// represents the mask parameters
        /// </summary>
        bool _maskValid;

        /// <summary>
        /// The radius of the mask in pixel
        /// </summary>
        int _radius;

        /// <summary>
        /// The center of the mask
        /// </summary>
        IppiPoint _center;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new circular mask object with a centered circle
        /// </summary>
        /// <param name="imageSize">The size of the mask image</param>
        /// <param name="radius">The radius of the circle</param>
        public CircularMask(IppiSize imageSize, int radius) : this(imageSize, new IppiPoint((int)imageSize.width / 2, (int)imageSize.height / 2), radius) { }

        /// <summary>
        /// Creates a new circular mask object with an arbitrarily centered circle
        /// </summary>
        /// <param name="imageSize">The size of the mask image</param>
        /// <param name="center">The center of the circle</param>
        /// <param name="radius">The radius of the circle</param>
        public CircularMask(IppiSize imageSize, IppiPoint center, int radius)
        {
            _mask = new Image8(imageSize.width, imageSize.height);
            Center = center;
            Radius = radius;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The radius of the mask in pixel
        /// </summary>
        public int Radius
        {
            get
            {
                return _radius;
            }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException("Radius has to be at least 1 pixel");
                if (value != _radius)
                    _maskValid = false;
                _radius = value;
            }
        }

        /// <summary>
        /// The center coordinates of the mask
        /// </summary>
        public IppiPoint Center
        {
            get
            {
                return _center;
            }
            set
            {
                if (value.x < 0 || value.y < 0 || value.x >= ImageSize.width || value.y >= ImageSize.height)
                    throw new ArgumentOutOfRangeException("The center of the circle has to be within the image");
                if (value.x != _center.x || value.y != _center.y)
                    _maskValid = false;
                _center = value;
            }
        }

        /// <summary>
        /// The size of the mask image
        /// </summary>
        public IppiSize ImageSize
        {
            get
            {
                return _mask.Size;
            }
        }

        public Image8 Mask
        {
            get
            {
                if (!_maskValid)
                    GenerateMask();
                return _mask;
            }
        }


        #endregion

        #region Methods

        void GenerateMask()
        {
            //To generate the mask, we use a euclidian distance transform to the center pixel
            //followed by a thresholding operation
            //To save resources we only do this within the outer square of the circle since
            //all pixels outside of that square will be black anyway
            double xStart, yStart;
            //compute outer square
            xStart = _center.x - _radius;
            yStart = _center.y - _radius;
            if (xStart < 0)
                xStart = 0;
            if (yStart < 0)
                yStart = 0;
            IppiROI outerSquare = new IppiROI((int)xStart, (int)yStart, 2*_radius, 2*_radius);
            if (outerSquare.X + outerSquare.Width >= _mask.Width)
            {
                outerSquare.Width = _mask.Width - outerSquare.X - 1;
            }
            if (outerSquare.Y + outerSquare.Height >= _mask.Height)
            {
                outerSquare.Height = _mask.Height - outerSquare.Y - 1;
            }

            //The distance transform will calculate for all non-0 pixels the distance to the closest
            //0 pixel - hence we set all pixels in our mask temporarily to 1 and the circles center to 0
            IppHelper.IppCheckCall(ip.ippiSet_8u_C1R(1, _mask.Image, _mask.Stride, _mask.Size));
            IppHelper.IppCheckCall(ip.ippiSet_8u_C1R(0, _mask[Center], _mask.Stride, new IppiSize(1,1)));
            //initialize buffers and calculate distance transform within outer square
            //we initialize the distance transform image with a value > radius so that all the
            //untouched pixel outside of the outer square will later be set to 0
            int bufferSize;
            IppHelper.IppCheckCall(cv.ippiTrueDistanceTransformGetBufferSize_8u32f_C1R(outerSquare.Size, &bufferSize));
            byte* dTransBuffer = (byte*)Marshal.AllocHGlobal(bufferSize);
            Image32F distImage = new Image32F(_mask);
            IppHelper.IppCheckCall(ip.ippiSet_32f_C1R(Radius + 1, distImage.Image, distImage.Stride, distImage.Size));
            IppHelper.IppCheckCall(cv.ippiTrueDistanceTransform_8u32f_C1R(_mask[outerSquare.TopLeft], _mask.Stride, distImage[outerSquare.TopLeft], distImage.Stride, outerSquare.Size, dTransBuffer));
            
            //set all pixels whose value after the distance transform is >radius to 0
            IppHelper.IppCheckCall(ip.ippiThreshold_GTVal_32f_C1IR(distImage.Image, distImage.Stride, distImage.Size, Radius, 0));
            //now set all pixels whose value is >0 to 1
            IppHelper.IppCheckCall(ip.ippiThreshold_GTVal_32f_C1IR(distImage.Image, distImage.Stride, distImage.Size, 0, 1));
            //convert dist image, copying it into the mask
            IppHelper.IppCheckCall(ip.ippiConvert_32f8u_C1R(distImage.Image, distImage.Stride, _mask.Image, _mask.Stride, _mask.Size, IppRoundMode.ippRndNear));
            //Clean up
            Marshal.FreeHGlobal((IntPtr)dTransBuffer);
            distImage.Dispose();

            _maskValid = true;

            /*
            //We want to generate the mask with the least amount of pixel-iterations possible
            //based on the radius we can define to rectangular regions for which we can
            //bulk-set the pixels
            //The inner square of the circle is definitely white (centered around center, side-length = sqrt(r*r/2)
            //The outer square is definitely black (centerered around center, side-length=radius)
            double sideLength,xStart,yStart;

            //compute inner square
            sideLength = Math.Sqrt(_radius * _radius / 2);
            xStart = _center.x - sideLength / 2;
            yStart = _center.y - sideLength / 2;
            if (xStart < 0)
                xStart = 0;
            if (yStart < 0)
                yStart = 0;
            IppiROI innerSquare = new IppiROI((int)xStart, (int)yStart, (int)sideLength, (int)sideLength);
            if (innerSquare.X + innerSquare.Width >= _mask.Width)
            {
                innerSquare.Width = _mask.Width - innerSquare.X - 1;
            }
            if (innerSquare.Y + innerSquare.Height >= _mask.Height)
            {
                innerSquare.Height = _mask.Height - innerSquare.Y - 1;
            }
            
            //compute outer square
            xStart = _center.x - _radius / 2.0;
            yStart = _center.y - _radius / 2.0;
            if (xStart < 0)
                xStart = 0;
            if (yStart < 0)
                yStart = 0;
            IppiROI outerSquare = new IppiROI((int)xStart, (int)yStart, _radius, _radius);
            if (outerSquare.X + outerSquare.Width >= _mask.Width)
            {
                outerSquare.Width = _mask.Width - outerSquare.X - 1;
            }
            if (outerSquare.Y + outerSquare.Height >= _mask.Height)
            {
                outerSquare.Height = _mask.Height - outerSquare.Y - 1;
            }

            //set whole image to black and inner square to 1
            IppHelper.IppCheckCall(ip.ippiSet_8u_C1R(0, _mask.Image, _mask.Stride, _mask.Size));
            IppHelper.IppCheckCall(ip.ippiSet_8u_C1R(1, _mask[innerSquare.TopLeft], _mask.Stride, innerSquare.Size));

            //loop over all the pixels that are in between the boundaries of the outer
            //and inner square, measure their distance to the center and
            //p = d>r?0:1;
            */
        }

        /// <summary>
        /// Multiplies the mask with the image effectively removing
        /// parts outside the mask
        /// </summary>
        /// <param name="image">The image to mask</param>
        public void MaskImage(Image8 image)
        {
            if (!_maskValid)
                GenerateMask();
            IppHelper.IppCheckCall(ip.ippiMul_8u_C1IRSfs(_mask.Image, _mask.Stride, image.Image, image.Stride, image.Size, 0));
        }

        #endregion

        #region IDisposable

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (_mask != null)
            {
                _mask.Dispose();
                _mask = null;
            }
            IsDisposed = true;
        }

        #endregion
    }
}
