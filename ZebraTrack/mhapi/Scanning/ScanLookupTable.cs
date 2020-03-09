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

namespace MHApi.Scanning
{
    #region Classdef

    /// <summary>
    /// Represents a pair of coordinates and voltages
    /// </summary>
    public struct PointVoltagePair
    {
        /// <summary>
        /// The x/y coordinates of the point
        /// </summary>
        public IppiPoint Coordinate;

        /// <summary>
        /// The voltage corresponding to the x-coordinate
        /// </summary>
        public float XVoltage;

        /// <summary>
        /// The voltage corresponding to the y-coordinate
        /// </summary>
        public float YVoltage;

        public PointVoltagePair(IppiPoint coordinate, float xvoltage, float yvoltage)
        {
            Coordinate = coordinate;
            XVoltage = xvoltage;
            YVoltage = yvoltage;
        }
    }

    #endregion

    /// <summary>
    /// Provides functionality to create a lookup table for converting
    /// point coordinates into scan voltages after 3-point calibration
    /// </summary>
    public class ScanLookupTable : PointVoltageConverter
    {       

        /// <summary>
        /// The x-coordinate voltages in the lookup table
        /// </summary>
        float[] _xVolts;

        /// <summary>
        /// The y-coordinate voltages in the lookup table
        /// </summary>
        float[] _yVolts;

        int _imageWidth;

        int _imageHeight;

        public override bool Complete { get; protected set; }

        /// <summary>
        /// The calculated distance (in "pixels") between X mirror and dish
        /// </summary>
        public double XDistance { get; private set; }

        /// <summary>
        /// The calculated distance (in "pixels") between Y mirror and dish
        /// </summary>
        public double YDistance { get; private set; }

        /// <summary>
        /// Creates a new ScanLookupTable for images of the specified dimensions
        /// </summary>
        /// <param name="imageWidth">The width of the image</param>
        /// <param name="imageHeight">The height of the image</param>
        public ScanLookupTable(int imageWidth, int imageHeight)
        {
            _xVolts = new float[imageWidth];
            _yVolts = new float[imageHeight];
            _imageHeight = imageHeight;
            _imageWidth = imageWidth;
            XDistance = 0;
            Complete = false;
        }

        /// <summary>
        /// Three-point calibration of coordinate-voltage mapping. Using the origin at 0V/0V and two points
        /// for averaging purposes
        /// </summary>
        /// <param name="origin">The coordinates of the origin at 0V/0V</param>
        /// <param name="p1">First calibration point</param>
        /// <param name="p2">Second calibration point</param>
        public void Calibrate(IppiPoint origin, PointVoltagePair p1, PointVoltagePair p2)
        {
            //distances from plane to mirror calculated for each point
            double x1d, x2d, y1d, y2d;
            //distance from the point in question to the origin
            double pointdist;
            //Calculate first distance measure for x-mirror
            pointdist = p1.Coordinate.x - origin.x;
            x1d = pointdist / Math.Tan(4 * Math.PI * p1.XVoltage / 180);
            //Calculate second distance measure for x-mirror
            pointdist = p2.Coordinate.x - origin.x;
            x2d = pointdist / Math.Tan(4 * Math.PI * p2.XVoltage / 180);
            //Calculate first distance measure for y-mirror
            pointdist = p1.Coordinate.y - origin.y;
            y1d = pointdist / Math.Tan(4 * Math.PI * p1.YVoltage / 180);
            //Calculate second distance measure for y-mirror
            pointdist = p2.Coordinate.y - origin.y;
            y2d = pointdist / Math.Tan(4 * Math.PI * p2.YVoltage / 180);
            //Calculate distance for x-mirror
            XDistance = (x1d + x2d) / 2;
            //Calculate distance for y-mirror
            YDistance = (y1d + y2d) / 2;
            //Create x-lookup table
            for (int x = 0; x < _imageWidth; x++)
            {
                _xVolts[x] = (float)(Math.Atan((x - origin.x) / XDistance) * 45 / Math.PI);
            }
            //Create y-lookup table
            for (int y = 0; y < _imageHeight; y++)
            {
                _yVolts[y] = (float)(Math.Atan((y - origin.y) / YDistance) * 45 / Math.PI);
            }
            Complete = true;
        }

        /// <summary>
        /// Three point calibration for coordinate voltage mapping. Uses three arbitrary points to calculate the mapping.
        /// The three points are used to form two pairs for mapping
        /// </summary>
        /// <param name="p1">The first point and its corresponding voltage</param>
        /// <param name="p2">The second point and its corresponding voltage</param>
        /// <param name="p3">The third point and its corresponding voltage</param>
        public void Calibrate(PointVoltagePair p1, PointVoltagePair p2, PointVoltagePair p3)
        {
            
            //distance from the point in question to the first point
            double pointdist;
            

            //To build the lookup table we need the coordinates of the origin
            //to have a rectangular triangle reference
            //We try linear interpolation based on the points we got
            IppiPoint origin = new IppiPoint();
            pointdist = p2.Coordinate.x - p1.Coordinate.x;
            double distpervolts = pointdist / (p2.XVoltage - p1.XVoltage);
            origin.x = (int)(p1.Coordinate.x - p1.XVoltage * distpervolts);
            pointdist = p2.Coordinate.y - p1.Coordinate.y;
            distpervolts = pointdist / (p2.YVoltage - p1.YVoltage);
            origin.y = (int)(p1.Coordinate.y - p1.YVoltage * distpervolts);

            Calibrate(origin, p2, p3);
            return;
        }

        public override IppiPoint_32f this[IppiPoint p]
        {
            get
            {
                if (p.x >= _imageWidth || p.x < 0 || p.y >= _imageHeight || p.y < 0)
                    throw new ArgumentOutOfRangeException("Requested coordinate is outside of the calibrated image dimensions");
                if (!Complete)
                    throw new NotSupportedException("Can't obtain voltages before calibration");
                IppiPoint_32f retval = new IppiPoint_32f(_xVolts[p.x], _yVolts[p.y]);
                return retval;
            }
        }
    }
}
