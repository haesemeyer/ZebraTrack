---
layout: page
title: Experiments
permalink: /experiments/
---

Experimental paradigms are implemented as separate classes that define the what data
should be extracted from the images, what should be written to file and which stimuli
should be generated, if any. All experimental classes are required to implement the
following interface so that the main program can properly interact with them:
{% highlight c# %}
interface IExperiment
{
    //Process the next frame and indicate whether to continue or not
    bool ProcessNext(int frameNumber, Image8 camImage, out IppiPoint? poi);

    //The number of seconds remaining in the experiment
    int SecondsRemaining { get; }

    //A displayable status message about the experiment
    string StatusMessage { get; }

    //Suggested size of the camera buffer
    float? SuggestedBufferSeconds { get; }
}
{% endhighlight %}
The most important item is the ProcessNext method which gets called for every individual
frame by the main program. This method receives the current frame index and associated
camera frame. It should return true while the experiment is still running. Returning false
will terminate the current experiment. An image coordinate passed back via the *poi*
parameter will center the details image of the user interface around those coordinates.
*SuggestedBufferSeconds* can be used to suggest a camera buffer size to the program. In
general camera buffers can be large for open-loop experiments (to ensure no frames are
dropped) but should small for closed-loop experiments (to avoid reacting to stale frames
in the buffer).

### Support classes

Each user experiment should inherit from *ExperimentBase*. This class provides convenient
support for common tasks:

* Storage of general experiment related information such as frame-rate, experiment length,
etc.
* Easy generation of of files with common base-name to store various experiment related data.
* Writing general experiment information, such as name fish type and date of birth or start
and end times into an info file wich can be further customized by sub-classes.

{% highlight c# %}
abstract class ExperimentBase : IExperiment, IDisposable
{
    //..
    //public interface for writing experiment information
    public void WriteExperimentInformation()
    {
        WriteExperimentInfo(_infoWriter);
    }

    /// Write overview information about the experiment to file
    /// can be overridden for adding specific information
    protected virtual void WriteExperimentInfo(StreamWriter infoWriter)
    {
        if (infoWriter != null)
        {
            infoWriter.WriteLine("Experiment name: {0}", Name);
            infoWriter.WriteLine("Fish name: {0}", FishID);
            infoWriter.WriteLine("Fish DOB: {0}", DOB);
            infoWriter.WriteLine("------");
            infoWriter.WriteLine("Comment:");
            infoWriter.WriteLine(Comment);
            infoWriter.WriteLine("------");
            infoWriter.WriteLine("Start date: {0}", StartTime.ToShortDateString());
            infoWriter.WriteLine("Start Time: {0}", StartTime.ToShortTimeString());
            infoWriter.WriteLine("");
        }
    }
    //..
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
}
{% endhighlight %}
Experiments that need fish tracking can derive from *TrackingExperiment* which provides a
method for conveniently creating a fitting tracker object based on general experiment
conditions such as a given frame rate and image resolution.
{% highlight c# %}
abstract class TrackingExperiment : ExperimentBase
{
    protected virtual Tracker90mmDish CreateFishTracker(int imageWidth, int imageHeight, IppiPoint center)
    {
        //Assume fish length around 4 mm
        int approxFishLen = PixelsPermm * 4;
        //Depending on image quality we can't expect the find the tail, assume body length of 2mm with a head-width of 0.8 mm as our area estimate
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

    protected override void WriteExperimentInfo(StreamWriter infoWriter)
    {
        base.WriteExperimentInfo(infoWriter);
        if(infoWriter != null)
        {
            infoWriter.WriteLine("Frame rate: {0} Hz", FrameRate);
            infoWriter.WriteLine("Resolution: {0} px/mm", PixelsPermm);
            infoWriter.WriteLine();
        }
    }

    public override bool ProcessNext(int frameNumber, Image8 camImage, out IppiPoint? poi)
    {
        //create tracker if necessary
        if (Tracker == null)
            Tracker = CreateFishTracker(camImage.Width, camImage.Height, new IppiPoint(camImage.Width / 2, camImage.Height / 2));
        poi = null;
        return true;
    }
}
{% endhighlight %}
These support classes make it very easy to quickly write an experimental class that writes
fish position and heading to a file for every camera frame:
{% highlight c# %}
class TrackAndWrite : TrackingExperiment
{
    // The last frame that was processed
    int _lastFrame = 0;

    // The total number of frames in the experiment
    int _totalFrames;

    // The file writer to save experiment information
    StreamWriter _trackWriter;
    
    public TrackAndWrite(int expSeconds, int frameRate, int pxPERmm, string folder, string name, string fishID) : base(folder, name, fishID, expSeconds, frameRate, pxPERmm)
    {
        _totalFrames = ExperimentLength * FrameRate;
        if(FileSaver != null)
            _trackWriter = FileSaver.GetStreamWriter(".track");
        SuggestedBufferSeconds = 2;
    }

    public override string StatusMessage{ get{ return "";}}

    public override int SecondsRemaining
    {
        get
        {
            return (int)(ExperimentLength - _lastFrame / FrameRate);
        }
    }
    
    public override bool ProcessNext(int frameNumber, Image8 camImage, out IppiPoint? poi)
    {
        base.ProcessNext(frameNumber, camImage, out poi);
        _lastFrame = frameNumber;
        if (frameNumber >= _totalFrames)
            return false;
        var fish = Tracker.Track(camImage);
        if (fish != null)
            poi = new IppiPoint(fish.Centroid.x, fish.Centroid.y);
        if (_trackWriter != null)
        {
            if (fish != null)
                _trackWriter.WriteLine("{0}\t{1}\t{2}\t{3}", frameNumber, fish.Centroid.x, fish.Centroid.y, fish.Angle);
            else
                _trackWriter.WriteLine("NaN\tNaN\tNaN\tNaN");
        }
        return true;
    }

    //Disposal

    }
}
{% endhighlight %}