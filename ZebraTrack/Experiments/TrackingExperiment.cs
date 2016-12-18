using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ipp;
using MHApi.DrewsClasses;
using MHApi.Tracking;

namespace ZebraTrack.Experiments
{
    /// <summary>
    /// Base class for experiments that track fish
    /// </summary>
    abstract class TrackingExperiment : ExperimentBase
    {

        /// <summary>
        /// The fish tracker used by the experiment
        /// </summary>
        protected Tracker90mmDish Tracker { get; private set; } = null;

        /// <summary>
        /// The resolution in pixels per mm of the acquisition
        /// </summary>
        protected int PixelsPermm { get; private set; }

        /// <summary>
        /// Constructs a new tracking experiment
        /// </summary>
        /// <param name="folder">The folder in which to save files</param>
        /// <param name="name">The name of the experiment</param>
        /// <param name="fishID">The fish used for the experiment</param>
        /// <param name="experimentLength">The length of the experiment in seconds</param>
        /// <param name="frameRate">The acquisition framerate</param>
        public TrackingExperiment(string folder, string name, string fishID, int experimentLength, int frameRate, int pxPERmm) : base(folder, name, fishID, experimentLength, frameRate)
        {
            PixelsPermm = pxPERmm;
        }

        /// <summary>
        /// Creates the standard zebrafish tracker
        /// </summary>
        /// <returns>The tracker object</returns>
        protected virtual Tracker90mmDish CreateFishTracker(int imageWidth, int imageHeight, IppiPoint center)
        {
            //Assume fish length around 4 mm
            int approxFishLen = PixelsPermm * 4;
            //Since depending on image quality we can't expect the find the tail, assume body length of 2mm with a head-width of 0.8 mm as our area estimate
            int approxFishArea = (int)(0.8 * PixelsPermm * PixelsPermm * 2);
            Tracker90mmDish retval = new Tracker90mmDish(imageWidth, imageHeight, center);
            retval.BGUpdateEvery = FrameRate / 10;//update background at every 10th frame only
            retval.FramesInBackground = 30 * FrameRate / retval.BGUpdateEvery;//this is 30s of background
            retval.FramesInitialBackground = 120 * FrameRate;//initially build background over 2 minutes without tracking
            retval.Threshold = 6;
            retval.MinArea = approxFishArea / 6;
            retval.MaxAllowedArea = (int)(approxFishArea * 1.5);
            retval.MinEccentricity = 0.3;
            retval.FullTrustMinArea = approxFishArea / 4;
            double maxFishSpeed = 250 * PixelsPermm;//maximally observed speeds of < 25 cm per second
            double maxDisp = maxFishSpeed / FrameRate;//maximum inferred per-frame displacement
            retval.SearchRegionSize = (int)(maxDisp + approxFishLen);//set search region size leniently to maximal displacement plus length of the fish
            retval.RemoveCMOSISBrightLineArtefact = false;
            return retval;
        }

        public override bool ProcessNext(int frameNumber, Image8 camImage, out IppiPoint? poi)
        {
            //create tracker if necessary
            if (Tracker == null)
                Tracker = CreateFishTracker(camImage.Width, camImage.Height, new IppiPoint(camImage.Width / 2, camImage.Height / 2));
            poi = null;
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if(Tracker != null)
            {
                Tracker.Dispose();
                Tracker = null;
            }
        }
    }
}
