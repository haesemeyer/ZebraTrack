using System;
using System.IO;
using MHApi.Imaging;
using MHApi.DrewsClasses;

namespace ZebraTrack.Experiments
{
    abstract class ExperimentBase : IExperiment, IDisposable
    {
        /// <summary>
        /// Text file interface for experiment information
        /// </summary>
        private StreamWriter _infoWriter;

        /// <summary>
        /// The saver object from which different file
        /// writers can be derived
        /// </summary>
        protected Saver FileSaver { get; private set; }

        /// <summary>
        /// The acquisition framerate
        /// </summary>
        protected int FrameRate { get; private set; }

        /// <summary>
        /// The intended length of the experiment in seconds
        /// </summary>
        protected int ExperimentLength { get; private set; }

        /// <summary>
        /// The name of the experiment
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The ID/kind of fish used for the experiment
        /// </summary>
        public string FishID { get; private set; }

        /// <summary>
        /// A comment string associated with the experiment
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// The time the experiment object was constructed
        /// </summary>
        protected DateTime StartTime { get; set; }

        /// <summary>
        /// Constructs a new experiment base
        /// </summary>
        /// <param name="folder">The folder in which to save files</param>
        /// <param name="name">The name of the experiment</param>
        /// <param name="fishID">The fish used for the experiment</param>
        /// <param name="experimentLength">The length of the experiment in seconds</param>
        /// <param name="frameRate">The acquisition framerate</param>
        public ExperimentBase(string folder, string name, string fishID, int experimentLength, int frameRate)
        {
            FileSaver = new Saver(folder, name + '_' + fishID, true);
            Name = name;
            FishID = fishID;
            _infoWriter = FileSaver.GetStreamWriter(".info");
            StartTime = DateTime.Now;
            ExperimentLength = experimentLength;
            FrameRate = frameRate;
        }

        /// <summary>
        /// Writes information about the experiment to the info file
        /// </summary>
        public void WriteExperimentInformation()
        {
            WriteExperimentInfo(_infoWriter);
        }

        /// <summary>
        /// Write overview information about the experiment to file
        /// can be overridden for adding specific information
        /// </summary>
        /// <param name="infoWriter"></param>
        protected virtual void WriteExperimentInfo(StreamWriter infoWriter)
        {
            infoWriter.WriteLine("Experiment name: {0}", Name);
            infoWriter.WriteLine("Fish name: {0}", FishID);
            infoWriter.WriteLine("------");
            infoWriter.WriteLine("Comment:");
            infoWriter.WriteLine(Comment);
            infoWriter.WriteLine("------");
            infoWriter.WriteLine("Start date: {0}", StartTime.ToShortDateString());
            infoWriter.WriteLine("Start Time: {0}", StartTime.ToShortTimeString());
            infoWriter.WriteLine("");
        }


        #region IExperiment
        public abstract int SecondsRemaining { get; }
        public abstract string StatusMessage { get; }

        public abstract bool ProcessNext(int frameNumber, BlobWithMoments fish, Image8 fishImage);
        #endregion

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                //write experiment end time to file
                if (_infoWriter != null)
                {
                    _infoWriter.WriteLine("--------------");
                    _infoWriter.WriteLine("Experiment ended:");
                    _infoWriter.WriteLine(DateTime.Now.ToString());
                }
                if (disposing)
                {
                    if(_infoWriter != null)
                    {
                        _infoWriter.Dispose();
                        _infoWriter = null;
                    }
                }

                disposedValue = true;
            }
        }

        ~ExperimentBase()
        {
           Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
