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

using MHApi.DrewsClasses;
using ipp;

namespace MHApi.Imaging
{
    /// <summary>
    /// Provides High-level access to image thresholding
    /// and morphology operations
    /// </summary>
    public unsafe static class BWImageProcessor
    {

        /// <summary>
        /// Implements a "greater than" threshold like MATLABS
        /// im2bw function
        /// </summary>
        /// <param name="imIn">The image to threshold</param>
        /// <param name="imThresh">The image after thresholding</param>
        /// <param name="region">The ROI in which to perform the operation</param>
        /// <param name="threshold">The threshold to apply</param>
        public static void Im2Bw(Image8 imIn, Image8 imThresh, IppiROI region, byte threshold)
        {
            IppHelper.IppCheckCall(ip.ippiThreshold_LTVal_8u_C1R(imIn[region.TopLeft], imIn.Stride, imThresh[region.TopLeft], imThresh.Stride, region.Size, (byte)(threshold + 1), 0));
            IppHelper.IppCheckCall(ip.ippiThreshold_GTVal_8u_C1IR(imThresh[region.TopLeft], imThresh.Stride, region.Size, threshold, 255));
        }
    }
}
