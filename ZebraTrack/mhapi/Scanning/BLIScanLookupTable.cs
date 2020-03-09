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
using System.IO;

using ipp;

using MHApi.Imaging;

namespace MHApi.Scanning
{
    /// <summary>
    /// Provides functionality for a fine-grained coordinate-voltage lookup table
    /// based on bilinear interpolation
    /// </summary>
    public class BLIScanLookupTable : PointVoltageConverter
    {
        #region Fields

        /// <summary>
        /// A 2D table of x-voltages corresponding
        /// to a given (x,y) coordinate
        /// </summary>
        float[] _xVolts;

        /// <summary>
        /// A 2D table of y-voltages corresponding
        /// to a given (x,y) coordinate
        /// </summary>
        float[] _yVolts;

        /// <summary>
        /// A 2D table of complete precomputed x-voltages
        /// corresponding to given (x,y) coordinates
        /// </summary>
        float[] _preCompX;

        /// <summary>
        /// A 2D table of complete precomputed y-voltages
        /// corresponding to given (x,y) coordinates
        /// </summary>
        float[] _preCompY;

        /// <summary>
        /// The pixel-spacing (x/y symmetric)
        /// in the lookup table
        /// </summary>
        int _spacing;

        /// <summary>
        /// The ROI stored in the table for scanning
        /// </summary>
        IppiROI _scanRoi;

        /*
        /// <summary>
        /// The width in pixels of the border excluded
        /// from the lookup table at all image borders
        /// </summary>
        int _border;

        /// <summary>
        /// The width of the image contained in the lookup table
        /// </summary>
        int _width;

        /// <summary>
        /// The height of the image contained in the lookup table
        /// </summary>
        int _height;*/

        /// <summary>
        /// The number of columns in the voltage table
        /// </summary>
        int _c;

        /// <summary>
        /// The number of rows in the voltage table
        /// </summary>
        int _r;

        /// <summary>
        /// Indicates whether the table has been completely filled or not
        /// </summary>
        bool _complete;

        /// <summary>
        /// For a non-complete table gives us the index
        /// of the element to add next to the table
        /// </summary>
        int _addIndex;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new empty BLIScanLookupTable
        /// </summary>
        /// <param name="scanRoi">The image ROI in which to compute coordinates</param>
        /// <param name="spacing">The spacing between the interpolation anchors stored in the table</param>
        public BLIScanLookupTable(IppiROI scanRoi, int spacing=8)
        {
            //validate dimensions in relation to spacing
            if (scanRoi.Width % spacing != 0)
                throw new ArgumentException("The ROI width has to be dividable by the spacing");
            if (scanRoi.Height % spacing != 0)
                throw new ArgumentException("The ROI height has to be dividable by the spacing");
            _spacing = spacing;
            //initialize arrays
            _c = (int)Math.Floor((float)scanRoi.Width / _spacing) + 1;
            _r = (int)Math.Floor((float)scanRoi.Height / spacing) + 1;
            _xVolts = new float[_c * _r];
            _yVolts = new float[_c * _r];
            _addIndex = -1;
            _scanRoi = scanRoi;
        }

        /// <summary>
        /// Creates a new BLIScanLookup Table which is prefilled
        /// with previously obtained point-voltage relationships
        /// </summary>
        /// <param name="xVolts">The 2D table of x-Voltages</param>
        /// <param name="yVolts">The 2D table of y-Voltages</param>
        /// <param name="scanRoi">The image ROI of the coordinates</param>
        /// <param name="spacing">The spacing between the interpolation anchors stored in the table</param>
        public BLIScanLookupTable(float[] xVolts, float[] yVolts, IppiROI scanRoi, int spacing = 8)
        {
            if (xVolts.Length != yVolts.Length)
                throw new ArgumentException("The two voltage lookup tables must have the same number of elements");
            _c = (int)Math.Floor((float)scanRoi.Width / spacing) + 1;
            _r = (int)Math.Floor((float)scanRoi.Height / spacing) + 1;
            if (xVolts.Length != _c * _r)
                throw new ArgumentException("The supplied voltage tables don't match with the image/border/spacing dimensions");
            _spacing = spacing;
            _xVolts = xVolts;
            _yVolts = yVolts;
            _complete = true;
            _addIndex = _c * _r;
            _scanRoi = scanRoi;
        }


        /*
        /// <summary>
        /// Creates a new empty BLIScanLookupTable
        /// </summary>
        /// <param name="imageWidth">The width of the image to cover</param>
        /// <param name="imageHeight">The height of the image to cover</param>
        /// <param name="spacing">The spacing between the interpolation anchors stored in the table</param>
        /// <param name="border">The width of the border on all image sides not covered by the table</param>
        public BLIScanLookupTable(int imageWidth, int imageHeight, int spacing=10, int border=10)
        {
            _spacing = spacing;
            _border = border;
            _width = imageWidth;
            _height = imageHeight;
            //validate dimensions
            if ((_width - 2 * _border) % _spacing != 0)
                throw new ArgumentException("The space between the borders has to be dividable by the spacing");
            if ((_height - 2 * _border) % _spacing != 0)
                throw new ArgumentException("The y space between the borders has to be dividable by the spacing");
            //initialize arrays
            _c = (int)Math.Floor((float)(imageWidth - 2 * border) / (float)spacing)+1;
            _r = (int)Math.Floor((float)(imageHeight - 2 * border) / (float)spacing) + 1;
            _xVolts = new float[_c * _r];
            _yVolts = new float[_c * _r];
            _addIndex = -1;
        }

        /// <summary>
        /// Creates a new BLIScanLookup Table which is prefilled with previously
        /// obtained anchor-point voltage relationships
        /// </summary>
        /// <param name="xVolts">The 2D table of x-voltages</param>
        /// <param name="yVolts">The 2D table of y-voltages</param>
        /// <param name="imageWidth">The width of the image to cover</param>
        /// <param name="imageHeight">The height of the image to cover</param>
        /// <param name="spacing">The spacing between the interpolation anchors stored in the table</param>
        /// <param name="border">The width of the border on all image sides not covered by the table</param>
        public BLIScanLookupTable(float[] xVolts, float[] yVolts, int imageWidth, int imageHeight, int spacing, int border)
        {
            if (xVolts.Length != yVolts.Length)
                throw new ArgumentException("The two voltage lookup tables must have the same number of elements");
            _c = (int)Math.Floor((double)(imageWidth - 2 * border) / (double)spacing) + 1;
            _r = (int)Math.Floor((double)(imageHeight - 2 * border) / (double)spacing) + 1;
            if (xVolts.Length != _c * _r)
                throw new ArgumentException("The supplied voltage tables don't match with the image/border/spacing dimensions");
            _spacing = spacing;
            _border = border;
            _width = imageWidth;
            _height = imageHeight;
            _xVolts = xVolts;
            _yVolts = yVolts;
            _complete = true;
            _addIndex = _c * _r;
        }*/

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether the table is completed or not
        /// </summary>
        public override bool Complete
        {
            get
            {
                return _complete;
            }
            protected set
            {
                _complete = value;
            }
        }

        /// <summary>
        /// Indicates whether a full table has been precomputed for
        /// fast lookup
        /// </summary>
        public bool Precomputed { get; private set; }

        /*
        /// <summary>
        /// The width of the border within which we calibrated
        /// </summary>
        public int Border
        {
            get
            {
                return _border;
            }
        }
        */
        

        /// <summary>
        /// For a given point returns the corresponding
        /// x and y voltages
        /// </summary>
        /// <param name="p">The coordinate to look up</param>
        /// <returns>A 2 element array containing the x-voltage and y-voltage</returns>
        public override IppiPoint_32f this[IppiPoint p]
        {
            get
            {
                if (!_complete)
                {
                    throw new InvalidOperationException("Tried to use non-complete calibration table");
                }
                IppiPoint_32f retval = new IppiPoint_32f();
                if (p.x < _scanRoi.X || p.x >= (_scanRoi.Width + _scanRoi.X))
                    throw new ArgumentOutOfRangeException("X coordinate outside lookup table area");
                if (p.y < _scanRoi.Y || p.y >= (_scanRoi.Height + _scanRoi.Y))
                    throw new ArgumentOutOfRangeException("Y coordinate outside lookup table area");
                if (Precomputed)
                {
                    //our precomputed lookup table has a size equal to the ROI size
                    //therefore we need to deduct the ROI start from the point
                    //coordinates
                    p.x = p.x - _scanRoi.X;
                    p.y = p.y - _scanRoi.Y;
                    retval.x = _preCompX[p.y * _scanRoi.Width + p.x];
                    retval.y = _preCompY[p.y * _scanRoi.Width + p.x];
                    return retval;
                }
                int xIn, yIn;
                //correct for the border presence
                xIn = p.x - _scanRoi.X;
                yIn = p.y - _scanRoi.Y;
                //find table coordinates of the closest points with LOWER coordinates
                //then the other borders will be +1 from that
                int xTabLeft, yTabTop, xTabRight, yTabBottom;
                xTabLeft = xIn / _spacing;
                yTabTop = yIn / _spacing;
                xTabRight = xTabLeft + 1;
                yTabBottom = yTabTop + 1;
                if (xTabLeft * _spacing == xIn && yTabTop * _spacing == yIn)
                {
                    //we don't need to interpolate
                    retval.x = _xVolts[yTabTop * _c + xTabLeft];
                    retval.y = _yVolts[yTabTop * _c + xTabLeft];
                    return retval;
                }
                else if (xTabLeft * _spacing == xIn)
                {
                    //we only need to interpolate along y
                    int ydist = yIn - yTabTop * _spacing;
                    //the larger the distance, the more we weigh in the value from the bottom point
                    float bottomFrac = (float)ydist / (float)_spacing;
                    retval.x = _xVolts[yTabTop * _c + xTabLeft] * (1 - bottomFrac) + _xVolts[yTabBottom * _c + xTabLeft] * bottomFrac;
                    retval.y = _yVolts[yTabTop * _c + xTabLeft] * (1 - bottomFrac) + _yVolts[yTabBottom * _c + xTabLeft] * bottomFrac;
                    return retval;
                }
                else if (yTabTop * _spacing == yIn)
                {
                    //we only need to interpolate along x
                    int xdist = xIn - xTabLeft * _spacing;
                    //the larger the distance the more we weigh in the value from the right point
                    float rightFrac = (float)xdist / (float)_spacing;
                    retval.x = _xVolts[yTabTop * _c + xTabLeft] * (1 - rightFrac) + _xVolts[yTabTop * _c + xTabRight] * rightFrac;
                    retval.y = _yVolts[yTabTop * _c + xTabLeft] * (1 - rightFrac) + _yVolts[yTabTop * _c + xTabRight] * rightFrac;
                    return retval;
                }
                else
                {
                    //bilinear interpolation
                    float[] top, bottom;
                    top = new float[2];
                    bottom = new float[2];
                    int xdist = xIn - xTabLeft * _spacing;
                    //the larger the distance the more we weigh in the value from the right point
                    float rightFrac = (float)xdist / (float)_spacing;
                    int ydist = yIn - yTabTop * _spacing;
                    //the larger the distance, the more we weigh in the value from the bottom point
                    float bottomFrac = (float)ydist / (float)_spacing;
                    //first we interpolate along x for both top and bottom
                    top[0] = _xVolts[yTabTop * _c + xTabLeft] * (1 - rightFrac) + _xVolts[yTabTop * _c + xTabRight] * rightFrac;
                    top[1] = _yVolts[yTabTop * _c + xTabLeft] * (1 - rightFrac) + _yVolts[yTabTop * _c + xTabRight] * rightFrac;
                    bottom[0] = _xVolts[yTabBottom * _c + xTabLeft] * (1 - rightFrac) + _xVolts[yTabBottom * _c + xTabRight] * rightFrac;
                    bottom[1] = _yVolts[yTabBottom * _c + xTabLeft] * (1 - rightFrac) + _yVolts[yTabBottom * _c + xTabRight] * rightFrac;
                    //now we interpolate between top and bottom
                    retval.x = top[0] * (1 - bottomFrac) + bottom[0] * bottomFrac;
                    retval.y = top[1] * (1 - bottomFrac) + bottom[1] * bottomFrac;
                    return retval;
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Incidates whether a given point can be analyzed
        /// </summary>
        /// <param name="p">The point in questions</param>
        /// <returns>Returns true if the point is covered by the calibration list</returns>
        public bool PointValid(IppiPoint p)
        {
            return (p.x >= _scanRoi.X && p.x < (_scanRoi.X+_scanRoi.Width) && p.y >= _scanRoi.Y && p.y < (_scanRoi.Height+_scanRoi.Y));
        }

        /// <summary>
        /// Adds a new point to the calibration table, verifies that it has the expected coordinates
        /// and returns true until the table is complete 
        /// </summary>
        /// <param name="calibrationPoint">The new calibration point to add to the table</param>
        /// <param name="nextPoint">The point expected next in the calibration</param>
        /// <returns>True while still expecting new points</returns>
        /// <exception cref="ArgumentExcpetion">Thrown if the wrong calibration point is provided</exception>
        public bool AddNext(PointVoltagePair calibrationPoint, out IppiPoint nextPoint)
        {
            int x, y;
            //Verify new point
            if (_addIndex > -1 && _addIndex < _xVolts.Length)
            {
                x = _addIndex % _c;
                x = x * _spacing + _scanRoi.X;
                y = (_addIndex - _addIndex % _c) / _c;
                y = y * _spacing + _scanRoi.Y;
                if (calibrationPoint.Coordinate.x != x || calibrationPoint.Coordinate.y != y)
                    throw new ArgumentException("Wrong calibration point provided");
            }

            return ForceAddNext(calibrationPoint, out nextPoint);
        }

        /// <summary>
        /// Adds a new point to the calibration table without checking its validity. Returns
        /// true until the table is complete and provides the next point to target
        /// </summary>
        /// <param name="calibrationPoint">The new calibration point to add to the table</param>
        /// <param name="nextPoint">The point expected next in the calibration</param>
        /// <returns>True until the table is complete</returns>
        public bool ForceAddNext(PointVoltagePair calibrationPoint, out IppiPoint nextPoint)
        {
            if (_addIndex >= _xVolts.Length)
            {
                System.Diagnostics.Debug.WriteLine("Tried to add new point to already complete calibration table");
                nextPoint = new IppiPoint(-1, -1);
                return false;
            }

            int x, y;

            if (_addIndex == -1)
            {
                _addIndex++;
                //Compute next point
                x = _addIndex % _c;
                x = x * _spacing + _scanRoi.X;
                y = (_addIndex - _addIndex % _c) / _c;
                y = y * _spacing + _scanRoi.Y;
                nextPoint = new IppiPoint(x, y);
                return true;
            }



            //Add point to table
            _xVolts[_addIndex] = calibrationPoint.XVoltage;
            _yVolts[_addIndex] = calibrationPoint.YVoltage;

            _addIndex++;
            if (_addIndex >= _xVolts.Length)
            {
                nextPoint = new IppiPoint(-1, -1);
                _complete = true;
                return false;
            }
            else
            {
                //Compute next point
                x = _addIndex % _c;
                x = x * _spacing + _scanRoi.X;
                y = (_addIndex - _addIndex % _c) / _c;
                y = y * _spacing + _scanRoi.Y;
                nextPoint = new IppiPoint(x, y);
                return true;
            }
        }

        public void Precompute()
        {
            if (!Complete)
                throw new InvalidOperationException("Can't precompute lookup table before completing calibration");
            if (Precomputed)
                return;
            //create new tables
            _preCompX = new float[_scanRoi.Width * _scanRoi.Height];
            _preCompY = new float[_scanRoi.Width * _scanRoi.Height];
            //iterate over each coordinate within the image borders
            //and precompute it's voltages, storing it in the table
            for (int x = 0; x < _scanRoi.Width; x++)
            {
                for (int y = 0; y < _scanRoi.Height; y++)
                {
                    IppiPoint_32f voltages = this[new IppiPoint(x+_scanRoi.X, y+_scanRoi.Y)];
                    _preCompX[y * _scanRoi.Width + x] = voltages.x;
                    _preCompY[y * _scanRoi.Width + x] = voltages.y;
                }
            }
            Precomputed = true;
        }

        public void Clear()
        {
            _complete = false;
            _addIndex = -1;
            Precomputed = false;
        }

        /// <summary>
        /// Save the interpolation lookup table to file
        /// using the supplied filestream
        /// </summary>
        /// <param name="fileWriter">The textfile to write into</param>
        public void SaveToFile(System.IO.TextWriter fileWriter)
        {
            fileWriter.WriteLine("InterpolationLookupTable");
            fileWriter.WriteLine("Spacing");
            fileWriter.WriteLine(_spacing);
            fileWriter.WriteLine("XStart");
            fileWriter.WriteLine(_scanRoi.X);
            fileWriter.WriteLine("YStart");
            fileWriter.WriteLine(_scanRoi.Y);
            fileWriter.WriteLine("ScanWidth");
            fileWriter.WriteLine(_scanRoi.Width);
            fileWriter.WriteLine("ScanHeight");
            fileWriter.WriteLine(_scanRoi.Height);
            fileWriter.WriteLine("XVoltages");
            foreach (float v in _xVolts)
                fileWriter.WriteLine(v);
            fileWriter.WriteLine("YVoltages");
            foreach (float v in _yVolts)
                fileWriter.WriteLine(v);
            fileWriter.WriteLine("EOF");
        }

        /// <summary>
        /// Loads an interpolation table from a provided text file
        /// </summary>
        /// <param name="fileReader">The textfile to load the table from</param>
        /// <returns>The new interpolation lookup table</returns>
        public static BLIScanLookupTable LoadFromFile(System.IO.TextReader fileReader)
        {
            float[] xvolts;
            float[] yvolts;
            int spacing, xStart, yStart, width, height;
            if (fileReader.ReadLine() != "InterpolationLookupTable")
                throw new IOException("The provided file does not contain an interpolation lookup table or is corrupted");
            try
            {
                fileReader.ReadLine();//"Spacing"
                spacing = int.Parse(fileReader.ReadLine());
                fileReader.ReadLine();//"XStart"
                xStart = int.Parse(fileReader.ReadLine());
                fileReader.ReadLine();//"YStart"
                yStart = int.Parse(fileReader.ReadLine());
                fileReader.ReadLine();//"ScanWidth"
                width = int.Parse(fileReader.ReadLine());
                fileReader.ReadLine();//"ScanHeight"
                height = int.Parse(fileReader.ReadLine());
                fileReader.ReadLine();//"XVoltages"
                int c = (int)Math.Floor((float)width / spacing) + 1;
                int r = (int)Math.Floor((float)height / spacing) + 1;
                xvolts = new float[c * r];
                for (int i = 0; i < c * r; i++)
                {
                    xvolts[i] = float.Parse(fileReader.ReadLine());
                }
                fileReader.ReadLine();//"YVoltages"
                yvolts = new float[c * r];
                for (int i = 0; i < c * r; i++)
                {
                    yvolts[i] = float.Parse(fileReader.ReadLine());
                }
                if(fileReader.ReadLine() != "EOF")
                    throw new IOException("The provided file does not contain an interpolation lookup table or is corrupted");
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                throw new IOException("The provided file does not contain an interpolation lookup table or is corrupted");
            }
            return new BLIScanLookupTable(xvolts, yvolts, new IppiROI(xStart, yStart, width, height), spacing);
        }

        #endregion
    }
}
