/*
Copyright 2017 Martin Haesemeyer
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
using System.Threading.Tasks;

using MHApi.Scanning;
using MHApi.Laser;
using ipp;
using MHApi.DrewsClasses;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MHApi.Imaging;
using System.Runtime.InteropServices;

namespace ZebraTrack.Experiments
{
    /// <summary>
    /// "Experimental" class to perform stimulus laser
    /// alignment to camera coordinates
    /// </summary>
    /// <remarks>
    /// Alignment will involve two steps:
    /// 1) Given know volts-angle transformations on mirrors determine height of setup h and camera coordinate system to
    ///    mirror coordinate system rotation theta (compute for both x and y and average - subtract pi/4 from y-theta for true theta)
    ///      NOTE: The camera likely also reflects the y-axis in relation to the x-axis which cannot be captured in a rotation
    ///      This can be detected by the fact theta computed for x vs. y is not the same but 180 degrees apart. In that case
    ///      theta should be set to x-theta and a flag should be set and y-voltages need to be sign-inverted!
    ///      This method also works if x is in fact reflected - since that case can be transformed into a y-reflection
    ///      using an appropriate rotation.
    /// 2) Use h and theta to roughly direct the beam in a grid that serves to populate a fine-grained lookup table
    ///    translating unrotated (!) camera coordinates to mirror voltages. The lookup table should use
    ///    MHApi.Scanning.BLIScanLookupTable class for creation
    /// </remarks>
    unsafe class LaserAlignment : ExperimentBase
    {
        /// <summary>
        /// Transformations assume that beam is perpendicular to the stage at this voltage
        /// </summary>
        IppiPoint_32f _v0 = new IppiPoint_32f(0.0f, 0.0f);

        /// <summary>
        /// The coordinates of the beam center at _v0
        /// </summary>
        IppiPoint _p0;

        /// <summary>
        /// If set to true it indicates that the camera y-axis is reflected
        /// in relation to the mirror y-axis
        /// </summary>
        bool _isYReflected = false;

        /// <summary>
        /// The rotation of the camera fov with respect to the mirror fov in radians
        /// </summary>
        double _camera_theta = 0.0;

        /// <summary>
        /// The height of the camera above the stage in pixels
        /// </summary>
        double _camera_height = 0.0;

        /// <summary>
        /// Background model to allow beam extraction
        /// </summary>
        DynamicBackgroundModel _bgModel;

        /// <summary>
        /// Image for calculation intermediates
        /// </summary>
        Image8 _calc;

        /// <summary>
        /// Image for foreground extraction
        /// </summary>
        Image8 _foreground;

        /// <summary>
        /// Buffer for marker extraction
        /// </summary>
        byte* _markerBuffer;

        /// <summary>
        /// Creates a new LaserAlignment object
        /// </summary>
        /// <param name="frameRate">The frameRate of the experiment</param>
        public LaserAlignment(int frameRate) : base("", "", "", 0, frameRate)
        {
        }

        #region Properties

        public override int SecondsRemaining
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string StatusMessage
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Computes an average angle for a set of angles
        /// </summary>
        /// <param name="angles">The angles which should be averaged in radians</param>
        /// <returns>The average angle in radians</returns>
        public static double AverageAngles(double[] angles)
        {
            double sinsum = 0;
            double cossum = 0;
            foreach(var angle in angles)
            {
                sinsum += Math.Sin(angle);
                cossum += Math.Cos(angle);
            }
            return Math.Atan2(sinsum, cossum);
        }

        ///// <summary>
        ///// Computes the augmented rotation matrix for an augmented 2D point around the origin
        ///// </summary>
        ///// <param name="theta">The clockwise rotation angle in radians</param>
        ///// <returns>The augmented (3x3) 2D rotation matrix</returns>
        //public static Matrix<double> RotationMatrix(double theta)
        //{
        //    return DenseMatrix.OfArray(new double[,]
        //    {
        //        {Math.Cos(theta), Math.Sin(theta), 0 },
        //        {-1*Math.Sin(theta), Math.Cos(theta), 0 },
        //        {0, 0, 1 }
        //    });
        //}

        ///// <summary>
        ///// Computes the augmented translation matrix for an augmented 2D point
        ///// </summary>
        ///// <param name="dx">Translation amount in x direction</param>
        ///// <param name="dy">Translation amount in y direction</param>
        ///// <returns>The augmented (3x3) 2D translation matrix</returns>
        //public static Matrix<double> TranslationMatrix(double dx, double dy)
        //{
        //    return DenseMatrix.OfArray(new double[,]
        //    {
        //        {1, 0, dx },
        //        {0, 1, dy },
        //        {0, 0, 1 }
        //    });
        //}

        ///// <summary>
        ///// Augments a 2D coordinate point
        ///// </summary>
        ///// <param name="px">The x-coordinate</param>
        ///// <param name="py">The y-coordinate</param>
        ///// <returns>A 3 element vector comprising the point</returns>
        //public static Vector<double> Augment2DCoordinate(double px, double py)
        //{
        //    return DenseVector.OfArray(new double[] { px, py, 1 });
        //}

        ///// <summary>
        ///// Composes (multiplies) a list of transformation matrices in order
        ///// NOTE: List should be ordered in order of application which needs to be reversed for the composition!
        ///// </summary>
        ///// <param name="matrices">List of transformations to compose</param>
        ///// <returns>The composed matrix</returns>
        //public static Matrix<double> Compose(List<Matrix<double>> matrices)
        //{
        //    Matrix<double> m = DenseMatrix.CreateIdentity(3);
        //    for(int i = matrices.Count-1; i>=0; i--)
        //        m *= matrices[i];
        //    return m;
        //}

        ///// <summary>
        ///// Applies transformation to given point
        ///// </summary>
        ///// <param name="p"></param>
        ///// <param name="T"></param>
        ///// <returns></returns>
        //public static IppiPoint TransformPoint(IppiPoint p, Matrix<double> T)
        //{
        //    Vector<double> result = Augment2DCoordinate(p.x, p.y) * T;
        //    return new IppiPoint((int)result[0], (int)result[1]);
        //}

        ///// <summary>
        ///// Applies transformation to given point
        ///// </summary>
        ///// <param name="p"></param>
        ///// <param name="T"></param>
        ///// <returns></returns>
        //public static IppiPoint_32f TransformPoint(IppiPoint_32f p, Matrix<double> T)
        //{
        //    Vector<double> result =  T * Augment2DCoordinate(p.x, p.y);
        //    return new IppiPoint_32f((float)result[0], (float)result[1]);
        //}

        /// <summary>
        /// Method for creating image background
        /// </summary>
        /// <param name="frameNumber">The current camera frame number</param>
        /// <param name="camImage">The camera image</param>
        /// <param name="poi">A detected centroid (all null)</param>
        /// <returns>True</returns>
        protected bool BuildBackground(int frameNumber, Image8 camImage, out IppiPoint? poi)
        {
            poi = null;
            if (frameNumber == 0 || _bgModel == null)
            {
                _bgModel = new DynamicBackgroundModel(camImage, 1.0f / Properties.Settings.Default.FrameRate);
                //Create image intermediates
                _calc = new Image8(camImage.Size);
                _foreground = new Image8(camImage.Size);
                //Initialize buffer for label markers
                int bufferSize = 0;
                cv.ippiLabelMarkersGetBufferSize_8u_C1R(camImage.Size, &bufferSize);
                if (_markerBuffer != null)
                {
                    Marshal.FreeHGlobal((IntPtr)_markerBuffer);
                    _markerBuffer = null;
                }
                _markerBuffer = (byte*)Marshal.AllocHGlobal(bufferSize);
            }
            else
                _bgModel.UpdateBackground(camImage);
            return true;
        }

        /// <summary>
        /// Runs the initial three point calibration to find approximate camera height and angle/reflection
        /// </summary>
        /// <param name="frameNumber">The current camera frame number</param>
        /// <param name="camImage">The camera image</param>
        /// <param name="poi">The detected beam centroid</param>
        /// <returns>True</returns>
        protected bool ThreePointCalibration(int frameNumber, Image8 camImage, out IppiPoint? poi)
        {

        }

        /// <summary>
        /// Finds the location of the laser beam in the camera image
        /// </summary>
        /// <param name="image">The current image in which to find the beam</param>
        /// <param name="background">The calculated background image</param>
        /// <param name="calc">Image buffer for intermediate calculations</param>
        /// <param name="foreground">Image buffer for foreground extraction</param>
        /// <returns></returns>
        private IppiPoint FindBeamLocation(Image8 image, Image8 background, Image8 calc, Image8 foreground)
        {
            IppiSize imageSize = new IppiSize(image.Width, image.Height);
            //Clear foreground and calc
            ip.ippiSet_8u_C1R(0, foreground.Image, foreground.Stride, imageSize);
            ip.ippiSet_8u_C1R(0, calc.Image, calc.Stride, imageSize);
            //Perform background subtraction
            cv.ippiAbsDiff_8u_C1R(image.Image, image.Stride, background.Image, background.Stride, calc.Image, calc.Stride, imageSize);
            //remove noise via 3x3 median filtering
            ip.ippiFilterMedianWeightedCenter3x3_8u_C1R(calc[2, 2], calc.Stride, foreground[2, 2], foreground.Stride, new IppiSize(image.Width - 4, image.Height - 4), 1);
            //threshold difference
            ip.ippiThreshold_LTVal_8u_C1IR(foreground.Image, foreground.Stride, imageSize, 111, 0);
            ip.ippiThreshold_GTVal_8u_C1IR(foreground.Image, foreground.Stride, imageSize, 110, 255);
            //perform closing operation
            ip.ippiDilate3x3_8u_C1IR(foreground.Image, foreground.Stride, imageSize);
            ip.ippiErode3x3_8u_C1IR(foreground.Image, foreground.Stride, imageSize);
            //feature extraction
            int nMarkers = 0;
            cv.ippiLabelMarkers_8u_C1IR(foreground.Image, foreground.Stride, imageSize, 1, 254, IppiNorm.ippiNormInf, &nMarkers, _markerBuffer);
            BlobWithMoments[] blobsDetected = BlobWithMoments.ExtractBlobs(foreground, nMarkers, new IppiROI(0, 0, image.Width, image.Height));
            //re-threshold foreground for display
            ip.ippiThreshold_LTVal_8u_C1IR(foreground.Image, foreground.Stride, imageSize, 1, 0);//6
            ip.ippiThreshold_GTVal_8u_C1IR(foreground.Image, foreground.Stride, imageSize, 0, 255);//5
            long maxArea = 0;
            int maxIndex = -1;
            if (blobsDetected != null)
            {
                for (int i = 0; i < blobsDetected.Length; i++)
                {
                    if (blobsDetected[i] == null)
                        break;
                    //Simply note down the largest blob
                    if (blobsDetected[i].Area > maxArea)
                    {
                        maxArea = blobsDetected[i].Area;
                        maxIndex = i;
                    }
                }
                //System.Diagnostics.Debug.WriteLine("Area: {0}", blobsDetected[maxIndex].Area);
                //System.Diagnostics.Debug.WriteLine("Eccentricity: {0}", blobsDetected[maxIndex].Eccentricity);
                if (maxArea > 100)
                    return blobsDetected[maxIndex].Centroid;
                else
                    return new IppiPoint(-1, -1);
            }
            else
                return new IppiPoint(-1, -1);
        }

        /// <summary>
        /// Uses three point calibration data to suggest mirror voltages to hit a given point
        /// </summary>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        private IppiPoint_32f GetMirrorVolts(IppiPoint coordinates)
        {
            var desired_rotation = _camera_theta * -1;//back rotation
            var dx_mirror = coordinates.x * Math.Cos(desired_rotation) - coordinates.y * Math.Sin(desired_rotation);
            var dy_mirror = coordinates.x * Math.Sin(desired_rotation) + coordinates.y * Math.Cos(desired_rotation);
            var vx = Math.Atan(dx_mirror / _camera_height);
            var vy = Math.Atan(dy_mirror / _camera_height);
            if (_isYReflected)
                vy *= -1;
            return new IppiPoint_32f((float)vx, (float)vy);
        }

        /// <summary>
        /// Given a voltage calculates the corresponding angle of the mirror in radians
        /// </summary>
        /// <param name="mirrorVolts"></param>
        /// <returns>The mirror angle in radians corresponding to the voltage</returns>
        private double MirrorAngle(double mirrorVolts)
        {
            double ang_degrees = mirrorVolts * Properties.Settings.Default.MirrorDOptPerV;
            return (ang_degrees / 180) * Math.PI;
        }

        /// <summary>
        /// Given a mirror angle in radians calculates the corresponding command voltage
        /// </summary>
        /// <param name="mirrorAngle">The desired angle in radians</param>
        /// <returns>The command voltage</returns>
        private double MirrorVoltage(double mirrorAngle)
        {
            double ang_degrees = mirrorAngle / Math.PI * 180;
            return ang_degrees / Properties.Settings.Default.MirrorDOptPerV;
        }

        /// <summary>
        /// Calculates the height of the mirrors above the stage in pixels
        /// </summary>
        /// <param name="pAlign">The pixel coordinates of the alignment beam</param>
        /// <param name="mirrorAngle">The mirror angle in radians</param>
        /// <returns>The height of the mirrors above the stage in pixels</returns>
        private double CalculateHeight(IppiPoint pAlign, double mirrorAngle)
        {
            double radius = Math.Sqrt((pAlign.x - _p0.x) * (pAlign.x - _p0.x) + (pAlign.y - _p0.y) * (pAlign.y - _p0.y));
            return radius / Math.Tan(mirrorAngle);
        }

        /// <summary>
        /// Calculates the rotation of the camera FOV relative to the mirror FOV
        /// </summary>
        /// <param name="pAlign">The pixel coordinates of the alignment beam</param>
        /// <param name="is_y_displacement">If this is calculated for y-excursion on the mirror need to adjust angle</param>
        /// <returns>The rotation angle of the camera axes relative to the mirror axes in radians</returns>
        private double CalculateCameraTheta(IppiPoint pAlign, bool is_y_displacement)
        {
            double dx = pAlign.x - _p0.x;
            double dy = pAlign.y - _p0.y;
            double radius = Math.Sqrt(dx * dx + dy * dy);
            if (radius == 0)
                throw new ArgumentException("Alignment radius is 0. Increase alignment mirror excursion voltages.");
            double offset = is_y_displacement ? Math.PI / 4 : 0;
            double theta = Math.Acos(dx / radius);
            if (dy >= 0)
                return theta - offset;
            else
                return 2 * Math.PI - theta - offset;
        }

        /// <summary>
        /// Takes an angle and transforms it to the corresponding angle in the interval [0, 2*PI]
        /// </summary>
        /// <param name="a">The angle to transform</param>
        /// <returns>The corresponding angle in the interval [0, 2*PI]</returns>
        private double NormAngle(double a)
        {
            if (a > 2 * Math.PI)
                while (a > 2 * Math.PI)
                    a -= 2 * Math.PI;
            else if (a < 0)
                while (a < 0)
                    a += 2 * Math.PI;
            return a;
        }

        /// <summary>
        /// Computes the smallest rotational difference between two angles independent
        /// of their sign or total rotation
        /// </summary>
        /// <param name="a1">The first angle</param>
        /// <param name="a2">The second angle</param>
        /// <returns>The smallest difference along the circle of a1 and a2</returns>
        private double ComputeMinorDeltaAngle(double a1, double a2)
        {
            a1 = NormAngle(a1);
            a2 = NormAngle(a2);
            var delta = Math.Abs(a1 - a2);
            if (delta > Math.PI)
                return 2 * Math.PI - delta;
            else
                return delta;
        }

        /// <summary>
        /// Checks based on the theta values returned by the independent axes calibration
        /// if the y-axis of the camera is reflected compared to the mirror y axis
        /// </summary>
        /// <param name="theta_x">Angle determined based on x excursion</param>
        /// <param name="theta_y">Angle determined based on y excursion</param>
        /// <param name="final_theta">The angle to use for the camera rotation</param>
        /// <returns>True if the y-axis is rotated</returns>
        private bool CheckReflection(double theta_x, double theta_y, out double final_theta)
        {
            //Compute minor angular difference between theta_x and theta_y
            //If below quality threshold, assume same and average
            //If above reflection threshold, report reflection and use theta_x
            //If in between write warning to Console and average
            var delta = ComputeMinorDeltaAngle(theta_x, theta_y);
            if(delta < Math.PI / 4)
            {
                final_theta = AverageAngles(new double[2] { theta_x, theta_y });
                return false;
            }
            if(delta > 3*Math.PI /4)
            {
                final_theta = theta_x;
                return true;
            }
            System.Diagnostics.Debug.WriteLine("WARNING: Angles determined by x and y mirror displacement are between 45 and 135 degrees.");
            System.Diagnostics.Debug.WriteLine("Assume no rotation and average. Likely inaccurate and will slow down calibration.");
            final_theta = AverageAngles(new double[2] { theta_x, theta_y });
            return false;
        }

        public override bool ProcessNext(int frameNumber, Image8 camImage, out IppiPoint? poi)
        {
            // Build background over five seconds
            if (frameNumber < Properties.Settings.Default.FrameRate * 5)
                return BuildBackground(frameNumber, camImage, out poi);
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (_foreground != null)
                _foreground.Dispose();
            if (_calc != null)
                _calc.Dispose();
            if (_markerBuffer != null)
            {
                Marshal.FreeHGlobal((IntPtr)_markerBuffer);
                _markerBuffer = null;
            }
        }

        #endregion
    }
}
