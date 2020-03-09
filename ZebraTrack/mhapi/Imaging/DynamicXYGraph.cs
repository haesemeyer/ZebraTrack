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
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

//using System.ComponentModel;

using ipp;

using MHApi.DrewsClasses;

namespace MHApi.Imaging
{
    /// <summary>
    /// Draws a graph to which new points are dynamically added
    /// </summary>
    public unsafe class DynamicXYGraph : Graph
    {
        private bool _connectingLines;

        private Color _bgColor;

        private Color _axesColor;

        /// <summary>
        /// The xLimits in user xy coordinates
        /// </summary>
        private Graph.Limit _xLim;

        /// <summary>
        /// The x limits in canvas coordinates of the graphing area
        /// </summary>
        private Graph.Limit _xLimAbsolute;

        /// <summary>
        /// The yLimits in user xy coordinates
        /// </summary>
        private Graph.Limit _yLim;

        /// <summary>
        /// The y limits in canvas coordinates of the graphing area
        /// </summary>
        private Graph.Limit _yLimAbsolute;

        /// <summary>
        /// The colors associated with individual channels - also fixes the actual number of channels!
        /// </summary>
        private Color[] _fgChannels;

        //private List<Point>[] _channelPoints;

        /// <summary>
        /// The previous points for each channel that have been drawn
        /// </summary>
        private Point[] _previous;

        /// <summary>
        /// Indicates whether the first point has already been drawn
        /// </summary>
        private bool[] _firstDrawDone;

        

        /// <summary>
        /// The number of drawable channels in the graph
        /// </summary>
        public int NumberOfChannels
        {
            get
            {
                return _fgChannels.Length;
            }
        }

        /// <summary>
        /// Determines if we only draw new points or connect each new point with a line to
        /// the previous
        /// </summary>
        public bool ConnectingLines
        {
            get { return _connectingLines; }
            private set { _connectingLines = value; }
        }

        /// <summary>
        /// The background color of the graph
        /// </summary>
        public Color BgColor
        {
            get
            {
                return _bgColor;
            }
        }

        /// <summary>
        /// The color of the axes on the graph
        /// </summary>
        public Color AxesColor
        {
            get
            {
                return _axesColor;
            }
        }

        /// <summary>
        /// The XLimits of points on the graph
        /// </summary>
        public Graph.Limit XLimits
        {
            get
            {
                return _xLim;
            }
        }

        /// <summary>
        /// The YLimits of points on the graph
        /// </summary>
        public Graph.Limit yLimits
        {
            get
            {
                return _yLim;
            }
        }

        /// <summary>
        /// The Colors of the different foreground channels
        /// </summary>
        public Color[] FgChannelColors
        {
            get
            {
                return _fgChannels;
            }
        }

        #region Constructors


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="width">Pixel width of the graph</param>
        /// <param name="height">Pixel height of the graph</param>
        /// <param name="xLim">The xLimits of plottable values</param>
        /// <param name="yLim">The yLimits of plottable values</param>
        /// <param name="connectingLines">Should consecutive points be connected?</param>
        /// <param name="channels">The colors of foreground channels that are used to draw the graphs</param>
        public DynamicXYGraph(int width, int height,Graph.Limit xLim, Graph.Limit yLim, bool connectingLines, Color[] fgChannels)
            : base(width, height)
        {
            ConnectingLines = connectingLines;
            _xLim = xLim;
            _yLim = yLim;
            //we at least add one channel
            if (fgChannels != null && fgChannels.Length > 1)
                _fgChannels = fgChannels;
            else
            {
                _fgChannels = new Color[1];
                _fgChannels[0] = Colors.Red;
            }

            //Set up the points array that will hold the previous drawn points as well as
            //array indicating if first draw has occured for a given channel
            _previous = new Point[_fgChannels.Length];
            _firstDrawDone = new bool[_fgChannels.Length];

            //Default is white background
            _bgColor = Colors.White;
            Fill(_bgColor);
            //Default is black axes
            _axesColor = Colors.Black;
            //We draw the X-Axis at the x-Coordinate _xLim.Min and the Y-Axis at _yLim.Min
            //In absolute coordinates we leave 5% of pixels on each side free of the graph
            //Therefore:
            _xLimAbsolute.Min = Width / 20.0;
            _xLimAbsolute.Max = Width - _xLimAbsolute.Min;
            _yLimAbsolute.Min = Height / 20.0;
            _yLimAbsolute.Max = Height - _yLimAbsolute.Min;
            DrawAxes();
            UpdateUserInterface();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="widht">Pixel width of the graph</param>
        /// <param name="height">Pixel height of the graph</param>
        /// <param name="xLim">The xLimits of plottable values</param>
        /// <param name="yLim">The yLimits of plottable values</param>
        /// <param name="bg">The background color of the graph</param>
        /// <param name="axes">The axes color on the graph</param>
        /// <param name="connectingLines">Should consecutive points be connected?</param>
        /// /// <param name="channels">The colors of foreground channels that are used to draw the graphs</param>
        public DynamicXYGraph(int widht, int height, Graph.Limit xLim, Graph.Limit yLim,Color bg, Color axes, bool connectingLines, Color[] fgChannels)
            :base(widht,height)
        {
            ConnectingLines = connectingLines;
            _xLim = xLim;
            _yLim = yLim;
            _bgColor = bg;
            _axesColor = axes;
            //we at least add one channel
            if (fgChannels!= null && fgChannels.Length > 1)
                _fgChannels = fgChannels;
            else
            {
                _fgChannels = new Color[1];
                _fgChannels[0] = Colors.Red;
            }
            //Set up the points array that will hold the previous drawn points as well as
            //array indicating if first draw has occured for a given channel
            _previous = new Point[_fgChannels.Length];
            _firstDrawDone = new bool[_fgChannels.Length];

            Fill(_bgColor);
            //We draw the X-Axis at the x-Coordinate _xLim.Min and the Y-Axis at _yLim.Min
            //In absolute coordinates we leave 5% of pixels on each side free of the graph
            //Therefore:
            _xLimAbsolute.Min = Width / 20.0;
            _xLimAbsolute.Max = Width - _xLimAbsolute.Min;
            _yLimAbsolute.Min = Height / 20.0;
            _yLimAbsolute.Max = Height - _yLimAbsolute.Min;
            DrawAxes();
            UpdateUserInterface();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Internal method adding points without GUI update
        /// </summary>
        /// <param name="p">The point with its userspace xy coordinates</param>
        /// <param name="channelNumber">The number of the channel to which to add the point</param>
        private void AddPointInternal(Point p, int channelNumber)
        {
            //Transform the given values into the absolute canvas coordinate system
            Point absolute = TransformCoordinates(p);
            if (ConnectingLines)
            {
                //int nPoints = _channelPoints[channelNumber].Count;
                if (!_firstDrawDone[channelNumber])
                {//no previous point yet
                    DrawPointAbsolute(absolute, _fgChannels[channelNumber]);
                    _firstDrawDone[channelNumber] = true;
                }
                else
                {
                    Point previousPoint = TransformCoordinates(_previous[channelNumber]);
                    DrawLineAbsolute(previousPoint, absolute, _fgChannels[channelNumber]);
                }
            }
            else
                DrawPointAbsolute(absolute, _fgChannels[channelNumber]);
            _previous[channelNumber] = p;
            //_channelPoints[channelNumber].Add(p);
        }

        /// <summary>
        /// Adds a new point to a given channel and immediately draws it
        /// </summary>
        /// <param name="p">The new point in userspace xy value coordinates</param>
        /// <param name="channelNumber">The number of the channel to which to add the point</param>
        public void AddPoint(Point p, int channelNumber)
        {
            if (channelNumber < 0 || channelNumber >= NumberOfChannels)
            {
                throw new ArgumentOutOfRangeException("Specified channel does not exist");
            }
            if (_stop.WaitOne(0))
                return;
            AddPointInternal(p, channelNumber);
            UpdateUserInterface();
        }

        /// <summary>
        /// Adds points to consecutive channels starting from channel0 - only initiates one GUI update
        /// </summary>
        /// <param name="p">Array of points to be added to consecutive channels</param>
        public void AddPoints(Point[] p)
        {
            if (p.Length > NumberOfChannels)
                throw new ArgumentException("Can't add more points than there are available channels");
            if (_stop.WaitOne(0))
                return;
            for (int i = 0; i < p.Length; i++)
            {
                AddPointInternal(p[i], i);
            }
            UpdateUserInterface();
        }

        /// <summary>
        /// Draw the coordinate system axes
        /// </summary>
        protected void DrawAxes()
        {
            //The graph space is from 10% to 90% of the canvas in both directions
            //The axes border that space
            Point start = new Point();
            Point end = new Point();
            start.X = _xLimAbsolute.Min;
            end.X = _xLimAbsolute.Max;
            start.Y = _yLimAbsolute.Max;
            end.Y = _yLimAbsolute.Max;
            DrawLineAbsolute(start, end, _axesColor);
            //Draw Y-axis
            start.X = _xLimAbsolute.Min;
            end.X = _xLimAbsolute.Min;
            start.Y = _yLimAbsolute.Min;
            end.Y = _yLimAbsolute.Max;
            DrawLineAbsolute(start, end, _axesColor);
            //Draw Tickmarks - for both axes we draw one mark for every 10% of the (max-min)
            //Tickmarks go from +/-1% of graph space around the axes
            double deltaX = (_xLimAbsolute.Max - _xLimAbsolute.Min) / 10;
            for (double x = _xLimAbsolute.Min; x <= _xLimAbsolute.Max; x += deltaX)
            {
                start.X = x;
                end.X = x;
                start.Y = _yLimAbsolute.Max - Width / 100;
                end.Y = _yLimAbsolute.Max + Width / 100;
                DrawLineAbsolute(start, end, _axesColor);
            }
            double deltaY = (_yLimAbsolute.Max - _yLimAbsolute.Min) / 10;
            for (double y = _yLimAbsolute.Max; y >= _yLimAbsolute.Min; y -= deltaY)
            {
                start.X = _xLimAbsolute.Min - Height / 100;
                end.X = _xLimAbsolute.Min + Height / 100;
                start.Y = y;
                end.Y = y;
                DrawLineAbsolute(start, end, _axesColor);
            }
            
        }

        /// <summary>
        /// Transforms user xy space to the absolute canvas space
        /// </summary>
        /// <param name="xyValues">The point in xy user coordinates to transform</param>
        /// <returns>The corresponding point in canvas coordinates</returns>
        protected Point TransformCoordinates(Point xyValues)
        {           
            //We use the absolute canvas boundaries to transform the coordinates
            //for user coordinates points are expected to be higher if their yvalue is higher
            //this is opposite than for canvas coordinates
            double xLengthAbs = _xLimAbsolute.Max - _xLimAbsolute.Min;
            double yLengthAbs = _yLimAbsolute.Max - _yLimAbsolute.Min;
            double xLength = _xLim.Max - _xLim.Min;
            double yLength = _yLim.Max - _yLim.Min;
            Point retval = new Point();
            retval.X = _xLimAbsolute.Min + (xyValues.X - _xLim.Min) / xLength * xLengthAbs;
            retval.Y = _yLimAbsolute.Max - (xyValues.Y - _yLim.Min) / yLength * yLengthAbs;//minus since increasing y should go upwards not downwards
            return retval;
        }

        /// <summary>
        /// Resets the drawing area to blank
        /// </summary>
        public void Reset()
        {
            Fill(_bgColor);
            DrawAxes();
            for(int i=0;i<_fgChannels.Length;i++)
                _firstDrawDone[i] = false;
            UpdateUserInterface();
        }


        #endregion //Methods


    }
}
