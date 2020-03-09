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
using MHApi.DrewsClasses;

namespace MHApi.Imaging
{
    public unsafe class DynamicBackgroundModel : BackgroundModel
    {

        /// <summary>
        /// The fraction of the current image to use for updating the background
        /// </summary>
        public float FractionUpdate { get; protected set; }

        public override void UpdateBackground(Image8 im)
        {
            if (im.Width != width || im.Height != height)
                throw new ArgumentException("The supplied image must have the same dimensions as the background");
            IppHelper.IppCheckCall(cv.ippiAddWeighted_8u32f_C1IR(im.Image, im.Stride, background.Image, background.Stride, im.Size, FractionUpdate));
        }

        /// <summary>
        /// Default constructor setting FractionCurrent to 0.1
        /// </summary>
        /// <param name="im">The initial background image</param>
        public DynamicBackgroundModel(Image8 im)
            : base(im)
        {
            FractionUpdate = 0.1F;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="im">The initial background image</param>
        /// <param name="fUpdate">The fraction of the current image to use for updating the background</param>
        public DynamicBackgroundModel(Image8 im, float fUpdate) : base(im) {
            if (0 > fUpdate || 1 < fUpdate)
            {
                throw new ArgumentOutOfRangeException("fUpdate", "The update fraction has to be bigger than 0 and smaller than 1");
            }
            FractionUpdate = fUpdate;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
