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
using System.Threading;

using ipp;

using MHApi.DrewsClasses;

namespace MHApi.Threading
{
    /// <summary>
    /// A fast Producer Consumer implementation for images
    /// using a fixed ring-buffer
    /// </summary>
    public unsafe class PrCoImageRingBuffer : IDisposable
    {

        #region Fields

        /// <summary>
        /// Pointer to the image buffers used by the ring-buffer
        /// </summary>
        private readonly byte*[] _buffers;

        /// <summary>
        /// The threadlock for buffer access
        /// </summary>
        private readonly object _accessLock = new object();

        /// <summary>
        /// Index of the buffer where the last
        /// image was produced
        /// </summary>
        private int _lastProduce;

        /// <summary>
        /// Index of the buffer from which the
        /// last image was consumed
        /// </summary>
        private int _lastConsume;

        /// <summary>
        /// The number of images the buffer can store
        /// </summary>
        private readonly int _nImages;

        /// <summary>
        /// The width of the images the buffer will contain
        /// </summary>
        private readonly int _imageWidth;

        /// <summary>
        /// The height of the images the buffer will contain
        /// </summary>
        private readonly int _imageHeight;

        /// <summary>
        /// The stride of the images the buffer will contain
        /// </summary>
        private readonly int _imageStride;

        /// <summary>
        /// The indices of the images currently in each
        /// place of the buffer
        /// </summary>
        private int[] _imageIndices;

        /// <summary>
        /// The index of the last produced image
        /// </summary>
        private volatile int _imageIndex;

        /// <summary>
        /// Indicates how many images are currently stored in the buffer
        /// This is a dumb counter. Gets incremented by production and decremented
        /// by consumption - does not take overflow into account.
        /// </summary>
        private int _storageCount;

        #endregion//Fields

        #region Properties

        /// <summary>
        /// The total number of images the buffer can hold
        /// </summary>
        public int NImages
        {
            get
            {
                return _nImages;
            }
        }

        /// <summary>
        /// The width of the images stored in the buffer
        /// </summary>
        public int ImageWidth
        {
            get
            {
                return _imageWidth;
            }
        }

        /// <summary>
        /// The height of the images stored in the buffer
        /// </summary>
        public int ImageHeight
        {
            get
            {
                return _imageHeight;
            }
        }

        /// <summary>
        /// The stride of the images stored in the buffer
        /// </summary>
        public int ImageStride
        {
            get
            {
                return _imageStride;
            }
        }

        /// <summary>
        /// The index of the last produced image
        /// </summary>
        public int ImageIndex
        {
            get
            {
                return _imageIndex;
            }
        }

        #endregion//Properties

        /// <summary>
        /// Creates a new thread-safe ring-buffer
        /// </summary>
        /// <param name="imageWidth">The desired width of the images to be stored in the buffer</param>
        /// <param name="imageHeight">The desired height of the images to be stored in the buffer</param>
        /// <param name="nImages">The number of images the buffer should hold</param>
        public PrCoImageRingBuffer(int imageWidth, int imageHeight, int nImages)
        {
            if (nImages < 1)
                throw new ArgumentOutOfRangeException("The buffer has to hold at least one image");
            if (imageWidth < 1 || imageHeight < 1)
                throw new ArgumentOutOfRangeException("Width and height have to be larger than 0");
            _imageWidth = imageWidth;
            _imageHeight = imageHeight;
            _nImages = nImages;
            _buffers = new byte*[_nImages];
            _imageIndices = new int[_nImages];
            _storageCount = 0; //we don't have images in the buffer yet
            _lastConsume = -1;//the first consumption will happen in buffer 0
            _lastProduce = -1;//the first production will happen in buffer 0
            _imageIndex = -1;//no image produced yet
            //Calculate the corresponding image stride for 4-byte boundary alignment
            _imageStride = (int)(4 * Math.Ceiling(_imageWidth / 4.0));
            //Allocate sub-buffers
            for (int i = 0; i < _nImages; i++)
            {
                _buffers[i] = (byte*)Marshal.AllocHGlobal(_imageStride * _imageHeight).ToPointer();
                _imageIndices[i] = -1;//right now, no buffer position contains an image
            }          
        }

        #region Methods

        /// <summary>
        /// Produces an image in the ring buffer
        /// </summary>
        /// <param name="image">The image to write into the buffer</param>
        public void Produce(Image8 image)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("PrCoImageRingBuffer");
            if (image == null)
                return;
            if (image.Width != _imageWidth || image.Height != _imageHeight)
                throw new ArgumentException("The produced image is not compatible with the buffer");
            lock (_accessLock)
            {
                //advance our production buffer "pointer" to the next place in the ring buffer
                _lastProduce = (_lastProduce + 1) % _nImages;
                //increment _imageIndex internally
                var temp = _imageIndex+1;
                //copy image over
                ip.ippiCopy_8u_C1R(image.Image, image.Stride, _buffers[_lastProduce],_imageStride, new IppiSize(_imageWidth, _imageHeight));
                //update the indices in the production place with the current image index
                _imageIndices[_lastProduce] = temp;
                //Image has been produced, update index and storage counter
                _imageIndex = temp;
                _storageCount++;
                //Signal waiting threads that we are about to release the lock
                Monitor.Pulse(_accessLock);
            }
        }

        /// <summary>
        /// Consumes an image from the ring buffer
        /// </summary>
        /// <param name="imageOut">The Image8 into which the consumed image should be copied</param>
        /// <param name="stop">Wait handle to signal that the consumption thread is about to stop</param>
        /// <returns>The index of the retrieved image</returns>
        public int Consume(Image8 imageOut, AutoResetEvent stop)
        {
            int retval;

            if (_isDisposed)
                throw new ObjectDisposedException("PrCoImageRingBuffer");
            if (imageOut == null)
                throw new ArgumentNullException("imageOut");
            if (imageOut.Width != _imageWidth || imageOut.Height != _imageHeight)
                throw new ArgumentException("The recieving image is not compatible with the buffer");

            lock (_accessLock)
            {
                while (_storageCount < 1)
                {
                    //If we are told to stop then we throw an OperationCanceledException
                    if (stop.WaitOne(0))
                        throw new OperationCanceledException("Consume signaled to stop");
                    //We wait for a pulse for 100ms. After that we independently try to reacquire the lock.
                    //This way if the the producer has retired we are not indefinitely stuck on the wait and can listen
                    //for a stop signal
                    Monitor.Wait(_accessLock, 100);
                }
                //There is at least one image in storage, lets consume it
                //advance the consumption "pointer" to the next place in the buffer
                _lastConsume = (_lastConsume + 1) % _nImages;
                //copy image over
                ip.ippiCopy_8u_C1R(_buffers[_lastConsume], _imageStride, imageOut.Image, imageOut.Stride, new IppiSize(_imageWidth, _imageHeight));
                //we want to return the index of the consumed image
                retval = _imageIndices[_lastConsume];
                //decrement storage counter
                _storageCount--;
            }

            return retval;
        }

        /// <summary>
        /// Consumes an image from the ring buffer
        /// </summary>
        /// <param name="imageOut">The Image8 into which the consumed image should be copied</param>
        /// <param name="expectedIndex">The index we expect to recieve</param>
        /// <param name="stop">Wait handle to signal that the consumption thread is about to stop</param>
        /// <returns>True if the expected index could be consumed, false otherwise</returns>
        public bool Consume(Image8 imageOut, int expectedIndex, AutoResetEvent stop)
        {
            int indexRetrieved = Consume(imageOut, stop);
            if (indexRetrieved != expectedIndex)
            {
                System.Diagnostics.Debug.WriteLine("Ring buffer overflow. Expected image index {0} got image index {1}.", expectedIndex, indexRetrieved);
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Virtually resets the ring buffer (does NOT clear memory)
        /// </summary>
        public void ClearBuffer()
        {
            if (_isDisposed)
                throw new ObjectDisposedException("PrCoImageRingBuffer");
            lock (_accessLock)
            {
                _storageCount = 0; //we don't have images in the buffer yet
                _lastConsume = -1;//the first consumption will happen in buffer 0
                _lastProduce = -1;//the first production will happen in buffer 0
                _imageIndex = -1;//no image produced yet
                //Reset indices
                for (int i = 0; i < _nImages; i++)
                {
                    _imageIndices[i] = -1;//right now, no buffer position contains an image
                }   
            }
        }

        #endregion

        #region IDisposable

        bool _isDisposed;

        /// <summary>
        /// Indicates if the object has already been disposed
        /// </summary>
        public bool IsDisposed
        {
            get
            {
                return _isDisposed;
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;
            for (int i = 0; i < _nImages; i++)
            {
                Marshal.FreeHGlobal((IntPtr)_buffers[i]);
            }
            _isDisposed = true;
        }

        ~PrCoImageRingBuffer()
        {
            if (!_isDisposed)
            {
                System.Diagnostics.Debug.WriteLine("PrCoImageRingBuffer was not disposed properly");
                Dispose();
            }
        }

        #endregion
    }
}
