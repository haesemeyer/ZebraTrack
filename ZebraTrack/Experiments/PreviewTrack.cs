﻿using System;
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
        /// <summary>
        /// If available the background image of the tracker
        /// </summary>
        public Image8 Background
        {
            get
            {
                return Tracker != null ? Tracker.Background : null;
            }
        }

        /// <summary>
        /// If available the foreground image of the tracker
        /// </summary>
        public Image8 Foreground
        {
            get
            {
                return Tracker != null ? Tracker.BGSubtracted : null;
            }
        }

        /// <summary>
        /// If available the tresholded foreground of the tracker
        /// </summary>
        public Image8 Thresholded
        {
            get
            {
                return Tracker != null ? Tracker.Foreground : null;
            }
        }

        /// <summary>
        /// Create a new PreviewTrack experiment
        /// </summary>
        /// <param name="frameRate">The acquisition framerate</param>
        /// <param name="pxPERmm">The acquisition resolution</param>
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
