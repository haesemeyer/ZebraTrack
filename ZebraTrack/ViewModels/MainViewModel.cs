﻿/*
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
using System.Threading;

using MHApi.GUI;
using MHApi.CameraLink;
using MHApi.Threading;
using MHApi.DrewsClasses;
using MHApi.Tracking;
using MHApi.Imaging;
using MHApi.Utilities;

using ipp;

using ZebraTrack.Experiments;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace ZebraTrack.ViewModels
{
    unsafe class MainViewModel : ViewModelBase
    {
        public enum ImageType { Camera=0, Background=1, Foreground=2, Thresholded=3};

        #region Members

        /// <summary>
        /// The thread to run data acquisition
        /// </summary>
        WorkerT<IExperiment> _acquisitionThread;

        /// <summary>
        /// Indicates whether we are running an
        /// experiment or if we are in preview mode
        /// </summary>
        bool _isRunning;

        /// <summary>
        /// The current frame index
        /// </summary>
        uint _frameIndex;

        /// <summary>
        /// Displays the camera image
        /// </summary>
        EZImageSource _mainImage;

        /// <summary>
        /// Displays the fish closeup
        /// </summary>
        EZImageSource _fishImage;

        /// <summary>
        /// The name of the experiment
        /// </summary>
        string _experimentName;

        /// <summary>
        /// The fish name
        /// </summary>
        string _fishName;

        /// <summary>
        /// Comment to go along with the experiment
        /// </summary>
        string _comment;

        /// <summary>
        /// The date of birth of the fish
        /// </summary>
        DateTime _dob;

        /// <summary>
        /// The type of image to display during preview
        /// </summary>
        ImageType _displayImage;

        /// <summary>
        /// Radius of a circular mask drawn during preview
        /// </summary>
        private int _maskRadius;

        /// <summary>
        /// The image of the mask that will be drawn
        /// </summary>
        private Image8 _maskImage;

        #endregion

        #region Properties

        /// <summary>
        /// String representation of our image types
        /// </summary>
        public string[] DisplayTypeNames
        {
            get
            {
                return Enum.GetNames(typeof(ImageType));
            }
        }

        /// <summary>
        /// The type of image to display during preview
        /// </summary>
        public ImageType DisplayImage
        {
            get
            {
                return _displayImage;
            }
            set
            {
                _displayImage = value;
                RaisePropertyChanged(nameof(DisplayImage));
            }
        }

        /// <summary>
        /// The name of the experiment
        /// </summary>
        public string ExperimentName
        {
            get
            {
                return _experimentName;
            }
            set
            {
                _experimentName = value;
                RaisePropertyChanged(nameof(ExperimentName));
            }
        }

        /// <summary>
        /// The name of the fish type used
        /// </summary>
        public string FishName
        {
            get
            {
                return _fishName;
            }
            set
            {
                _fishName = value;
                RaisePropertyChanged(nameof(FishName));
            }
        }

        /// <summary>
        /// Comment associated with the experiment
        /// </summary>
        public string Comment
        {
            get
            {
                return _comment;
            }
            set
            {
                _comment = value;
                RaisePropertyChanged(nameof(Comment));
            }
        }

        /// <summary>
        /// The date of birth of the fish
        /// </summary>
        public DateTime DOB
        {
            get
            {
                return _dob;
            }
            set
            {
                _dob = value;
                RaisePropertyChanged(nameof(DOB));
            }
        }

        /// <summary>
        /// Radius of a circular mask drawn during preview
        /// </summary>
        public int MaskRadius
        {
            get
            {
                return _maskRadius;
            }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(MaskRadius), "MaskRadius cannot be smaller than 0");
                //If the mask is already initialized, create the new image
                if (_maskImage != null)
                    CreateCircularMask(_maskImage, DishCenter, value, true);
                _maskRadius = value;
                RaisePropertyChanged(nameof(MaskRadius));
            }
        }

        public IppiPoint DishCenter { get; private set; }

        /// <summary>
        /// Indicates whether we are running an
        /// experiment or if we are in preview mode
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return _isRunning;
            }
            private set
            {
                _isRunning = value;
                RaisePropertyChanged(nameof(IsRunning));
            }
        }

        /// <summary>
        /// The current frame index
        /// </summary>
        public uint FrameIndex
        {
            get
            {
                return _frameIndex;
            }
            private set
            {
                _frameIndex = value;
                RaisePropertyChanged(nameof(FrameIndex));
            }
        }

        /// <summary>
        /// The camera frame-rate
        /// </summary>
        public int FrameRate
        {
            get
            {
                return Properties.Settings.Default.FrameRate;
            }
        }

        /// <summary>
        /// Displays the camera image
        /// </summary>
        public EZImageSource MainImage
        {
            get
            {
                return _mainImage;
            }
        }

        /// <summary>
        /// Displays the fish closeup
        /// </summary>
        public EZImageSource FishImage
        {
            get
            {
                return _fishImage;
            }
        }

        public static IExperimentVM ExperimentViewModel { get; set; }

        #endregion

        public MainViewModel()
        {
            ExperimentName = "Experiment01";
            FishName = "TLAB";
            Comment = "";
            MaskRadius = 360;
            DOB = DateTime.Now - new TimeSpan(5, 0, 0, 0);
            DisplayImage = ImageType.Camera;
            if (IsInDesignMode)
                return;
            _mainImage = new EZImageSource();
            _fishImage = new EZImageSource();
            var preview = new PreviewTrack(Properties.Settings.Default.FrameRate, Properties.Settings.Default.PixelsPermm);
            _acquisitionThread = new WorkerT<IExperiment>(TrackThreadRun, preview, true, 3000);
        }

        #region Methods

        /// <summary>
        /// Stop running experiment and switch back to preview
        /// </summary>
        void Stop()
        {
            if (!IsRunning)
                return;
            IsRunning = false;
            //shut down experimental threads
            if (_acquisitionThread != null)
                _acquisitionThread.Dispose();

            //experiment stopped, restart preview
            FrameIndex = 0;
            var preview = new PreviewTrack(Properties.Settings.Default.FrameRate, Properties.Settings.Default.PixelsPermm);
            _acquisitionThread = new WorkerT<IExperiment>(TrackThreadRun, preview, true, 3000);
        }

        /// <summary>
        /// Start a new experiment
        /// </summary>
        void Start()
        {
            if (IsRunning)
                return;
            //create experimental class
            ExperimentBase exp = null;
            if (ExperimentViewModel != null)
                exp = ExperimentViewModel.CreateExperiment(ExperimentName, FishName);
            else
            {
                System.Diagnostics.Debug.WriteLine("No experiment view model was active. Exiting.");
                return;
            }
            if (exp == null)
            {
                MessageBox.Show("Could not create experiment. Check parameters.", "Failure to launch", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            exp.Comment = Comment;
            exp.DOB = DOB;
            exp.DishCenter = DishCenter;
            exp.Radius = MaskRadius;
            exp.WriteExperimentInformation();
            //dispose previewthread
            if (_acquisitionThread != null)
                _acquisitionThread.Dispose();
            //start experimental threads
            FrameIndex = 0;
            _acquisitionThread = new WorkerT<IExperiment>(TrackThreadRun, exp, true, 3000);
            IsRunning = true;
        }

        /// <summary>
        /// Restarts image preview
        /// </summary>
        void RestartPreview()
        {
            //Don't do this while an experiment is running
            if (IsRunning)
                return;
            //shut down acquisition thread
            if (_acquisitionThread != null)
                _acquisitionThread.Dispose();

            //restart preview
            FrameIndex = 0;
            var preview = new PreviewTrack(Properties.Settings.Default.FrameRate, Properties.Settings.Default.PixelsPermm);
            _acquisitionThread = new WorkerT<IExperiment>(TrackThreadRun, preview, true, 3000);
        }

        /// <summary>
        /// Safely copies region around given centroid
        /// </summary>
        /// <param name="centroid">The centroid around which image region should be copied</param>
        /// <param name="regionImage">The target of the copy</param>
        /// <param name="image">The source of the copy operation</param>
        public static void CopyRegionImage(IppiPoint centroid, Image8 regionImage, Image8 image)
        {
            IppiPoint copyStart = new IppiPoint(centroid.x - regionImage.Width / 2, centroid.y - regionImage.Height / 2);
            int destX = 0;
            int destY = 0;
            if(copyStart.x < 0)
            {
                destX -= copyStart.x;
                copyStart.x = 0;
            }
            if (copyStart.y < 0)
            {
                destY -= copyStart.y;
                copyStart.y = 0;
            }
            IppiSize copySize = new IppiSize(regionImage.Width - destX, regionImage.Height - destY);
            if (copyStart.x + copySize.width > image.Width)
                copySize.width = image.Width - copyStart.x;
            if (copyStart.y + copySize.height > image.Height)
                copySize.height = image.Height - copyStart.y;
            if (copySize.width > 0 && copySize.height > 0)
                ip.ippiCopy_8u_C1R(image[copyStart], image.Stride, regionImage[destX, destY], regionImage.Stride, copySize);
        }

        /// <summary>
        /// Draws a multiplicative mask for positioning a circular disk highlighting the center
        /// and circumference of a defined circle. Marked pixels are set to 0 all other to 1
        /// </summary>
        /// <param name="maskImage">The image on which the mask is drawn</param>
        /// <param name="center">The center of the circle - will be marked with 10px sized filled circle</param>
        /// <param name="maskRadius">The radius of the circle - will be marked with 3px wide line on the outside of maskRadius</param>
        public static void CreateCircularMask(Image8 maskImage, IppiPoint center, int maskRadius, bool addLookupBorder)
        {
            if (maskImage == null)
                throw new ArgumentNullException(nameof(maskImage));
            if (maskImage.IsDisposed)
                throw new ObjectDisposedException(nameof(maskImage));
            //Set all pixels to 1
            ip.ippiSet_8u_C1R(1, maskImage.Image, maskImage.Stride, maskImage.Size);
            //Loop over pixels that are within the outer square of the mask, considering the line thickness as well
            int minX = center.x - maskRadius - 3;
            if (minX < 0)
                minX = 0;
            int maxX = center.x + maskRadius + 3;
            if (maxX >= maskImage.Size.width)
                maxX = maskImage.Size.width - 1;
            int minY = center.y - maskRadius - 3;
            if (minY < 0)
                minY = 0;
            int maxY = center.y + maskRadius + 3;
            if (maxY >= maskImage.Size.height)
                maxY = maskImage.Size.height - 1;
            IppiPoint testP = new IppiPoint();
            for(int x = minX; x <= maxX; x++)
            {
                for(int y = minY; y <= maxY; y++)
                {
                    testP.x = x;
                    testP.y = y;
                    //Test if we are either in the boundary line or within the innner marker
                    var dist = Distance.Euclidian(center, testP);
                    if (dist <= 10 || (dist >= maskRadius && dist <= maskRadius + 3))
                    {
                        byte* pixel = maskImage.Image + x + maskImage.Stride * y;
                        *pixel = 0;
                    }
                }
            }
            if (addLookupBorder)
            {
                for (int x = 0; x <= maskImage.Size.width; x++)
                {
                    byte* pixel = maskImage.Image + x + maskImage.Stride * 783;
                    *pixel = 0;
                    pixel = maskImage.Image + x + maskImage.Stride * 784;
                    *pixel = 0;
                    pixel = maskImage.Image + x + maskImage.Stride * 785;
                    *pixel = 0;
                }
            }
        }

        /// <summary>
        /// Uses multiplication to draw a mask onto an image
        /// </summary>
        /// <param name="maskImage">The mask image to draw</param>
        /// <param name="image">The camera image to draw the mask on</param>
        public static void DrawMask(Image8 maskImage, Image8 image)
        {
            if (maskImage.Size.width != image.Size.width || maskImage.Size.height != image.Size.height)
                throw new ArgumentException("Both images need to have the same dimensions");
            ip.ippiMul_8u_C1IRSfs(maskImage.Image, maskImage.Stride, image.Image, image.Stride, image.Size, 0);
        }

        #endregion

        #region ThreadProcedures

        void TrackThreadRun(AutoResetEvent stop, IExperiment experiment)
        {
            //Set up image for small region around fish
            Image8 fishImage = new Image8(60, 60);
            CameraLinkCamera camera = null;
            try
            {
                camera = new CameraLinkCamera(Properties.Settings.Default.CameraInterface);
                using(Image8 image = new Image8(camera.Width, camera.Height))
                {
                    if(experiment != null && experiment.SuggestedBufferSeconds != null)
                    {
                        int buffsize = (int)(experiment.SuggestedBufferSeconds.Value * FrameRate);
                        camera.Start(buffsize > 2 ? buffsize : 2);
                    }
                    else
                        camera.Start(100);//start camera with 100 frames in buffer by default
                    while (!stop.WaitOne(0))
                    {
                        try
                        {
                            FrameIndex = camera.Extract(image, FrameIndex) + 1;
                            IppiPoint? fishCentroid = null;
                            if (experiment != null)
                                if (!experiment.ProcessNext((int)FrameIndex, image, out fishCentroid))
                                    break;

                            //at 10Hz display camera and fish image
                            if(FrameIndex % (FrameRate / 10) == 0)
                            {
                                //blank the fish image
                                ip.ippiSet_8u_C1R(0, fishImage.Image, fishImage.Stride, fishImage.Size);
                                if (experiment is PreviewTrack && DisplayImage != ImageType.Camera)
                                {
                                    Image8 toHandle = null;
                                    PreviewTrack pt = experiment as PreviewTrack;
                                    switch (DisplayImage)
                                    {
                                        case ImageType.Background:
                                            toHandle = pt.Background;
                                            MainImage.CMax = 255;
                                            FishImage.CMax = 255;
                                            break;
                                        case ImageType.Foreground:
                                            toHandle = pt.Foreground;
                                            MainImage.CMax = 255;
                                            FishImage.CMax = 255;
                                            break;
                                        default:
                                            toHandle = pt.Thresholded;
                                            MainImage.CMax = 1;
                                            FishImage.CMax = 1;
                                            break;
                                    }
                                    if(toHandle != null)
                                    {
                                        if (fishCentroid != null)
                                            CopyRegionImage(fishCentroid.Value, fishImage, toHandle);
                                        MainImage.Write(toHandle, stop);
                                        FishImage.Write(fishImage, stop);
                                    }
                                }
                                else
                                {
                                    //In preview we also apply our mask
                                    if (experiment is PreviewTrack)
                                    {
                                        if (_maskImage == null || _maskImage.Width != image.Width || _maskImage.Height != image.Height)
                                        {
                                            if (_maskImage != null)
                                                _maskImage.Dispose();
                                            _maskImage = new Image8(image.Width, image.Height);
                                            DishCenter = new IppiPoint(_maskImage.Width / 2, _maskImage.Height / 2);
                                            CreateCircularMask(_maskImage, DishCenter, MaskRadius, true);
                                        }
                                        DrawMask(_maskImage, image);
                                    }

                                    if (fishCentroid != null)
                                        CopyRegionImage(fishCentroid.Value, fishImage, image);
                                    MainImage.CMax = 255;
                                    FishImage.CMax = 255;
                                    MainImage.Write(image, stop);
                                    FishImage.Write(fishImage, stop);
                                }
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }
                }
            }
            catch(NIImaqException camException)
            {
                System.Diagnostics.Debug.WriteLine(camException);
            }
            finally
            {
                if (experiment != null && experiment is IDisposable)
                    (experiment as IDisposable).Dispose();
                if(camera != null)
                    camera.Dispose();
                if (fishImage != null)
                    fishImage.Dispose();
                if(IsRunning)
                    DispatcherHelper.CheckBeginInvokeOnUI(() => { Stop(); });
            }
        }

        #endregion

        #region Command sinks

        RelayCommand _startStopClick;

        /// <summary>
        /// The command that handles starting/stopping of an experiment
        /// </summary>
        public ICommand StartStopClick
        {
            get
            {
                if (_startStopClick == null)
                    _startStopClick = new RelayCommand(param => { if (!IsRunning) Start(); else Stop(); });
                return _startStopClick;
            }
        }

        RelayCommand _restartPreviewClick;

        /// <summary>
        /// The command that handles restarting of image preview
        /// </summary>
        public ICommand RestartPreviewClick
        {
            get
            {
                if (_restartPreviewClick == null)
                    _restartPreviewClick = new RelayCommand(param => { if (!IsRunning) RestartPreview(); });
                return _restartPreviewClick;
            }
        }

        #endregion


        #region Cleanup

        protected override void Dispose(bool disposing)
        {
            //Stop acquisition
            if(_acquisitionThread != null)
            {
                _acquisitionThread.Dispose();
                _acquisitionThread = null;
            }
            if(_maskImage != null)
            {
                _maskImage.Dispose();
                _maskImage = null;
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
