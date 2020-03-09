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
using System.Drawing;

#if EmguCV

using Emgu.CV;
using Emgu.CV.Structure;

#endif

namespace MHApi.Imaging
{
    /// <summary>
    /// Describes a blob in an image (analogous to the structures returned by regionprops in MATLAB)
    /// </summary>
    [Obsolete("Consinder using BlobWithMoments instead.")]
    public sealed class Blob : IComparable
    {
        /// <summary>
        /// The collection of pixels making up the blob
        /// </summary>
        List<PointF> _pixels;

        /// <summary>
        /// Indicates if pre-calculated
        /// eccentricity are still valid
        /// </summary>
        bool _eccentricityValid;

        /// <summary>
        /// The precalculated eccentricity of the blob
        /// </summary>
        double _eccentricity;

        /// <summary>
        /// The intensity weighted sum of the x-coordinates
        /// </summary>
        int _weightedSumX;

        /// <summary>
        /// The intensity weighted sum of the y-coordinates
        /// </summary>
        int _weightedSumY;

        /// <summary>
        /// The total sum of all pixel intensity values
        /// </summary>
        int _sumIntensities;

        /// <summary>
        /// The upper-left corner of the bounding box
        /// </summary>
        Point _boundingBoxUpperLeft;

        /// <summary>
        /// The lower-right corner of the bounding box
        /// </summary>
        Point _boundingBoxLowerRight;

        /// <summary>
        /// The minimum pixel-intensity of the blob
        /// </summary>
        byte _minValue;

        /// <summary>
        /// The maximum pixel-intensity of the blob
        /// </summary>
        byte _maxValue;

        /// <summary>
        /// The score of the blob given by a tracking algorithm
        /// </summary>
        uint _score;

        /// <summary>
        /// The pixel-intensity weighted centroid of the blob
        /// </summary>
        public Point WeightedCentroid
        {
            get
            {
                return new Point(_weightedSumX / _sumIntensities, _weightedSumY / _sumIntensities);
            }
        }

        /// <summary>
        /// The bounding box containing the blob
        /// </summary>
        public Rectangle BoundingBox
        {
            get
            {
                return new Rectangle(_boundingBoxUpperLeft.X, _boundingBoxUpperLeft.Y, _boundingBoxLowerRight.X - _boundingBoxUpperLeft.X+1, _boundingBoxLowerRight.Y - _boundingBoxUpperLeft.Y+1);
            }
        }

        /// <summary>
        /// The contrast of the blobs image
        /// </summary>
        public double Contrast
        {
            get
            {
                if (_minValue == 0)
                {
                    return 1000;
                }
                return (double)_maxValue / (double)_minValue;
            }
        }

        /// <summary>
        /// The eccentricity of the blob
        /// </summary>
        public double Eccentricity
        {
            get
            {
#if EmguCV
                if (_eccentricityValid)
                    return _eccentricity;
                else // we have to recalculate the eccentricity
                {
                    Ellipse fit = PointCollection.EllipseLeastSquareFitting(_pixels.ToArray());
                    double a, b;//The half length of the long and short axis of the ellipse respectively
                    a = Math.Max(fit.MCvBox2D.size.Width, fit.MCvBox2D.size.Height) / 2;
                    b = Math.Min(fit.MCvBox2D.size.Height, fit.MCvBox2D.size.Width) / 2;
                    if (a != 0)
                        _eccentricity = Math.Sqrt((a * a - b * b) / (a * a));
                    else
                        _eccentricity = 0;
                    _eccentricityValid = true;
                    return _eccentricity;
                }
#else
                return 0;
#endif
            }
        }

        /// <summary>
        /// The area of the blob, ie the number of pixels
        /// </summary>
        public int Area
        {
            get
            {
                return _pixels.Count;
            }
        }

        /// <summary>
        /// The score of the blob given by a tracking algorithm
        /// </summary>
        public uint Score
        {
            get
            {
                return _score;
            }
            set
            {
                _score = value;
            }
        }

        /// <summary>
        /// Constructs a new empty blob object
        /// </summary>
        public Blob()
        {
            _pixels = new List<PointF>();
            _eccentricityValid = false;
        }

        /// <summary>
        /// Adds a new pixel to the blob
        /// </summary>
        /// <param name="x">The x-coordinate of the pixel</param>
        /// <param name="y">The y-coordinate of the pixel</param>
        /// <param name="value">The intensity value of the pixel</param>
        public void AddPixel(int x, int y, byte value){
            //Update values for weighted centroid calculation
            _sumIntensities += value;
            _weightedSumX += x * (int)value;
            _weightedSumY += y * (int)value;
            //Update bounding points of bounding box and values important for contrast calculation
            if (_pixels.Count < 1)
            {
                //we are adding the first element
                _boundingBoxUpperLeft = new Point(x, y);
                _boundingBoxLowerRight = new Point(x, y);
                _minValue = value;
                _maxValue = value;
            }
            else
            {
                _boundingBoxUpperLeft.X = Math.Min(_boundingBoxUpperLeft.X, x);
                _boundingBoxLowerRight.X = Math.Max(_boundingBoxLowerRight.X, x);
                _boundingBoxUpperLeft.Y = Math.Min(_boundingBoxUpperLeft.Y, y);
                _boundingBoxLowerRight.Y = Math.Max(_boundingBoxLowerRight.Y, y);
                _minValue = Math.Min(_minValue, value);
                _maxValue = Math.Max(_maxValue, value);
            }
            _pixels.Add(new PointF(x, y));
            _eccentricityValid = false; //new point has been added, descriptors need recacluation
        }

        #region IComparable

        /// <summary>
        /// Compares to blob objects according to their score - lower scores preceding higher scores
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            //We precede null
            if (obj == null)
                return -1;
            Blob otherBlob = obj as Blob;
            if (otherBlob == null)
                throw new ArgumentException("Object is not a blob");
            else
            {
                return this.Score.CompareTo(otherBlob.Score);
            }
        }

        #endregion
    }
}
