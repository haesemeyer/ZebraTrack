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
using MHApi.DrewsClasses;

namespace MHApi.Imaging {
    public unsafe class Image32F : IDisposable {

        public float* Image;
        public int Width, Height, Stride;
        public IppiSize Size;

        //Construct image with 1 channel and 32-bit floats for each pixel from scratch
        public Image32F(int width, int height) {
            Width = width;
            Height = height;
            Size = new IppiSize(width,height);
            Stride = 4*width;
            Image = (float*)Marshal.AllocHGlobal(Stride * height).ToPointer();
        }

        //Construct 32F image using an 8-bit unsigned image as input
        public Image32F(Image8 im) {
            Width = im.Width;
            Height = im.Height;
            Size = new IppiSize(Width,Height);
            Stride = 4*Width;
            Image = (float*)Marshal.AllocHGlobal(Stride * Height).ToPointer();
            //convert image
            IppHelper.IppCheckCall(ip.ippiConvert_8u32f_C1R(im.Image, im.Stride, Image, Stride, Size));
        }

        public void ReduceTo8U(Image8 im) {
            System.Diagnostics.Debug.Assert(im.Width==Width);
            System.Diagnostics.Debug.Assert(im.Height == Height);
            IppHelper.IppCheckCall(ip.ippiConvert_32f8u_C1R(Image, Stride, im.Image, im.Stride, Size, IppRoundMode.ippRndZero));
        }

        /// <summary>
        /// Pointer to a given pixel in the image buffer
        /// </summary>
        /// <param name="x">The x-coordinate of the pixel</param>
        /// <param name="y">The y-coordinate of the pixel</param>
        /// <returns>Pointer to the pixel in the buffer</returns>
        public float* this[int x, int y]
        {
            get
            {
                if (x >= Width || y >= Height)
                {
                    throw new IndexOutOfRangeException("The indexed point lies outside of the image");
                }
                return (float*)((byte*)Image + x*4 + y * Stride);
            }
        }

        /// <summary>
        /// Pointer to a given pixel in the image buffer
        /// </summary>
        /// <param name="point">The point referencing the pixel</param>
        /// <returns>Pointer to the pixel in the buffer</returns>
        public float* this[IppiPoint point]
        {
            get
            {
                if (point.x >= Width || point.y >= Height)
                {
                    throw new IndexOutOfRangeException("The indexed point lies outside of the image");
                }
                return (float*)((byte*)Image + point.x*4 + point.y * Stride);
            }
        }

        #region IDisposable Members
        bool isDisposed;
        public void Dispose() {
            if (isDisposed) return;
            Marshal.FreeHGlobal((IntPtr)Image);
            isDisposed = true;
        }

        ~Image32F() {
            Dispose();
        }
        #endregion
    }
}
