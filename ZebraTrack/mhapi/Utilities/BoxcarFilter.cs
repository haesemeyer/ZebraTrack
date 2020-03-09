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

namespace MHApi.Utilities
{
    /// <summary>
    /// Implementation of a realtime boxcar filter
    /// </summary>
    public unsafe class BoxcarFilter : IDisposable
    {
        #region Members

        /// <summary>
        /// The size of our boxcar filter used for
        /// smoothing the shifts
        /// </summary>
        readonly int _bcFilterSize;

        /// <summary>
        /// The taps of our boxcar filter
        /// </summary>
        float* _bcTaps;

        /// <summary>
        /// The delay lines of our boxcar filter
        /// </summary>
        float* _bcDelay;

        /// <summary>
        /// The state of our boxcar filter
        /// </summary>
        IppsFIRState_32f* _filterState;

        /// <summary>
        /// The filter state memory
        /// </summary>
        byte* _filterStateMem;

        #endregion

        /// <summary>
        /// Creates a new BoxcarFilter object
        /// </summary>
        /// <param name="filterSize">The size of the filter in time-steps</param>
        public BoxcarFilter(int filterSize)
        {
            if (filterSize < 1)
                throw new ArgumentOutOfRangeException(nameof(filterSize), "Has to be 1 or larger");
            _bcFilterSize = filterSize;
            //create memory for and initialize taps and delay lines
            _bcTaps = (float*)Marshal.AllocHGlobal(_bcFilterSize * 4);
            int i = 0;
            while (i < _bcFilterSize)
                _bcTaps[i++] = 1 / (float)_bcFilterSize;
            _bcDelay = (float*)Marshal.AllocHGlobal(_bcFilterSize * 4);
            sp.ippsZero_32f(_bcDelay, _bcFilterSize);
            //get memory requirements and allocate state structures
            int size = 0;
            sp.ippsFIRGetStateSize_32f(_bcFilterSize, &size);
            _filterStateMem = (byte*)Marshal.AllocHGlobal(size);
            //initialize state structure
            fixed (IppsFIRState_32f** ppState = &_filterState)
            {
                sp.ippsFIRInit_32f(ppState, _bcTaps, _bcFilterSize, _bcDelay, _filterStateMem);
            }
        }

        /// <summary>
        /// The size/length of the boxcar filter
        /// </summary>
        public int FilterSize
        {
            get
            {
                return _bcFilterSize;
            }
        }

        #region Methods

        /// <summary>
        /// Computes the next filtered value
        /// </summary>
        /// <param name="NewValue">The new value to add to the filter</param>
        /// <returns>The newly filtered value</returns>
        public float Next(float NewValue)
        {
            float smoothened = 0;
            sp.ippsFIR_32f(&NewValue, &smoothened, 1, _filterState);
            return smoothened;
        }

        /// <summary>
        /// Resets the delay line contents back to 0
        /// </summary>
        public void Reset()
        {
            sp.ippsZero_32f(_bcDelay, _bcFilterSize);
            //re-initialize state structure
            fixed (IppsFIRState_32f** ppState = &_filterState)
            {
                sp.ippsFIRInit_32f(ppState, _bcTaps, _bcFilterSize, _bcDelay, _filterStateMem);
            }
        }

        #endregion

        #region IDisposable Support
        public bool IsDisposed { get; private set; } = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    
                }

                if (_bcTaps != null)
                {
                    Marshal.FreeHGlobal((IntPtr)_bcTaps);
                    _bcTaps = null;
                }
                if (_bcDelay != null)
                {
                    Marshal.FreeHGlobal((IntPtr)_bcDelay);
                    _bcDelay = null;
                }
                if (_filterStateMem != null)
                {
                    Marshal.FreeHGlobal((IntPtr)_filterStateMem);
                    _filterStateMem = null;
                }

                IsDisposed = true;
            }
        }


        ~BoxcarFilter()
        {
            Dispose(false);
        }

        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
