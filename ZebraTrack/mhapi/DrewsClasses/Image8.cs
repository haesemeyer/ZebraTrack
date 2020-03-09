/*
Copyright 2011-16 Drew Robson, Martin Haesemeyer
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

namespace MHApi.DrewsClasses {

    /// <summary>
    /// Class to encapsulate an 8bit image in raw memory
    /// </summary>
    public unsafe class Image8 : IDisposable {

        private float[] _scalingBuffer;
        
        /// <summary>
        /// Pointer to the image in memory
        /// </summary>
        public byte* Image;
        /// <summary>
        /// Image Width
        /// </summary>
        public int Width
        {
            get
            {
                return Size.width;
            }
        }

        /// <summary>
        /// Image height
        /// </summary>
        public int Height
        {
            get
            {
                return Size.height;
            }
        }

        /// <summary>
        /// Image stride (memory layout)
        /// </summary>
        public int Stride;

        /// <summary>
        /// Pointer to a given pixel in the image buffer
        /// </summary>
        /// <param name="x">The x-coordinate of the pixel</param>
        /// <param name="y">The y-coordinate of the pixel</param>
        /// <returns>Pointer to the pixel in the buffer</returns>
        public byte* this[int x, int y]
        {
            get
            {
                if (x >= Width || y >= Height)
                {
                    throw new IndexOutOfRangeException("The indexed point lies outside of the image");
                }
                return Image+x + y * Stride;
            }
        }

        /// <summary>
        /// Pointer to a given pixel in the image buffer
        /// </summary>
        /// <param name="point">The point referencing the pixel</param>
        /// <returns>Pointer to the pixel in the buffer</returns>
        public byte* this[IppiPoint point]
        {
            get
            {
                if (point.x >= Width || point.y >= Height)
                {
                    throw new IndexOutOfRangeException("The indexed point lies outside of the image");
                }
                return Image + point.x + point.y * Stride;
            }
        }

        /// <summary>
        /// Imagesize
        /// </summary>
        public IppiSize Size;

        public Image8(int width, int height) {
            Stride = (int)(4 * Math.Ceiling(width / 4.0));
            Size = new IppiSize(width, height);
            Image = (byte*)Marshal.AllocHGlobal(Stride * height).ToPointer();
        }

        public Image8(IppiSize imageSize) : this(imageSize.width, imageSize.height) { }

        public void FromImage16(Image16 im, float cMax) {
            if (im.Width != Width || im.Height != Height)
                throw new ArgumentException("Width and Height must match");
            if (_scalingBuffer == null || _scalingBuffer.Length < im.Width * im.Height)
                _scalingBuffer = new float[im.Width * im.Height];
            fixed (float* pScalingBuffer = _scalingBuffer) {
                ip.ippiConvert_16u32f_C1R(im.Image, im.Stride, pScalingBuffer, 4 * im.Width, im.Size);
                ip.ippiScale_32f8u_C1R(pScalingBuffer, 4 * im.Width, Image, Stride, Size, 0, cMax);
            }
        }

        #region IDisposable Members

        public bool IsDisposed { get; private set; }

        public void Dispose() {
            if (IsDisposed) return;
            GC.SuppressFinalize(this);
            if(Image != null)
                Marshal.FreeHGlobal((IntPtr)Image);
            IsDisposed = true;
        }

        ~Image8() {
            Dispose();
        }

        #endregion
    }
}
