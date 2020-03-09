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
using MHApi.DrewsClasses;

namespace MHApi.Imaging {
    public unsafe class DifferenceDetector : IDisposable {
        readonly Image32F temp1, temp2;

        public int RoiX { get; private set; }
        public int RoiY { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public DifferenceDetector(int x, int y, int width, int height) {
            temp1 = new Image32F(width,height);
            temp2 = new Image32F(width,height);
            Width = width;
            Height = height;
            RoiX = x;
            RoiY = y;
        }

        public void GetDifferenceImage(Image8 im1, Image8 im2, Image8 imDiff) {            
            System.Diagnostics.Debug.Assert(im1.Width==im2.Width);//both input images should have the same size
            System.Diagnostics.Debug.Assert(im1.Height == im2.Height);
            System.Diagnostics.Debug.Assert(Height == imDiff.Height);//difference image should have same size as ROI
            System.Diagnostics.Debug.Assert(Width == imDiff.Width);
            //increment pointer of source image to point at the start of the ROI, then use temp1.size to supply the ROI size
            ip.ippiConvert_8u32f_C1R(im1.Image+RoiY*im1.Stride+RoiX, im1.Stride, temp1.Image, temp1.Stride, temp1.Size);
            ip.ippiConvert_8u32f_C1R(im2.Image+RoiY*im2.Stride+RoiX, im2.Stride, temp2.Image, temp2.Stride, temp2.Size);
            ip.ippiSub_32f_C1IR(temp1.Image, temp1.Stride, temp2.Image, temp2.Stride, temp2.Size);
            ip.ippiAbs_32f_C1IR(temp2.Image, temp2.Stride, temp2.Size);
            temp2.ReduceTo8U(imDiff);
        }

        public double GetMeanDifference(Image8 im1, Image8 im2) {
            var diff = new double[1];
            var imDiff = new Image8(im1.Width, im1.Height);
            try {
                fixed (double* pDiff = diff) {
                    GetDifferenceImage(im1,im2,imDiff);
                    ip.ippiMean_8u_C1R(imDiff.Image, imDiff.Stride, imDiff.Size, pDiff);
                }
            }
            finally {
                imDiff.Dispose();
            }
            return diff[0];
        }

        public double GetMeanDifference(Image8 imDiff) {
            var diff = new double[1];
            fixed (double* pDiff = diff) {
                ip.ippiMean_8u_C1R(imDiff.Image, imDiff.Stride, imDiff.Size, pDiff);
            }           
            return diff[0];
        }

        #region IDisposable members
        bool isDisposed;
        public void Dispose() {
            if (isDisposed)
                return;
            temp1.Dispose();
            temp2.Dispose();
            isDisposed = true;
        }

        ~DifferenceDetector() {
            Dispose();
        }
        #endregion
    }
}
