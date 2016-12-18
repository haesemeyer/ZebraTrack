using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ipp;
using MHApi.DrewsClasses;

namespace ZebraTrack.Experiments
{
    /// <summary>
    /// Experiment to track fish during image preview
    /// No information is written to disk
    /// </summary>
    class PreviewTrack : TrackingExperiment
    {
        public PreviewTrack(int frameRate, int pxPERmm) : base("", "", "", 0, frameRate, pxPERmm)
        {
        }

        public override int SecondsRemaining
        {
            get
            {
                return 0;
            }
        }

        public override string StatusMessage
        {
            get
            {
                return "";
            }
        }

        /// <summary>
        /// Process the next preview frame
        /// </summary>
        /// <param name="frameNumber">The current frame number (ignored)</param>
        /// <param name="camImage">The current camera image</param>
        /// <param name="poi">The centroid of the fish if found</param>
        /// <returns>True</returns>
        public override bool ProcessNext(int frameNumber, Image8 camImage, out IppiPoint? poi)
        {
            base.ProcessNext(frameNumber, camImage, out poi);
            var fish = Tracker.Track(camImage);
            if (fish != null)
                poi = new IppiPoint(fish.Centroid.x, fish.Centroid.y);
            return true;//Preview only stopped from outside
        }
    }
}
