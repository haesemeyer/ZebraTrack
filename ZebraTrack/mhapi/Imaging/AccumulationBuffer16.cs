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

using ipp;

namespace MHApi.Imaging
{
    /// <summary>
    /// Circular buffer for accumulating across
    /// a set number of 16-bit images
    /// </summary>
    /// <remarks>This class is not thread-safe</remarks>
    public unsafe class AccumulationBuffer16 : IDisposable
    {
        /// <summary>
        /// The current accumulated image
        /// </summary>
        Image16 _accumulated;

        /// <summary>
        /// The internal image buffer
        /// </summary>
        Image16[] _images;

        /// <summary>
        /// Image addition counter
        /// </summary>
        uint _counter;

        /// <summary>
        /// Creates a new accumulation buffer
        /// </summary>
        /// <param name="accumulationWindow">The number of images to accumulate across</param>
        /// <param name="width">The width of each image</param>
        /// <param name="height">The height of each image</param>
        public AccumulationBuffer16(uint accumulationWindow, int width, int height)
        {
            if (accumulationWindow < 1)
                throw new ArgumentOutOfRangeException(nameof(accumulationWindow), "Has to be larger than 0");
            if (width < 1)
                throw new ArgumentOutOfRangeException(nameof(width), "Has to be larger than 0");
            if(height<1)
                throw new ArgumentOutOfRangeException(nameof(height), "Has to be larger than 0");
            _images = new Image16[accumulationWindow];
            for(int i = 0; i < accumulationWindow; i++)
            {
                //create images and set pixels to 0
                _images[i] = new Image16(width, height);
                ip.ippiSet_16u_C1R(0, _images[i].Image, _images[i].Stride, _images[i].Size);
            }
            //create our cumulant image and set to 0
            _accumulated = new Image16(width, height);
            ip.ippiSet_16u_C1R(0, _accumulated.Image, _accumulated.Stride, _accumulated.Size);
        }

        /// <summary>
        /// The current accumulated image
        /// </summary>
        public Image16 Accumulated
        {
            get
            {
                return _accumulated;
            }
        }

        /// <summary>
        /// Adds a new image to the rolling cumulant
        /// </summary>
        /// <param name="im">The image to add</param>
        /// <returns>The new accumulated image after the addition</returns>
        public Image16 AppendImage(Image16 im)
        {
            if (im.Width != _accumulated.Width || im.Height != _accumulated.Height)
                throw new ArgumentOutOfRangeException(nameof(im), "Dimensions of added image must match dimensions of buffer.");
            uint ixIn = (uint)(_counter % _images.Length);//new image gets added in this position
            //add the new image and subtract the image that is currently at ixIn from _accumulated
            //note that these operations can lead to saturation
            ip.ippiSub_16u_C1IRSfs(_images[ixIn].Image, _images[ixIn].Stride, _accumulated.Image, _accumulated.Stride, _accumulated.Size, 0);
            ip.ippiAdd_16u_C1IRSfs(im.Image, im.Stride, _accumulated.Image, _accumulated.Stride, _accumulated.Size, 0);
            //copy new image into buffer
            ip.ippiCopy_16u_C1R(im.Image, im.Stride, _images[ixIn].Image, _images[ixIn].Stride, im.Size);
            _counter++;
            return _accumulated;
        }

        /// <summary>
        /// Sets the accumulated image and all buffer
        /// contents to 0
        /// </summary>
        public void Reset()
        {
            if (_images == null)
                return;
            for (int i = 0; i < _images.Length; i++)
            {
                ip.ippiSet_16u_C1R(0, _images[i].Image, _images[i].Stride, _images[i].Size);
            }
            ip.ippiSet_16u_C1R(0, _accumulated.Image, _accumulated.Stride, _accumulated.Size);
        }

        /// <summary>
        /// Free resources
        /// </summary>
        public void Dispose()
        {
            if(_images != null)
            {
                for (int i = 0; i < _images.Length; i++)
                    if (_images[i] != null)
                        _images[i].Dispose();
                _images = null;
            }
            if(_accumulated != null)
            {
                _accumulated.Dispose();
                _accumulated = null;
            }
        }
    }
}
