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
    /// Represents a 16 bit image
    /// </summary>
    public unsafe class Image16 : IDisposable
    {
        public ushort* Image;

        public readonly int Width, Height, Stride;
        public readonly IppiSize Size;

        /// <summary>
        /// Indicates how our memory was acquired - MemoryAllocation
        /// or via pinned GC object
        /// </summary>
        private bool _isFromPinned;

        /// <summary>
        /// Buffer handle in case of pinned object
        /// </summary>
        private GCHandle _hImage;

        //Construct image with 1 channel and 16bit for each pixel from scratch
        public Image16(int width, int height) {
            Width = width;
            Height = height;
            Size = new IppiSize(width,height);
            Stride = (int)(4 * Math.Ceiling(width * 2 / 4.0));
            Image = (ushort*)Marshal.AllocHGlobal(Stride * height).ToPointer();
            _isFromPinned = false;
        }

        /// <summary>
        /// Allocate a new Image16 object directly from a pinned
        /// garbage collected memory buffer
        /// </summary>
        /// <param name="imageBuffer">2D image buffer, 4byte aligned</param>
        public Image16(ushort[,] imageBuffer)
        {
            Height = imageBuffer.GetLength(0);
            Width = imageBuffer.GetLength(1);
            Stride = (int)(4 * Math.Ceiling(2.0 * Width / 4.0));
            if (Stride != 2 * Width)
                throw new ArgumentException("Can only accept 4-byte aligned 2D image buffers!");
            Size = new IppiSize(Width, Height);
            _hImage = GCHandle.Alloc(imageBuffer, GCHandleType.Pinned);
            Image = (ushort*)_hImage.AddrOfPinnedObject().ToPointer();
            _isFromPinned = true;
        }

        public static explicit operator Image16(ushort[,] imageBuffer)
        {
            return new Image16(imageBuffer);
        }

        //Construct 16-bit image using an 8-bit unsigned image as input
        public Image16(Image8 im) {
            Width = im.Width;
            Height = im.Height;
            Size = new IppiSize(Width,Height);
            Stride = (int)(4 * Math.Ceiling(Width * 2 / 4.0));
            Image = (ushort*)Marshal.AllocHGlobal(Stride * Height).ToPointer();
            //scale, convert and copy image
            IppHelper.IppCheckCall(ip.ippiScale_8u16u_C1R(im.Image, im.Stride, Image, Stride, Size));
            _isFromPinned = false;
        }

        /// <summary>
        /// Scale image to 8bit representation avoiding clipping
        /// </summary>
        /// <param name="im"></param>
        public void ReduceTo8U(Image8 im) {
            System.Diagnostics.Debug.Assert(im.Width==Width);
            System.Diagnostics.Debug.Assert(im.Height == Height);
            IppHelper.IppCheckCall(ip.ippiScale_16u8u_C1R(Image, Stride, im.Image, im.Stride, Size, IppHintAlgorithm.ippAlgHintFast));
        }

        /// <summary>
        /// Pointer to a given pixel in the image buffer
        /// </summary>
        /// <param name="x">The x-coordinate of the pixel</param>
        /// <param name="y">The y-coordinate of the pixel</param>
        /// <returns>Pointer to the pixel in the buffer</returns>
        public ushort* this[int x, int y]
        {
            get
            {
                if (x >= Width || y >= Height)
                {
                    throw new IndexOutOfRangeException("The indexed point lies outside of the image");
                }
                return (ushort*)((byte*)Image + x*2 + y * Stride);
            }
        }

        /// <summary>
        /// Pointer to a given pixel in the image buffer
        /// </summary>
        /// <param name="point">The point referencing the pixel</param>
        /// <returns>Pointer to the pixel in the buffer</returns>
        public ushort* this[IppiPoint point]
        {
            get
            {
                if (point.x >= Width || point.y >= Height)
                {
                    throw new IndexOutOfRangeException("The indexed point lies outside of the image");
                }
                return (ushort*)((byte*)Image + point.x*2 + point.y * Stride);
            }
        }
        
        #region IDisposable Members
        public bool IsDisposed { get; set; }

        public void Dispose() {
            if (IsDisposed) return;
            GC.SuppressFinalize(this);
            if (!_isFromPinned)
                Marshal.FreeHGlobal((IntPtr)Image);
            else
                _hImage.Free();
            IsDisposed = true;
        }

        ~Image16() {
            Dispose();
        }
        #endregion
    }
}
