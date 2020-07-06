/*
Copyright 2017-2019 Martin Haesemeyer
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
using System.IO;

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
        /// Type to describe the current experimental phase
        /// </summary>
        public enum ExperimentPhases { BGround = 0, ThreePoint = 1, InterpTable = 2, Done = 3 }

        /// <summary>
        /// Container for the points identified during 3-point calibration
        /// </summary>
        class CalibrationPoints
        {
            /// <summary>
            /// The origin at vx=vy=0
            /// </summary>
            public IppiPoint Origin = new IppiPoint(-1, -1);

            /// <summary>
            /// The point when moving the xmirror while keeping vy=0
            /// </summary>
            public IppiPoint XMove = new IppiPoint(-1, -1);

            /// <summary>
            /// The point when moving the ymirror while keeping vx=0
            /// </summary>
            public IppiPoint YMove = new IppiPoint(-1, -1);
        }

        /// <summary>
        /// Collects all information related to the interpolation
        /// lookup table construction
        /// </summary>
        class InterpolationParams
        {
            /// <summary>
            /// For each pixel we have accumulated as error correct
            /// mirrors by this many volts
            /// </summary>
            public const float VoltsPerErrorPixel = 0.0005f;

            /// <summary>
            /// Accumulated x error in pixels
            /// </summary>
            public int XError = 0;

            /// <summary>
            /// Accumulated y error in pixels
            /// </summary>
            public int YError = 0;

            /// <summary>
            /// Indicates whether a request has been satisfied
            /// and a new point should be requested
            /// </summary>
            public bool RequestDealt = true;//initially true to request first point

            /// <summary>
            /// Inidicates that we are currently walking the beam
            /// to find a requested point
            /// </summary>
            public bool Walking;

            /// <summary>
            /// For each point keeps track of the frame count
            /// </summary>
            public int StepFrame = 0;

            /// <summary>
            /// The number of times we tried to find a point from scratch
            /// </summary>
            public int RetryCount = 0;

            /// <summary>
            /// The next point that should be targeted and found
            /// </summary>
            public IppiPoint NextRequest = new IppiPoint();

            /// <summary>
            /// The currently identified position of the laser
            /// </summary>
            public IppiPoint CurrentPoint = new IppiPoint();

            /// <summary>
            /// The voltages delived to the mirror for the current point
            /// </summary>
            public IppiPoint_32f CurrentVolts = new IppiPoint_32f();

            /// <summary>
            /// The scanning lookup table we build
            /// </summary>
            public BLIScanLookupTable LookupTable;
        }

        /// <summary>
        /// The current experimental phase
        /// </summary>
        ExperimentPhases _experimentPhase = ExperimentPhases.BGround;

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
        /// Use second "background" model to average
        /// foreground images
        /// </summary>
        DynamicBackgroundModel _fgModel;

        /// <summary>
        /// Image for calculation intermediates
        /// </summary>
        Image8 _calc;

        /// <summary>
        /// Image for foreground extraction
        /// </summary>
        Image8 _foreground;

        /// <summary>
        /// Morphology object for noise reductions
        /// </summary>
        Morphology _strel3x3;

        /// <summary>
        /// Buffer for marker extraction
        /// </summary>
        byte* _markerBuffer;

        /// <summary>
        /// Keeps track of the current frame in the three-point calibration
        /// </summary>
        int _threePointFrame;

        /// <summary>
        /// The points identified during 3-point calibration
        /// </summary>
        CalibrationPoints _threePointPoints;

        /// <summary>
        /// The parameters used when building interpolation table
        /// </summary>
        InterpolationParams _interpParams;

        /// <summary>
        /// The controller of the laser we use
        /// </summary>
        SDLPS500Controller _laser;

        /// <summary>
        /// The laser positioning scanner
        /// </summary>
        RandomAccessScanner _scanner;

        /// <summary>
        /// Creates a new LaserAlignment object
        /// </summary>
        /// <param name="frameRate">The frameRate of the experiment</param>
        public LaserAlignment(int frameRate) : base("", "", "", 0, frameRate)
        {
            //TODO: Here we should probably get information about actual ROI to use as well as where to store calibration data
            _laser = new SDLPS500Controller(Properties.Settings.Default.DAQ, Properties.Settings.Default.LaserAO);
            string xchannel = Properties.Settings.Default.DAQ + "/" + Properties.Settings.Default.ScannerX;
            string ychannel = Properties.Settings.Default.DAQ + "/" + Properties.Settings.Default.ScannerY;
            _scanner = new RandomAccessScanner(null, xchannel, ychannel, -10, 10);
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
        protected void BuildBackground(int frameNumber, Image8 camImage, out IppiPoint? poi)
        {
            _laser.LaserPower = 0;
            poi = null;
            if (frameNumber == 0 || _bgModel == null)
            {
                _bgModel = new DynamicBackgroundModel(camImage, 1.0f / Properties.Settings.Default.FrameRate);
                _fgModel = new DynamicBackgroundModel(camImage, 5.0f / Properties.Settings.Default.FrameRate);
                //Create image intermediates
                _calc = new Image8(camImage.Size);
                _foreground = new Image8(camImage.Size);
                _strel3x3 = Morphology.Generate3x3Mask(camImage.Size);
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
            if (frameNumber >= Properties.Settings.Default.FrameRate * 5)
            {
                _experimentPhase = ExperimentPhases.ThreePoint;
                _threePointFrame = 0;
                _threePointPoints = new CalibrationPoints();
            }
        }

        /// <summary>
        /// Method to perform all necessary steps to find one calibration point
        /// in three-point calibration
        /// </summary>
        /// <param name="frame">Frame number 0-based from start of operation</param>
        /// <param name="camImage">The current camera image</param>
        /// <param name="voltages">The voltages to move laser to</param>
        /// <returns>-1,-1 or the identified point</returns>
        protected IppiPoint MoveAndDetect(int frame, Image8 camImage, IppiPoint_32f voltages)
        {
            if (frame < Properties.Settings.Default.FrameRate / 10)
            {
                //In the first 100 ms we just give the scanner ample time to reach the target
                _scanner.Hit(voltages);
                return new IppiPoint(-1, -1);
            }
            else if (frame < Properties.Settings.Default.FrameRate * 1.1)
            {
                //In the next second we build our foreground
                _laser.LaserPower = Properties.Settings.Default.LaserCalibPowermW;
                _fgModel.UpdateBackground(camImage);
                return new IppiPoint(-1, -1);
            }
            else
            {
                //Let's find the beam location and return it
                return FindBeamLocation(_fgModel.Background, _bgModel.Background, _calc, _foreground);
            }
        }

        /// <summary>
        /// Runs the initial three point calibration to find approximate camera height and angle/reflection
        /// </summary>
        /// <param name="frameNumber">The current camera frame number</param>
        /// <param name="camImage">The camera image</param>
        /// <param name="poi">The detected beam centroid</param>
        protected void ThreePointCalibration(int frameNumber, Image8 camImage, out IppiPoint? poi)
        {
            poi = null;
            //Points not set in _threePointPoints are set to -1,-1. Use this to track which point we handle right now
            if(_threePointPoints.Origin.x == -1)
            {
                //Work on origin
                var p = MoveAndDetect(_threePointFrame, camImage, new IppiPoint_32f(0.0f, 0.0f));
                if (p.x != -1)
                {
                    //found the point
                    _threePointFrame = -1;//will be incremented to 0 below!
                    _threePointPoints.Origin = new IppiPoint(p.x, p.y);
                    _p0 = new IppiPoint(p.x, p.y);//set our origin coordinates for later calculations
                    poi = p;
                    //reset foreground in preparation for x movement
                    _fgModel.Dispose();
                    _fgModel = new DynamicBackgroundModel(camImage, 5.0f / Properties.Settings.Default.FrameRate);
                }
            }
            else if(_threePointPoints.XMove.x == -1)
            {
                //Work on x-displacement
                var p = MoveAndDetect(_threePointFrame, camImage, new IppiPoint_32f(Properties.Settings.Default.MirrorXVAln, 0.0f));
                if (p.x != -1)
                {
                    //found the point
                    _threePointFrame = -1;//will be incremented to 0 below!
                    _threePointPoints.XMove = new IppiPoint(p.x, p.y);
                    poi = p;
                    //reset foreground in preparation for y movement
                    _fgModel.Dispose();
                    _fgModel = new DynamicBackgroundModel(camImage, 5.0f / Properties.Settings.Default.FrameRate);
                }
            }
            else if(_threePointPoints.YMove.x == -1)
            {
                //Work on y-displacement
                var p = MoveAndDetect(_threePointFrame, camImage, new IppiPoint_32f(0.0f, Properties.Settings.Default.MirrorYVAln));
                if (p.x != -1)
                {
                    //found the point
                    _threePointFrame = -1;//will be incremented to 0 below!
                    _threePointPoints.YMove = new IppiPoint(p.x, p.y);
                    poi = p;
                    //reset foreground in preparation of interpolation table building
                    _fgModel.Dispose();
                    _fgModel = new DynamicBackgroundModel(camImage, 20.0f / Properties.Settings.Default.FrameRate);
                }
            }
            else
            {
                //Use results to calculate calibration
                //Height
                double camera_height_x = CalculateHeight(_threePointPoints.XMove, MirrorAngle(Properties.Settings.Default.MirrorXVAln));
                System.Diagnostics.Debug.WriteLine("Height according to x-mirror is: {0}", camera_height_x);
                double camera_height_y = CalculateHeight(_threePointPoints.YMove, MirrorAngle(Properties.Settings.Default.MirrorYVAln));
                System.Diagnostics.Debug.WriteLine("Height according to y-mirror is: {0}", camera_height_y);
                _camera_height = (camera_height_x + camera_height_y) / 2.0;
                //Theta and reflection
                double theta_x = CalculateCameraTheta(_threePointPoints.XMove, false);
                System.Diagnostics.Debug.WriteLine("Theta according to x-mirror is: {0}", theta_x);
                double theta_y = CalculateCameraTheta(_threePointPoints.YMove, true);
                System.Diagnostics.Debug.WriteLine("Theta according to y-mirror is: {0}", theta_y);
                _isYReflected = CheckReflection(theta_x, theta_y, out _camera_theta);
                System.Diagnostics.Debug.WriteLine("Y reflection: {0}", _isYReflected);
                _experimentPhase = ExperimentPhases.InterpTable;
                _interpParams = new InterpolationParams();
                //At this point include whole camera image in the interpolation ROI
                _interpParams.LookupTable = new BLIScanLookupTable(new IppiROI(0, 0, camImage.Width, camImage.Height));
            }
            _threePointFrame++;
        }

        /// <summary>
        /// Runs the construction of the interpolation lookup table
        /// </summary>
        /// <param name="frameNumber">The current camera frame number</param>
        /// <param name="camImage">The camera image</param>
        /// <param name="poi">The detected beam centroid</param>
        protected void InterpTableCalibration(int frameNumber, Image8 camImage, out IppiPoint? poi)
        {
            poi = null;
            if (_interpParams.RequestDealt)
            {
                System.Diagnostics.Debug.Assert(!_interpParams.Walking);
                //we have dealt the last request so lets get the next point or leave calibration
                //if table is complete
                if (!_interpParams.LookupTable.AddNext(new PointVoltagePair(_interpParams.CurrentPoint, _interpParams.CurrentVolts.x,
                    _interpParams.CurrentVolts.y), out _interpParams.NextRequest))
                {
                    //lookup table complete. Precompute and save to file
                    _interpParams.LookupTable.Precompute();
                    _experimentPhase = ExperimentPhases.Done;
                    var calibFile = File.CreateText("main.calib");
                    _interpParams.LookupTable.SaveToFile(calibFile);
                    calibFile.Close();
                    System.Diagnostics.Debug.WriteLine("Calibration updated");
                    return;
                }
                //we got a new point to deal with - prepare to immediately trigger the next condition and reset our foreground
                _fgModel.Dispose();
                _fgModel = new DynamicBackgroundModel(camImage, 5.0f / Properties.Settings.Default.FrameRate);
                _interpParams.RequestDealt = false;
                _interpParams.StepFrame = 0;
            }
            if (_interpParams.StepFrame == 0 && !_interpParams.RequestDealt)
            {
                //we have a request to deal with - try to target the requested coordinates and initiate walk
                _interpParams.CurrentVolts = GetMirrorVolts(_interpParams.NextRequest);
                _interpParams.CurrentVolts.x += _interpParams.XError * InterpolationParams.VoltsPerErrorPixel;
                _interpParams.CurrentVolts.y += _interpParams.YError * InterpolationParams.VoltsPerErrorPixel;
                _scanner.Hit(_interpParams.CurrentVolts);
                _interpParams.Walking = true;
                _interpParams.StepFrame++;
            }
            else if (_interpParams.StepFrame > Properties.Settings.Default.FrameRate/100 &&
                _interpParams.StepFrame < Properties.Settings.Default.FrameRate/5 && _interpParams.Walking)
            {
                //give scanner 10ms to reach the target and then collect the foreground until 200ms
                _fgModel.UpdateBackground(camImage);
                _interpParams.StepFrame++;
            }
            else if (_interpParams.StepFrame > Properties.Settings.Default.FrameRate / 5 && _interpParams.Walking)
            {
                //Analyse coordinates. If they match the requested point, add the voltages
                //to the lookup table. Otherwise compute the error and try again
                IppiPoint actual = FindBeamLocation(_fgModel.Background, _bgModel.Background, _calc, _foreground);
                poi = new IppiPoint(actual.x, actual.y);
                //test for error
                if ((actual.x - _interpParams.NextRequest.x) == 0 && (actual.y - _interpParams.NextRequest.y) == 0)
                {
                    //Looks good, add point and on next iteration we continue with next request
                    _interpParams.RequestDealt = true;
                    _interpParams.RetryCount = 0;
                    _interpParams.CurrentPoint = _interpParams.NextRequest;
                    _interpParams.StepFrame = 0;
                }
                else
                {
                    int error;
                    //update x and y errors
                    if (_interpParams.NextRequest.x > actual.x)
                    {
                        error = _interpParams.NextRequest.x - actual.x;
                        if (error < 5)
                            _interpParams.XError += error;
                        else
                            _interpParams.XError += 5;
                    }
                    else
                    {
                        error = _interpParams.NextRequest.x - actual.x;
                        if (error > -5)
                            _interpParams.XError += error;
                        else
                            _interpParams.XError -= 5;
                    }
                    if (_interpParams.NextRequest.y > actual.y)
                    {
                        error = _interpParams.NextRequest.y - actual.y;
                        if (error < 5)
                            _interpParams.YError += error;
                        else
                            _interpParams.YError += 5;
                    }
                    else
                    {
                        error = _interpParams.NextRequest.y - actual.y;
                        if (error > -5)
                            _interpParams.YError += error;
                        else
                            _interpParams.YError -= 5;
                    }
                    //too large errors may indicate that the point actually moved off the screen
                    //in that case we try to recover by returning to the original position and going
                    //from there
                    if (Math.Abs(error) > 100)
                    {
                        _interpParams.XError = 0;
                        _interpParams.YError = 0;
                    }
                    _interpParams.RetryCount++;
                    if (_interpParams.RetryCount > 250)
                    {
                        //We can't reach the point. Add it to the table nonetheless and continue (may be occluded point)
                        //At this point the errors are likely meaningless so reset them
                        //also we add the point as if the trigonometry lookup table is correct
                        _interpParams.CurrentVolts = GetMirrorVolts(_interpParams.NextRequest);
                        _interpParams.XError = 0;
                        _interpParams.YError = 0;
                        _interpParams.CurrentPoint = _interpParams.NextRequest;
                        if(!_interpParams.LookupTable.ForceAddNext(new PointVoltagePair(_interpParams.CurrentPoint, _interpParams.CurrentVolts.x,
                            _interpParams.CurrentVolts.y), out _interpParams.NextRequest))
                        {
                            //lookup table complete - Precompute interpolations and save to file
                            _interpParams.LookupTable.Precompute();
                            var calibFile = File.CreateText("main.calib");
                            _interpParams.LookupTable.SaveToFile(calibFile);
                            calibFile.Close();
                            System.Diagnostics.Debug.WriteLine("Calibration updated");
                            return;
                        }
                        System.Diagnostics.Debug.WriteLine("Force add point to lookup table");
                        _interpParams.RetryCount = 0;
                    }
                }
                //Not walking and reset step frame to restart where we left off
                _interpParams.Walking = false;
                _interpParams.StepFrame = 0;
            }//else if analyse walk
        }

        protected override void WriteExperimentInfo(StreamWriter infoWriter)
        {
            base.WriteExperimentInfo(infoWriter);
            if (infoWriter != null)
            {
                infoWriter.WriteLine("Laser Alignment performed");
                infoWriter.WriteLine();
            }
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
            //perform closing operation - 2 step to put result back into foreground
            _strel3x3.Dilate(foreground, calc, imageSize);
            _strel3x3.Erode(calc, foreground, imageSize);
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
                // System.Diagnostics.Debug.WriteLine("Area: {0}", blobsDetected[maxIndex].Area);
                // System.Diagnostics.Debug.WriteLine("Eccentricity: {0}", blobsDetected[maxIndex].Eccentricity);
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
            double offset = is_y_displacement ? Math.PI / 2 : 0;
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
            switch (_experimentPhase)
            {
                case ExperimentPhases.BGround:
                    BuildBackground(frameNumber, camImage, out poi);
                    break;
                case ExperimentPhases.ThreePoint:
                    ThreePointCalibration(frameNumber, camImage, out poi);
                    break;
                case ExperimentPhases.InterpTable:
                    InterpTableCalibration(frameNumber, camImage, out poi);
                    break;
                default:
                    poi = null;
                    return false;
            }
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if(_bgModel != null)
            {
                _bgModel.Dispose();
                _bgModel = null;
            }
            if(_fgModel != null)
            {
                _fgModel.Dispose();
                _fgModel = null;
            }
            if (_calc != null)
            {
                _calc.Dispose();
                _calc = null;
            }
            if(_foreground != null)
            {
                _foreground.Dispose();
                _foreground = null;
            }
            if (_markerBuffer != null)
            {
                Marshal.FreeHGlobal((IntPtr)_markerBuffer);
                _markerBuffer = null;
            }
            if(_laser != null)
            {
                _laser.Dispose();
                _laser = null;
            }
            if(_scanner != null)
            {
                _scanner.Hit(new IppiPoint_32f(0.0f, 0.0f));
                _scanner.Dispose();
                _scanner = null;
            }
            if(_strel3x3 != null)
            {
                _strel3x3.Dispose();
                _strel3x3 = null;
            }
        }

        #endregion
    }
}
