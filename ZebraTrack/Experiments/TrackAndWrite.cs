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
    /// Very simple experiment class that for a given time
    /// writes fish position and heading to file
    /// </summary>
    class TrackAndWrite : ExperimentBase
    {
        /// <summary>
        /// The last frame that was processed
        /// </summary>
        int _lastFrame = 0;

        /// <summary>
        /// The total number of frames in the experiment
        /// </summary>
        int _totalFrames;
        
        public TrackAndWrite(int expSeconds, int frameRate, string folder, string name, string fishID) : base(folder, name, fishID, expSeconds, frameRate)
        {
            _totalFrames = ExperimentLength * FrameRate;
        }

        public override string StatusMessage
        {
            get
            {
                return "";
            }
        }

        /// <summary>
        /// The number of seconds remaining in the experiment
        /// </summary>
        public override int SecondsRemaining
        {
            get
            {
                return (int)(ExperimentLength - _lastFrame / FrameRate);
            }
        }
        

        public override bool ProcessNext(int frameNumber, IppiPoint fishCentroid, double heading, Image8 fishImage)
        {
            _lastFrame = frameNumber;
            if (frameNumber >= _totalFrames)
                return false;
            throw new NotImplementedException();
        }
    }
}
