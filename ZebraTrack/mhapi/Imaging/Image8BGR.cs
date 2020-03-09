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

namespace MHApi.Imaging
{
    /// <summary>
    /// ipp compatible raw memory representation of an 8bit BGR image
    /// </summary>
    public unsafe class Image8BGR : IDisposable
    {
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
        /// The BGR components of a given pixel in the image
        /// </summary>
        /// <param name="x">The x-coordinate of the pixel</param>
        /// <param name="y">The y-coordinate of the pixel</param>
        /// <returns>Pointer to the pixel in the buffer</returns>
        public byte[] this[int x, int y]
        {
            get
            {
                if (x >= Width || y >= Height)
                {
                    throw new IndexOutOfRangeException("The indexed point lies outside of the image");
                }
                byte[] retval = new byte[3];
                retval[0] = *(Image + x * 3 + y * Stride);
                retval[1] = *(Image + x * 3 + y * Stride + 1);
                retval[2] = *(Image + x * 3 + y * Stride + 2);
                return retval;
            }
            set
            {
                if (value.Length != 3)
                    throw new ArgumentException("A 3 element byte array for the BGR components is required");
                *(Image + x * 3 + y * Stride) = value[0];
                *(Image + x * 3 + y * Stride + 1) = value[1];
                *(Image + x * 3 + y * Stride + 2) = value[2];
            }
        }

        /// <summary>
        /// The BGR components of a given pixel in the image
        /// </summary>
        /// <param name="point">The point referencing the pixel</param>
        /// <returns>Pointer to the pixel in the buffer</returns>
        public byte[] this[IppiPoint point]
        {
            get
            {
                return this[point.x,point.y];
            }
            set
            {
                this[point.x, point.y] = value;
            }
        }

        /// <summary>
        /// Imagesize
        /// </summary>
        public IppiSize Size;

        public Image8BGR(int width, int height) {
            Stride = (int)(4 * Math.Ceiling(width*3 / 4.0));
            Size = new IppiSize(width, height);
            Image = (byte*)Marshal.AllocHGlobal(Stride * height).ToPointer();
        }

        #region IDisposable

        public bool IsDisposed { get; private set; }
        public void Dispose() {
            if (IsDisposed) return;
            Marshal.FreeHGlobal((IntPtr)Image);
            IsDisposed = true;
        }

        ~Image8BGR() {
            Dispose();
        }

        #endregion
    }
}
