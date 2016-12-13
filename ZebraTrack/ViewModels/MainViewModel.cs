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

using ipp;

using ZebraTrack.Experiments;

namespace ZebraTrack.ViewModels
{
    unsafe class MainViewModel : ViewModelBase
    {
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

        #endregion

        #region Properties

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

        #endregion

        public MainViewModel()
        {
            if (IsInDesignMode)
                return;
            _mainImage = new EZImageSource();
            _fishImage = new EZImageSource();
            _acquisitionThread = new WorkerT<IExperiment>(TrackThreadRun, null, true, 3000);
        }

        #region Methods

        void Stop()
        {
            if (!IsRunning)
                return;
            //shut down experimental threads
            if (_acquisitionThread != null)
                _acquisitionThread.Dispose();

            //experiment stopped, restart preview
            FrameIndex = 0;
            _acquisitionThread = new WorkerT<IExperiment>(TrackThreadRun, null, true, 3000);
            IsRunning = false;
        }

        void Start()
        {
            if (IsRunning)
                return;
            //dispose previewthread
            if (_acquisitionThread != null)
                _acquisitionThread.Dispose();
            //create experimental class
            IExperiment exp = null;
            //start experimental threads
            _acquisitionThread = new WorkerT<IExperiment>(TrackThreadRun, exp, true, 3000);
            FrameIndex = 0;
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
            IppiSize copySize = new IppiSize(regionImage.Width, regionImage.Height);
            if (copyStart.x + copySize.width > image.Width)
                copySize.width = image.Width - copyStart.x;
            if (copyStart.y + copySize.height > image.Height)
                copySize.height = image.Height - copyStart.y;
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
                    camera.Start(100);//start camera with 100 frames in buffer
                    while (!stop.WaitOne(0))
                    {
                        try
                        {
                            FrameIndex = camera.Extract(image, FrameIndex) + 1;
                            BlobWithMoments fish = tracker.Track(image);

                            //blank the fish image and if a fish was found display region around it
                            ip.ippiSet_8u_C1R(0, fishImage.Image, fishImage.Stride, fishImage.Size);
                            if (fish != null)
                                CopyRegionImage(fish.Centroid, fishImage, image);

                            if (experiment != null)
                                experiment.ProcessNext(FrameIndex, fish.Centroid, fish.Angle, fishImage);

                            //at 10Hz display camera and fish image
                            if(FrameIndex % (FrameRate / 10) == 0)
                            {
                                MainImage.Write(image, stop);
                                FishImage.Write(fishImage, stop);
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
            }
        }

        #endregion

            #region Cleanup

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        #endregion
    }
}
