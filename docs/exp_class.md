---
layout: page
title: Experiments
permalink: /experiments/
---


All experimental classes must implement the following interface so that the main
program can properly interact with them:

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