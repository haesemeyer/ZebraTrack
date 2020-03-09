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
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading;
//using System.Drawing;

using MHApi.DrewsClasses;
using MHApi.Utilities;

using ipp;

namespace MHApi.Imaging
{

    /// <summary>
    /// Baseclass for fast graphing of point and line graphs
    /// </summary>
    public unsafe class Graph : IDisposable, INotifyPropertyChanged
    {
        /// <summary>
        /// Represents axes/graphing limit
        /// </summary>
        public struct Limit
        {
            private double _min;

            private double _max;

            /// <summary>
            /// The minimum of the limits
            /// </summary>
            public double Min
            {
                get
                {
                    return _min;
                }
                set
                {
                    _min = value;
                }
            }

            /// <summary>
            /// The maximum of the limits
            /// </summary>
            public double Max
            {
                get
                {
                    return _max;
                }
                set
                {
                    _max = value;
                }
            }

            public Limit(double min, double max)
            {
                _min = min;
                _max = max;
            }
        }

        #region Fields and Properties

        /// <summary>
        /// The image to draw on
        /// </summary>
        private Image8 _canvas;

        private WriteableBitmap _guiImage;

        /// <summary>
        /// The bitmap source that gets updated on the ui thread when the graph is updated
        /// </summary>
        public BitmapSource GuiImage
        {
            get
            {
                return _guiImage;
            }
        }

        /// <summary>
        /// A byte[3] array for holding color
        /// </summary>
        protected byte* _color;

        protected object _canvasLockObject = new object();

        /// <summary>
        /// The canvas that contains the graph
        /// </summary>
        protected Image8 Canvas
        {
            get
            {
                lock (_canvasLockObject)
                {
                    return _canvas;
                }
            }
            set
            {
                lock (_canvasLockObject)
                {
                    _canvas = value;
                }
            }
        }

        protected ManualResetEvent _stop = new ManualResetEvent(false);

        public int Width { get; private set; }

        public int Height { get; private set; }

        #endregion

        public Graph(int width, int height)
        {
            Width = width;
            Height = height;
            //Initialize our color placeholder
            _color = (byte*)Marshal.AllocHGlobal(3).ToPointer();
            //Initialize new canvas for the bgr pixels (hence width*3)
            _canvas = new Image8(width*3, height);
            ClearImage();
        }

        #region Methods


        /*
        /// <summary>
        /// Updates the user interface accesible image representation of the graph on the gui thread
        /// </summary>
        protected virtual void UpdateUserInterface()
        {
           Image8 im = new Image8(_canvas.Width, _canvas.Height);//directly access field to avoid lock which is not necessary since width and height never change!
           //Copy data of the canvas over, to prevent untimely Disposal from wrekking havoc
           ip.ippiCopy_8u_C1R(Canvas.Image, Canvas.Stride, im.Image, im.Stride, new IppiSize(Canvas.Width, Canvas.Height));
           
               DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    //Create new fitting bitmap source
                    _guiImage = new WriteableBitmap(Width, Height, 96, 96, PixelFormats.Bgr24, null);                   
                    //Write data into gui bitmap
                    _guiImage.WritePixels(new Int32Rect(0, 0, Width, Height), (IntPtr)im.Image, im.Stride * im.Height, im.Stride);
                    im.Dispose();
                });
           
           RaisePropertyChanged("GuiImage");
        }
        */

        /// <summary>
        /// Updates the user interface accesible image representation of the graph on the gui thread - BLOCKS until the ui thread is done
        /// This avoids repeatedly copying the canvas
        /// </summary>
        protected virtual void UpdateUserInterface()
        {
            if (_stop.WaitOne(0))
                return;

            AutoResetEvent guiDone = new AutoResetEvent(false);

            DispatcherHelper.CheckBeginInvokeOnUI(() =>
            {
                //Create new fitting bitmap source
                if(_guiImage == null || (int)_guiImage.Width != Width || (int)_guiImage.Height != Height)
                    _guiImage = new WriteableBitmap(Width, Height, 96, 96, PixelFormats.Bgr24, null);
                //Write data into gui bitmap
                if(Canvas!=null)
                    _guiImage.WritePixels(new Int32Rect(0, 0, Width, Height), (IntPtr)Canvas.Image, Canvas.Stride * Canvas.Height, Canvas.Stride);
                guiDone.Set();
            });
            WaitHandle.WaitAny(new WaitHandle[] { _stop, guiDone });
            RaisePropertyChanged("GuiImage");
        }

        /// <summary>
        /// Forces the graph to abandon UI changes
        /// </summary>
        public void Stop()
        {
            _stop.Set();
        }


        /// <summary>
        /// Draws a point at the absolute canvas x (0->width),y (0->height) position
        /// </summary>
        /// <param name="p">The point to draw</param>
        /// <param name="blue">The blue intensity</param>
        /// <param name="green">The green intensity</param>
        /// <param name="red">The red intensity</param>
        protected virtual void DrawPointAbsolute(Point p, Color col)
        {
            if (IsDisposed)
                throw new ObjectDisposedException("XYGraph");
            int x = (int)p.X;
            int y = (int)p.Y;
            //if the point is outside of the (physical) canvas we don't draw it
            if (x < 0 || x >= Width)
                return;
            if (y < 0 || y >= Height)
                return;
            int positionB = y * _canvas.Stride + x*3;
            int positionG = y * _canvas.Stride + x*3 + 1;
            int positionR = y * _canvas.Stride + x*3 + 2;
            _canvas.Image[positionB] = col.B;
            _canvas.Image[positionG] = col.G;
            _canvas.Image[positionR] = col.R;
        }

        /// <summary>
        /// Draws a line connecting two points on the canvas
        /// </summary>
        /// <param name="start">The starting point of the line</param>
        /// <param name="end">The ending point of the line</param>
        /// <param name="blue">The blue intensity</param>
        /// <param name="green">The green intensity</param>
        /// <param name="red">The red intensity</param>
        protected virtual void DrawLineAbsolute(Point start, Point end, Color col)
        {
            Point[] line = BresenhamLine(start, end);
            foreach (Point p in line)
                DrawPointAbsolute(p, col);
        }

        /// <summary>
        /// Sets all pixels to 0 (black)
        /// </summary>
        protected virtual void ClearImage()
        {
            if (IsDisposed)
                throw new ObjectDisposedException("XYGraph");
            _color[0] = _color[1] = _color[2] = 0;
            ipp.ip.ippiSet_8u_C3R(_color, _canvas.Image, _canvas.Stride, new ipp.IppiSize(Width,Height));
        }

        /// <summary>
        /// Fills the canvas with the selected color
        /// </summary>
        /// <param name="blue">The blue intensity</param>
        /// <param name="green">The green intensity</param>
        /// <param name="red">The red intensity</param>
        protected virtual void Fill(Color col)
        {
            _color[0] = col.B;
            _color[1] = col.G;
            _color[2] = col.R;
            ipp.ip.ippiSet_8u_C3R(_color, _canvas.Image, _canvas.Stride, new ipp.IppiSize(Width, Height));
        }

        /// <summary>
        /// Computes a line connecting the endpoints using the Bresenham algorithm
        /// </summary>
        /// <param name="start">The starting point</param>
        /// <param name="end">The ending point</param>
        /// <returns>An array of points forming the line including start and ending points</returns>
        protected Point[] BresenhamLine(Point start, Point end)
        {
            bool steep = Math.Abs(end.Y - start.Y) > Math.Abs(end.X - start.X);
            int x0,x1,y0,y1;
            if (steep)
            {
                x0 = (int)start.Y;
                y0 = (int)start.X;
                x1 = (int)end.Y;
                y1 = (int)end.X;
            }
            else
            {
                x0 = (int)start.X;
                y0 = (int)start.Y;
                x1 = (int)end.X;
                y1 = (int)end.Y;
            }

            if (x0 > x1)
            {
                int temp = x0;
                x0 = x1;
                x1 = temp;
                temp = y0;
                y0 = y1;
                y1 = temp;
            }

            int deltaX = x1 - x0;

            //if there is no x-difference we just return the startpoint
            if (deltaX == 0)
            {
                Point[] ret = new Point[1];
                ret[0] = start;
                return ret;
            }

            int deltaY = Math.Abs(y1 - y0);

            double error = 0;
            double deltaError = deltaY / deltaX;

            int ystep;
            int y = y0;

            if (y0 < y1)
                ystep = 1;
            else
                ystep = -1;

            Point[] retval = new Point[x1 - x0 + 1];

            for (int i = 0; i < retval.Length; i++)
            {
                if (steep)
                {
                    retval[i].X = y;
                    retval[i].Y = x0 + i;
                }
                else
                {
                    retval[i].X = x0 + i;
                    retval[i].Y = y;
                }
                error += deltaError;
                if (error >= 0.5)
                {
                    y = y + ystep;
                    error -= 1;
                }
            }

            return retval;

            /*
            function line(x0, x1, y0, y1)
     boolean steep := abs(y1 - y0) > abs(x1 - x0)
     if steep then
         swap(x0, y0)
         swap(x1, y1)
     if x0 > x1 then
         swap(x0, x1)
         swap(y0, y1)
     int deltax := x1 - x0
     int deltay := abs(y1 - y0)
     real error := 0
     real deltaerr := deltay / deltax
     int ystep
     int y := y0
     if y0 < y1 then ystep := 1 else ystep := -1
     for x from x0 to x1
         if steep then plot(y,x) else plot(x,y)
         error := error + deltaerr
         if error ≥ 0.5 then
             y := y + ystep
             error := error - 1.0
*/
        }

        #endregion


        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        protected void RaisePropertyChanged(string name)
        {
            if (name != "")
            {
                VerifyPropertyName(name);
            }
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        protected void VerifyPropertyName(string propertyName)
        {
            var myType = GetType();
            if (myType.GetProperty(propertyName) == null)
                throw new ArgumentException("Property not found", propertyName);
        }
        #endregion


        #region IDisposable

        public bool IsDisposed { get; private set; }

        public virtual void Dispose()
        {
            _stop.Set();
            if (_canvas != null)
            {
                _canvas.Dispose();
                _canvas = null;
            }
            if (_color != null)
            {
                Marshal.FreeHGlobal((IntPtr)_color);
                _color = null;
            }
            IsDisposed = true;
        }

        #endregion
    }
}
