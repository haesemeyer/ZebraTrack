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
    /// Describes a blob in an image (analogous to the structures returned by regionprops in MATLAB)
    /// together with the blob's moments
    /// </summary>
    public unsafe sealed class BlobWithMoments : IComparable<BlobWithMoments>
    {

        #region Fields

        /// <summary>
        /// The upper-left corner of the bounding box
        /// </summary>
        IppiPoint _boundingBoxUpperLeft = new IppiPoint();

        /// <summary>
        /// The lower-right corner of the bounding box
        /// </summary>
        IppiPoint _boundingBoxLowerRight = new IppiPoint();

        /// <summary>
        /// The score of the blob given by a tracking algorithm
        /// </summary>
        uint _score;

        #endregion

        #region Properties

        /// <summary>
        /// The 0 order moment == Area of the blob
        /// </summary>
        public long Moment00 { get; private set; }       

        /// <summary>
        /// The 1st order x=1,y=0 moment of the blob
        /// </summary>
        public long Moment10 { get; private set; }

        /// <summary>
        /// The 1st order x=0,y=1 moment of the blob
        /// </summary>
        public long Moment01 { get; private set; }

        /// <summary>
        /// The 2nd order x=2,y=0 moment of the blob
        /// </summary>
        public long Moment20 { get; private set; }

        /// <summary>
        /// The 2nd order x=1,y=1 moment of the blob
        /// </summary>
        public long Moment11 { get; private set; }

        /// <summary>
        /// The 2nd order x=0,y=2 moment of the blob
        /// </summary>
        public long Moment02 { get; private set; }

        /// <summary>
        /// The 3rd order x=0,y=3 moment of the blob
        /// </summary>
        public long Moment03 { get; private set; }

        /// <summary>
        /// The 3rd order x=3,y=0 moment of the blob
        /// </summary>
        public long Moment30 { get; private set; }

        /// <summary>
        /// The 3rd order x=2,y=1 moment of the blob
        /// </summary>
        public long Moment21 { get; private set; }

        /// <summary>
        /// The 3rd order x=1, y=2 moment of the blob
        /// </summary>
        public long Moment12 { get; private set; }

        public double Central20
        {
            get
            {
                return Moment20 - xc * Moment10;
            }
        }

        public double Central02
        {
            get
            {
                return Moment02 - yc * Moment01;
            }
        }

        public double Central11
        {
            get
            {
                return Moment11 - xc * Moment01;
            }
        }

        public double Central30
        {
            get
            {
                return Moment30 - 3 * xc * Moment20 + 2 * xc * xc * Moment10;
            }
        }

        public double Central03
        {
            get
            {
                return Moment03 - 3 * yc * Moment02 + 2 * yc * yc * Moment01;
            }
        }

        public double Central21
        {
            get
            {
                return Moment21 - 2 * xc * Moment11 - yc * Moment20 + 2 * xc * xc * Moment01;
            }
        }

        public double Central12
        {
            get
            {
                return Moment12 - 2 * yc * Moment11 - xc * Moment02 + 2 * yc * yc * Moment10;
            }
        }
        /// <summary>
        /// The bounding box containing the blob
        /// </summary>
        public IppiRect BoundingBox
        {
            get
            {
                return new IppiRect(_boundingBoxUpperLeft.x, _boundingBoxUpperLeft.y, _boundingBoxLowerRight.x - _boundingBoxUpperLeft.x+1, _boundingBoxLowerRight.y - _boundingBoxUpperLeft.y+1);
            }
        }

        /// <summary>
        /// The top left corner of the bounding box
        /// </summary>
        public IppiPoint BB_TopLeft
        {
            get
            {
                return _boundingBoxUpperLeft;
            }
        }

        /// <summary>
        /// The bottom right corner of the bounding box
        /// </summary>
        public IppiPoint BB_BottomRight
        {
            get
            {
                return _boundingBoxLowerRight;
            }
        }

        public IppiSize BB_Size
        {
            get
            {
                return new IppiSize(_boundingBoxLowerRight.x - _boundingBoxUpperLeft.x + 1, _boundingBoxLowerRight.y - _boundingBoxUpperLeft.y + 1);
            }
        }

        /// <summary>
        /// The area of the blob, ie the number of pixels
        /// </summary>
        public long Area
        {
            get
            {
                return Moment00;
            }
        }

        /// <summary>
        /// The centroid of the blob
        /// </summary>
        public IppiPoint Centroid
        {
            get
            {
                if (Moment00 != 0)
                    return new IppiPoint((int)xc, (int)yc);
                else
                    return new IppiPoint(-1,-1);
            }
        }

        /// <summary>
        /// The x-coordinate of the centroid
        /// </summary>
        public double xc
        {
            get
            {
                return (double)Moment10 / (double)Moment00;
            }
        }

        /// <summary>
        /// The y-coordinate of the centroid
        /// </summary>
        public double yc
        {
            get
            {
                return (double)Moment01 / (double)Moment00;
            }
        }

        /// <summary>
        /// The eccentricity of the blob (0=circle, 1=straight line)
        /// </summary>
        public double Eccentricity
        {
            get
            {
                double lambda1, lambda2, diff;
                double cov11, cov20, cov02;
                cov11 = Central11 / Moment00;
                cov20 = Central20 / Moment00;
                cov02 = Central02 / Moment00;
                diff = Math.Sqrt(4 * cov11 * cov11 + (cov20 - cov02) * (cov20 - cov02)) / 2;
                lambda1 = (cov20 + cov02) / 2 + diff;
                lambda2 = (cov20 + cov02) / 2 - diff;
                if (lambda2 < 0)
                    lambda2 = 0;
                if (lambda1 == 0)
                    return 0;//???
                else
                    return Math.Sqrt(1 - lambda2 / lambda1);
            }
        }

        /// <summary>
        /// The raw rotation angle of the major axis in radians
        /// </summary>
        public double Theta
        {
            get
            {
                //Normalize moments (covariance matrix of 2nd order moments)
                double norm20, norm11, norm02;
                norm20 = Central20 / Moment00;
                norm11 = Central11 / Moment00;
                norm02 = Central02 / Moment00;
                double diff = norm20 - norm02;
                if (diff == 0)
                {
                    if (norm11 == 0)
                        return 0;
                    else if (norm11 > 0)
                        return Math.PI / 4;
                    else //norm11<0
                        return -Math.PI / 4;
                }
                else if (diff > 0)
                {
                    if (norm11 == 0)
                        return 0;
                    else
                        return 0.5 * (Math.Atan((2 * norm11) / (norm20 - norm02)));
                }
                else
                {
                    if (norm11 == 0)
                        return 0;
                    else if (norm11 > 0)
                        return 0.5 * (Math.Atan((2 * norm11) / (norm20 - norm02))) + Math.PI / 2;
                    else
                        return 0.5 * (Math.Atan((2 * norm11) / (norm20 - norm02))) - Math.PI / 2;
                }
            }
        }

        /// <summary>
        /// The heading angle in radians
        /// </summary>
        public double Angle
        {
            get
            {
                //most angles can be solved by looking at cm12, however there are
                //"problem areas" around theta=0 and (for more difficult images) theta=-90
                //cm30 can be used to solve ambiguities for -20<=theta<=20 as it is
                //strongly negative for real angles around 90 but strongly positive for
                //real angels around 270
                //cm03 can be used to solve amiguities for 80<=theta and theta<=-80 as
                //cm03 is strongly positive for real angles around 0 but strongly
                //negative for real angles around 180 - on top of that we than have to
                //correct for were we actually are regarding theta - before or after
                //the "-90 to +90 jump"
                double th = Theta;
                if (-0.3491 <= th && th <= 0.3491)//problem area 1 -20<=th<=20
                {
                    if (Central30 < 0)
                        return th + Math.PI / 2;
                    else
                        return th + 3 * Math.PI / 2;
                }
                else if (1.3963 <= th || th <= -1.3963)//problem area 2
                {
                    if (Central03 < 0)
                    {
                        if (th < 0)
                            return th + 3 * Math.PI / 2;
                        else
                            return th + Math.PI / 2;
                    }
                    else
                    {
                        if (th < 0)
                            return th + Math.PI / 2;
                        else
                            return th + 3 * Math.PI / 2;
                    }
                }
                else//not a problem area
                {
                    if (Central12 < 0)
                        return th + Math.PI / 2;
                    else
                        return th + 3 * Math.PI / 2;
                }
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

        public string MomentString
        {
            get
            {
                return string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}", Moment00, Moment10, Moment01, Moment20, Moment11, Moment02, Moment30, Moment03, Moment21, Moment12);
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a new empty blob object
        /// </summary>
        public BlobWithMoments()
        {
            Moment00 = 0;
            Moment01 = 0;
            Moment10 = 0;
            Moment20 = 0;
            Moment11 = 0;
            Moment02 = 0;
            Moment30 = 0;
            Moment03 = 0;
            Moment21 = 0;
            Moment12 = 0;
        }

        /// <summary>
        /// Constructs a blob object with pre-computed moments such as obtained by ipp
        /// </summary>
        /// <param name="m00">The zero's order moment (area)</param>        
        /// <param name="m10">The x=1 y=0 first order moment</param>
        /// <param name="m01">The x=0 y=1 first order moment</param>
        /// <param name="m20">The x=2 y=0 second order moment</param>
        /// <param name="m11">The x=1 y=1 second order moment</param>
        /// <param name="m02">The x=0 y=2 second order moment</param>
        /// <param name="m30">The x=3 y=0 third order moment</param>
        /// <param name="m03">The x=0 y=3 third order moment</param>
        public BlobWithMoments(long m00, long m10, long m01, long m20, long m11, long m02, long m30, long m03, long m21, long m12)
        {
            Moment00 = m00;           
            Moment10 = m10;
            Moment01 = m01;
            Moment20 = m20;
            Moment11 = m11;
            Moment02 = m02;
            Moment30 = m30;
            Moment03 = m03;
            Moment21 = m21;
            Moment12 = m12;
        }

        /// <summary>
        /// Updates the values of all moments stored in the blob
        /// </summary>
        /// <param name="m00">The zero's order moment (area)</param>        
        /// <param name="m10">The x=1 y=0 first order moment</param>
        /// <param name="m01">The x=0 y=1 first order moment</param>
        /// <param name="m20">The x=2 y=0 second order moment</param>
        /// <param name="m11">The x=1 y=1 second order moment</param>
        /// <param name="m02">The x=0 y=2 second order moment</param>
        /// <param name="m30">The x=3 y=0 third order moment</param>
        /// <param name="m03">The x=0 y=3 third order moment</param>
        public void UpdateBlob(long m00, long m10, long m01, long m20, long m11, long m02, long m30, long m03, long m21, long m12)
        {
            Moment00 = m00;
            Moment10 = m10;
            Moment01 = m01;
            Moment20 = m20;
            Moment11 = m11;
            Moment02 = m02;
            Moment30 = m30;
            Moment03 = m03;
            Moment21 = m21;
            Moment12 = m12;
        }

        #endregion

        /// <summary>
        /// Sets all moment values of the blob back to their default value 0
        /// </summary>
        public void ResetBlob()
        {
            Moment00 = 0;
            Moment01 = 0;
            Moment10 = 0;
            Moment20 = 0;
            Moment11 = 0;
            Moment02 = 0;
            Moment30 = 0;
            Moment03 = 0;
            Moment21 = 0;
            Moment12 = 0;
        }

        /// <summary>
        /// Updates the bounding box without requiring IppiRect
        /// </summary>
        /// <param name="x">The top-left x coordinate</param>
        /// <param name="y">The top-left y coordinate</param>
        /// <param name="width">The width</param>
        /// <param name="height">The height</param>
        public void UpdateBoundingBox(int x, int y, int width, int height)
        {
            _boundingBoxUpperLeft.x = x;
            _boundingBoxUpperLeft.y = y;
            _boundingBoxLowerRight.x = x + width - 1;
            _boundingBoxLowerRight.y = y + height - 1;
        }

        /// <summary>
        /// Adds a new pixel to the blob
        /// </summary>
        /// <param name="x">The x-coordinate of the pixel</param>
        /// <param name="y">The y-coordinate of the pixel</param>
        /// <param name="value">The intensity value of the pixel</param>
        public void AddPixel(int x, int y){
            //Update bounding points of bounding box and values important for contrast calculation
            if (Moment00 < 1)
            {
                //we are adding the first element
                _boundingBoxUpperLeft = new IppiPoint(x, y);
                _boundingBoxLowerRight = new IppiPoint(x, y);
            }
            else
            {
                _boundingBoxUpperLeft.x = Math.Min(_boundingBoxUpperLeft.x, x);
                _boundingBoxLowerRight.x = Math.Max(_boundingBoxLowerRight.x, x);
                _boundingBoxUpperLeft.y = Math.Min(_boundingBoxUpperLeft.y, y);
                _boundingBoxLowerRight.y = Math.Max(_boundingBoxLowerRight.y, y);
            }
            //Update moments
            Moment00++;
            Moment10 += x;
            Moment01 += y;
            Moment20 += x * x;
            Moment11 += x * y;
            Moment02 += y * y;
            Moment30 += x * x * x;
            Moment03 += y * y * y;
            Moment21 += x * x * y;
            Moment12 += x * y * y;
        }

        /// <summary>
        /// Extract blobs from an image in which distinct contiguous regions are marked
        /// with distinct pixel values such as returned by ippiLabelMarkers
        /// </summary>
        /// <param name="imMarkers">The marker image</param>
        /// <param name="nMarkers">The number of expected markers</param>
        /// <param name="imOriginal">The original image. Used for pixel-intensity information</param>
        /// <param name="roi">The roi in which to perform blob extraction</param>
        /// <returns>An array of discovered blobs</returns>
        public static BlobWithMoments[] ExtractBlobs(Image8 imMarkers, int nMarkers, IppiROI roi)
        {
            if (nMarkers == 0)//no point to iterate over the image if no marker was found
                return null;
            BlobWithMoments[] blobs = new BlobWithMoments[nMarkers];
            byte markerValue;
            //loop over ROI in marker image, adding pixels to their appropriate blob
            for (int i = roi.Y; i < roi.Y + roi.Height; i++)
            {
                for (int j = roi.X; j < roi.X + roi.Width; j++)
                {
                    markerValue = imMarkers.Image[i * imMarkers.Stride + j];
                    if (markerValue != 0) //it belongs to a blob!
                    {
                        //since the lowest marker has the value 1 we need to deduct 1 from markerValue for 0-index based array mapping!
                        if (blobs[markerValue - 1] == null)
                            blobs[markerValue - 1] = new BlobWithMoments();
                        blobs[markerValue - 1].AddPixel(j, i);
                    }
                }
            }
            return blobs;
        }

        #region IComparableT

        /// <summary>
        /// Compares to blob objects according to their score - lower scores preceding higher scores
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(BlobWithMoments otherBlob)
        {
            //We precede null
            if (otherBlob == null)
                return -1;
            else
            {
                return this.Score.CompareTo(otherBlob.Score);
            }
        }

        #endregion
    }
}
