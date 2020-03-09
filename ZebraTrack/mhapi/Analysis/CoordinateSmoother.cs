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

using MHApi.Imaging;

namespace MHApi.Analysis
{
    /// <summary>
    /// Offers a function to smooth coordinate traces
    /// and contains all necessary internal buffers
    /// </summary>
    public unsafe class CoordinateSmoother : IDisposable
    {
        /// <summary>
        /// Internal buffer for data processing
        /// including border necessary for filtering
        /// </summary>
        CentroidBuffer _calc1;

        /// <summary>
        /// Internal buffer for data processing
        /// including border necessary for filtering
        /// </summary>
        CentroidBuffer _calc2;

        /// <summary>
        /// The filter kernel
        /// </summary>
        float* _kernel;

        /// <summary>
        /// The size of the allocated kernel
        /// </summary>
        int _kernelSize;

        public CoordinateSmoother(){}

        /// <summary>
        /// Pre-allocates the internal buffer to the requested size
        /// </summary>
        /// <param name="nCoordinates">The number of coordinates for which space should be pre-allocated in the internal buffers</param>
        public CoordinateSmoother(int nCoordinates, int windowSize)
        {
            _calc1 = new CentroidBuffer(nCoordinates + (int)Math.Ceiling(windowSize / 2.0) * 2);
            _calc2 = new CentroidBuffer(_calc1.Size.width);
            _kernelSize = windowSize;
            //preinit kernel
            _kernel = (float*)Marshal.AllocHGlobal(_kernelSize * 4);
            int i = 0;
            while (i < _kernelSize)
                _kernel[i++] = 1 / (float)_kernelSize;
        }

        /// <summary>
        /// Implements in place coordinate smoothing analogous to using filtfilt in matlab with a step function kernel
        /// (moving average with no peak displacement)
        /// </summary>
        /// <param name="srcDest">The source and destination coordinate buffer</param>
        /// <param name="windowSize">The windowsize for averaging</param>
        public void SmoothenTrack(CentroidBuffer srcDest, int windowSize)
        {
            SmoothenTrack(srcDest, srcDest, windowSize);
        }

        /// <summary>
        /// Implements coordinate smoothing analogous to using filtfilt in matlab with a step function kernel
        /// (moving average with no peak displacement)
        /// </summary>
        /// <param name="src">The source track to smoothen</param>
        /// <param name="dst">The destination buffer for the smoothened track</param>
        /// <param name="windowSize">The windowsize for averaging</param>
        public void SmoothenTrack(CentroidBuffer src, CentroidBuffer dst, int windowSize)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(this.ToString());
            if (src.Size.width != dst.Size.width)
                throw new ArgumentException("Source and destination buffers need to have the same size!");
            //For the internal buffers we require a size that fits both the coordinate buffer we
            //intend to filter as well as the border pixels required for filtering
            int borderSize = (int)Math.Ceiling(windowSize / 2.0);
            int reqSize = src.Size.width + borderSize * 2;
            //Adjust internal buffers if necessary
            if (_calc1 == null)
            {
                _calc1 = new CentroidBuffer(reqSize);
                _calc2 = new CentroidBuffer(reqSize);
            }
            else if (_calc1.Size.width != reqSize)
            {
                _calc1.Dispose();
                _calc2.Dispose();
                _calc1 = new CentroidBuffer(reqSize);
                _calc2 = new CentroidBuffer(reqSize);
            }
            if (_kernel == null)
            {
                _kernelSize = windowSize;
                _kernel = (float*)Marshal.AllocHGlobal(_kernelSize * 4);
                int i = 0;
                while (i < _kernelSize)
                    _kernel[i++] = 1 / (float)_kernelSize;
            }
            else if (_kernelSize != windowSize)
            {
                Marshal.FreeHGlobal((IntPtr)_kernel);
                _kernelSize = windowSize;
                _kernel = (float*)Marshal.AllocHGlobal(_kernelSize * 4);
                int i = 0;
                while (i < _kernelSize)
                    _kernel[i++] = 1 / (float)_kernelSize;
            }
            //filter parameters
            IppiSize regionSize = new IppiSize(src.Size.width, 1);
            IppiPoint anchor = new IppiPoint(borderSize, 0);
            IppiSize kernelSize = new IppiSize(_kernelSize, 1);
            float* calc1XStart = (float*)((byte*)_calc1.Buffer + borderSize * 4);
            float* calc1YStart = (float*)((byte*)_calc1.Buffer + borderSize * 4 + _calc1.Stride);
            float* calc2XStart = (float*)((byte*)_calc2.Buffer + borderSize * 4);
            float* calc2YStart = (float*)((byte*)_calc2.Buffer + borderSize * 4 + _calc2.Stride);
            //Copy src buffer adding borders
            IppHelper.IppCheckCall(ip.ippiCopyConstBorder_32f_C1R(src.Buffer, src.Stride, src.Size, _calc1.Buffer, _calc1.Stride, _calc1.Size, 0, borderSize, 0));
            //Fill calc2 to have borders ready after filtering
            IppHelper.IppCheckCall(ip.ippiSet_32f_C1R(0, _calc2.Buffer, _calc2.Stride, _calc2.Size));
            //filter x-coordinates with our kernel
            IppHelper.IppCheckCall(ip.ippiFilter_32f_C1R(calc1XStart, _calc1.Stride, calc2XStart, _calc2.Stride, regionSize, _kernel, kernelSize, anchor));
            //filter y-coordinates with our kernel
            IppHelper.IppCheckCall(ip.ippiFilter_32f_C1R(calc1YStart, _calc1.Stride, calc2YStart, _calc2.Stride, regionSize, _kernel, kernelSize, anchor));

            //invert buffer - mirror on vertical axis
            IppHelper.IppCheckCall(ip.ippiMirror_32f_C1R(_calc2.Buffer, _calc2.Stride, _calc1.Buffer, _calc1.Stride, _calc2.Size, IppiAxis.ippAxsVertical));
            //filter x-coordinates with our kernel - now on inverted buffer
            IppHelper.IppCheckCall(ip.ippiFilter_32f_C1R(calc1XStart, _calc1.Stride, calc2XStart, _calc2.Stride, regionSize, _kernel, kernelSize, anchor));
            //filter y-coordinates with our kernel - now on inverted buffer
            IppHelper.IppCheckCall(ip.ippiFilter_32f_C1R(calc1YStart, _calc1.Stride, calc2YStart, _calc2.Stride, regionSize, _kernel, kernelSize, anchor));
            //flip buffer back
            IppHelper.IppCheckCall(ip.ippiMirror_32f_C1R(_calc2.Buffer, _calc2.Stride, _calc1.Buffer, _calc1.Stride, _calc2.Size, IppiAxis.ippAxsVertical));
            //copy to dest
            IppHelper.IppCheckCall(ip.ippiCopy_32f_C1R(calc1XStart, _calc1.Stride, dst.Buffer, dst.Stride, dst.Size));
        }
        
        #region IDisposable Members

        public bool IsDisposed {get; private set;}

        public void Dispose() {
            if (IsDisposed)
                return;
            if (_calc1 != null)
            {
                _calc1.Dispose();
                _calc1 = null;
            }
            if (_calc2 != null)
            {
                _calc2.Dispose();
                _calc2 = null;
            }
            if (_kernel != null)
            {
                Marshal.FreeHGlobal((IntPtr)_kernel);
                _kernel = null;
            }
            IsDisposed = true;
        }

        ~CoordinateSmoother() {
            Dispose();
        }
        #endregion
    }
}
