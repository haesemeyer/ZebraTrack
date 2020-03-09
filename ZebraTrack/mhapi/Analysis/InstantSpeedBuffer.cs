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

namespace MHApi.Analysis
{
    /// <summary>
    /// Wrapper for unmanaged memory containing instant
    /// speed data in floating point format
    /// </summary>
    public unsafe class InstantSpeedBuffer : IDisposable
    {
        /// <summary>
        /// The instant speed buffer
        /// </summary>
        float* _buffer;

        /// <summary>
        /// The number of timepoints to store
        /// </summary>
        int _bufferSize;

        /// <summary>
        /// The stride (for alignment) of the buffer
        /// </summary>
        public int Stride { get; private set; }

        /// <summary>
        /// The ippisize of the whole buffer
        /// </summary>
        public IppiSize Size
        {
            get
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(this.ToString());
                return new IppiSize(_bufferSize, 1);
            }
        }

        /// <summary>
        /// Pointer to the internal buffer
        /// </summary>
        public float* Buffer
        {
            get
            {
                return _buffer;
            }
        }

        /// <summary>
        /// Initializes a new empty centroid buffer
        /// </summary>
        /// <param name="size">The size of the buffer</param>
        public InstantSpeedBuffer(int size)
        {
            _bufferSize = size;
            Stride = _bufferSize * 4;
            _buffer = (float*)Marshal.AllocHGlobal(Stride);
            ip.ippiSet_32f_C1R(0, _buffer, Stride, Size);
        }

        /// <summary>
        /// Creates a deep copy of the source buffer
        /// </summary>
        /// <param name="source"></param>
        public InstantSpeedBuffer(InstantSpeedBuffer source)
        {
            _bufferSize = source._bufferSize;
            Stride = source.Stride;
            _buffer = (float*)Marshal.AllocHGlobal(Stride);
            ip.ippiCopy_32f_C1R(source._buffer, source.Stride, _buffer, Stride, Size);
        }

        /// <summary>
        /// gets/sets an individual coordinate in the buffer
        /// </summary>
        /// <param name="element">The timepoint/element to query</param>
        /// <returns>The speed at the given timepoint</returns>
        public float this[int element]{
            get{
                if (IsDisposed)
                    throw new ObjectDisposedException(this.ToString());
                if (element >= _bufferSize || element < 0)
                    throw new ArgumentOutOfRangeException("Tried to access element outside of buffer boundaries");
                return _buffer[element];
            }
            set
            {
                if (element >= _bufferSize || element < 0)
                    throw new ArgumentOutOfRangeException("Tried to access element outside of buffer boundaries");
                _buffer[element] = value;
            }
        }

        #region IDisposable Members

        public bool IsDisposed {get; private set;}

        public void Dispose() {
            if (IsDisposed)
                return;
            if (_buffer != null)
            {
                Marshal.FreeHGlobal((IntPtr)_buffer);
                _buffer = null;
            }
            IsDisposed = true;
        }

        ~InstantSpeedBuffer() {
            Dispose();
        }
        #endregion
    }
}
