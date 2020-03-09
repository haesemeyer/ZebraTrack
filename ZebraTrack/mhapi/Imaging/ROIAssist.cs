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
using System.Drawing;
using MHApi.DrewsClasses;

namespace MHApi.Imaging {
    public unsafe class ROIAssist : IDisposable {
        
        //The step-size in pixels of the grid
        readonly int gridStep;

        //The image containing the grid
        readonly Image8 gridImage;

        //Construct ROIAssist to draw grid with 10 pixel gridstep
        public ROIAssist(int width, int height) {
            gridStep = 10;
            gridImage = new Image8(width,height);
            MakeGrid();
        }

        //Construct ROIAssist to draw grid with given sized gridstep
        public ROIAssist(int width, int height, int step) {
            gridStep = step;
            gridImage = new Image8(width, height);
            MakeGrid();
        }

        //Construct ROIAssist to draw a rectangular ROI of the given dimensions
        public ROIAssist(int width, int height, Point roiUpperLeft, Size roiSize) {
            gridStep = -1;
            gridImage = new Image8(width,height);
            MakeRectangle(roiUpperLeft,roiSize);
        }

        //Draws the initial grid image
        private void MakeGrid() {
            //draw grid
            for (int i = 0; i < gridImage.Width; i++) {
                for (int j = 0; j < gridImage.Height; j++) {
                    byte pixel;
                    if (i % gridStep == 0 || j % gridStep == 0) {
                        pixel = 255;
                    } else {
                        pixel = 0;
                    }
                    gridImage.Image[j * gridImage.Stride + i] = pixel;
                }
            }
        }

        //Draws the initial image as a rectangle rather than a grid
        private void MakeRectangle(Point roiUpperLeft, Size roiSize) {
            //draw rectangle
            for(int i=0; i<gridImage.Width; i++) {
                for(int j=0; j<gridImage.Height; j++) {
                    byte pixel;
                    if(i==roiUpperLeft.X || i==roiUpperLeft.X+roiSize.Width || j==roiUpperLeft.Y || j==roiUpperLeft.Y+roiSize.Height) {
                        if (i < roiUpperLeft.X || i > roiUpperLeft.X + roiSize.Width || j < roiUpperLeft.Y || j > roiUpperLeft.Y + roiSize.Height) {
                            pixel = 0;
                        } else {
                            pixel = 255;
                        }
                    }
                    else {
                        pixel = 0;
                    }
                    gridImage.Image[j*gridImage.Stride + i] = pixel;
                }
            }
        }

        //Draws the grid onto a provided image using ippAdd
        public void DrawGrid(Image8 im) {
            System.Diagnostics.Debug.Assert(im.Width == gridImage.Width);
            System.Diagnostics.Debug.Assert(im.Height == gridImage.Height);
            ip.ippiAdd_8u_C1IRSfs(gridImage.Image, gridImage.Stride, im.Image, im.Stride, im.Size, 0);
        }


        #region IDisposable members
        bool isDisposed;
        
        public void Dispose() {
            if (isDisposed)
                return;
            gridImage.Dispose();
            isDisposed = true;
        }

        ~ROIAssist() {
            Dispose();
        }
        #endregion
    }
}
