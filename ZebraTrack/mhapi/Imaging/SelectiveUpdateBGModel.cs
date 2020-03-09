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

using ipp;

using MHApi.DrewsClasses;

namespace MHApi.Imaging
{
    public unsafe class SelectiveUpdateBGModel : DynamicBackgroundModel
    {

        public SelectiveUpdateBGModel(Image8 im) : base(im, 0.1F) {
            _updateMask = new Image8(im.Width, im.Height);
        }

        public SelectiveUpdateBGModel(Image8 im, float fUpdate) : base(im, fUpdate) {
            _updateMask = new Image8(im.Width, im.Height);
        }

        /// <summary>
        /// We restrict our background update to regions that are not within
        /// fish bounding boxes by doing a masked background update
        /// </summary>
        protected Image8 _updateMask;

        /// <summary>
        /// Update background excluding detected regions from the update
        /// </summary>
        /// <param name="im">The new image to add to the background</param>
        /// <param name="regionsToExclude">The blobs that should be excluded from the update</param>
        [Obsolete("Don't use Blob[] version but use BlobWithMoments[] version of method.")]
        public virtual void UpdateBackground(Image8 im, Blob[] regionsToExclude)
        {
            if (regionsToExclude == null)//do a simple update
                UpdateBackground(im);
            else
            {
                //reset our mask to <update all>, then fill all bounding boxes with 0s
                ip.ippiSet_8u_C1R(byte.MaxValue, _updateMask.Image, _updateMask.Stride, _updateMask.Size);
                foreach (Blob b in regionsToExclude)
                {
                    if (b != null)
                    {
                        IppiROI roi = new IppiROI(b.BoundingBox);
                        ip.ippiSet_8u_C1R(0, _updateMask[roi.TopLeft], _updateMask.Stride, roi.Size);
                    }
                }

                IppHelper.IppCheckCall(cv.ippiAddWeighted_8u32f_C1IMR(im.Image, im.Stride, _updateMask.Image, _updateMask.Stride, background.Image, background.Stride, im.Size, FractionUpdate));
            }
        }

        /// <summary>
        /// Update background excluding detected regions from the update
        /// </summary>
        /// <param name="im">The new image to add to the background</param>
        /// <param name="regionsToExclude">The blobs that should be excluded from the update</param>
        public virtual void UpdateBackground(Image8 im, BlobWithMoments[] regionsToExclude)
        {
            if (regionsToExclude == null)
                UpdateBackground(im);
            else
            {
                //reset our mask to <update all>, then fill all bounding boxes with 0s
                ip.ippiSet_8u_C1R(byte.MaxValue, _updateMask.Image, _updateMask.Stride, _updateMask.Size);
                foreach (BlobWithMoments b in regionsToExclude)
                {
                    if (b != null)
                    {
                        ip.ippiSet_8u_C1R(0, _updateMask[b.BB_TopLeft], _updateMask.Stride, b.BB_Size);
                    }
                }

                IppHelper.IppCheckCall(cv.ippiAddWeighted_8u32f_C1IMR(im.Image, im.Stride, _updateMask.Image, _updateMask.Stride, background.Image, background.Stride, im.Size, FractionUpdate));
            }
        }

        public virtual void UpdateBackground(Image8 im, BlobWithMoments regionToExclude)
        {
            if (regionToExclude == null)
                UpdateBackground(im);
            else
            {
                //reset our mask to <update all>, then fill bounding box with 0s
                ip.ippiSet_8u_C1R(byte.MaxValue, _updateMask.Image, _updateMask.Stride, _updateMask.Size);
                IppiROI roi = new IppiROI(regionToExclude.BoundingBox.x, regionToExclude.BoundingBox.y, regionToExclude.BoundingBox.width, regionToExclude.BoundingBox.height);
                ip.ippiSet_8u_C1R(0, _updateMask[regionToExclude.BB_TopLeft], _updateMask.Stride, regionToExclude.BB_Size);
                IppHelper.IppCheckCall(cv.ippiAddWeighted_8u32f_C1IMR(im.Image, im.Stride, _updateMask.Image, _updateMask.Stride, background.Image, background.Stride, im.Size, FractionUpdate));
            }
        }

        protected override void Dispose(bool disposing)
        {
            _updateMask.Dispose();
            base.Dispose(disposing);
        }
    }
}
