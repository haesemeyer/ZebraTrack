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
    /// Generic experimental interface
    /// </summary>
    interface IExperiment
    {
        /// <summary>
        /// Processes the next frame
        /// </summary>
        /// <param name="frameNumber">The index of the current frame</param>
        /// <param name="fishCentroid">The centroid of the fish</param>
        /// <param name="heading">The heading angle of the fish</param>
        /// <param name="fishImage">Small ROI image of the fish for further processing</param>
        /// <returns>True if the experiment should continue</returns>
        bool ProcessNext(uint frameNumber, IppiPoint fishCentroid, double heading, Image8 fishImage);

        /// <summary>
        /// The time remaining in the experiment
        /// </summary>
        TimeSpan TimeRemaining { get; }

        /// <summary>
        /// A status message about the experiment that can be displayed
        /// </summary>
        string StatusMessage { get; }
    }
}
