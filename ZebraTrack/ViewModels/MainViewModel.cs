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
            exp.Comment = Comment;
            exp.DOB = DOB;
            exp.WriteExperimentInformation();
            //dispose previewthread
            if (_acquisitionThread != null)
                _acquisitionThread.Dispose();
            //start experimental threads
            FrameIndex = 0;
            _acquisitionThread = new WorkerT<IExperiment>(TrackThreadRun, exp, true, 3000);
            IsRunning = true;
        }

        Tracker90mmDish InitializeTracker(int imageWidth, int imageHeight)
        {
            IppiPoint dishCenter = new IppiPoint(imageWidth / 2, imageHeight / 2);
            Tracker90mmDish retval = new Tracker90mmDish(imageWidth, imageHeight, dishCenter);
            retval.BGUpdateEvery = FrameRate / 10;//update background at 10 Hz only
            retval.FramesInBackground = 30 * FrameRate / retval.BGUpdateEvery;//this is 30s of background
            retval.FramesInitialBackground = 120 * FrameRate;
            retval.Threshold = 6;
            retval.MinArea = 18;
            retval.MaxAllowedArea = 250;
            retval.MinEccentricity = 0.3;
            retval.FullTrustMinArea = 30;
            retval.SearchRegionSize = 90;
            retval.RemoveCMOSISBrightLineArtefact = false;
            return retval;
        }

        /// <summary>
        /// Safely copies region around given centroid
        /// </summary>
        /// <param name="centroid">The centroid around which image region should be copied</param>
        /// <param name="regionImage">The target of the copy</param>
        /// <param name="image">The source of the copy operation</param>
        void CopyRegionImage(IppiPoint centroid, Image8 regionImage, Image8 image)
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

        #endregion

        #region ThreadProcedures

        void TrackThreadRun(AutoResetEvent stop, IExperiment experiment)
        {
            //Set up image for small region around fish
            Image8 fishImage = new Image8(60, 60);
            CameraLinkCamera camera = null;
            Tracker90mmDish tracker = null;
            try
            {
                camera = new CameraLinkCamera(Properties.Settings.Default.CameraInterface);
                tracker = InitializeTracker(camera.Width, camera.Height);
                using(Image8 image = new Image8(camera.Width, camera.Height))
                {
                    if(experiment != null && experiment.SuggestedBufferSeconds != null)
                    {
                        int buffsize = (int)(experiment.SuggestedBufferSeconds.Value * FrameRate);
                        camera.Start(buffsize > 1 ? buffsize : 1);
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
                if (tracker != null)
                    tracker.Dispose();
                if (fishImage != null)
                    fishImage.Dispose();
                if(IsRunning)
                    DispatcherHelper.CheckBeginInvokeOnUI(() => { Stop(); });
            }
        }

        #endregion

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


        #region Cleanup

        protected override void Dispose(bool disposing)
        {
            //Stop acquisition
            if(_acquisitionThread != null)
            {
                _acquisitionThread.Dispose();
                _acquisitionThread = null;
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
