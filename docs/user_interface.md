---
layout: page
title: User Interface
permalink: /user_interface/
---

The user interface of ZebraTrack shows a live camera feed on the right (Preview mode) after
startup. After initial background model generation by the tracker if there is a trackable
fish object a small image below will display a closeup of the tracked fish.  
On the upper left general information about the experiment can be entered, such as name
line identification or fish date of birth. The tab-control below allows selecting an
experiment type as well as setting of parameters specific to the chosen paradigm.  
The two buttons on the bottom left start an experiment (and exit Preview mode) and
restart the preview, including fresh background generation.
{% include image.html
    img="assets/scr/UI_CamImage.png"
    title="User interface"
    width="400"
    caption="Camera feed" %}
During Preview mode, it is possible to select different tracker intermediate images for
display instead of the camera feed via the combo-box below the main image.
{% include image.html
    img="assets/scr/Views.png"
    title="Tracking views"
    width="400"
    caption="Preview tracking views" %}
These views are useful to trouble-shoot tracker settings such as turnover of the background
model or the threshold value for foreground extraction.

## Experiment user interface hookup
{% highlight c# %}
interface IExperimentVM
{
    ExperimentBase CreateExperiment(string name, string fishID);
}

class TWExperimentViewModel : ViewModelBase, IExperimentVM
{
    //..
    public ExperimentBase CreateExperiment(string name, string fishID)
    {
        //..
    }
}
{% endhighlight %}

{% highlight c# %}
public partial class TWExperimentView : WindowAwareView
{
    TWExperimentViewModel _viewModel;

    public TWExperimentView()
    {
        InitializeComponent();
        _viewModel = ViewModel.Source as TWExperimentViewModel;
    }

    private void WindowAwareView_Loaded(object sender, RoutedEventArgs e)
    {
        //Make the MainViewModel aware of the fact
        //that this experiment type has been selected
        MainViewModel.ExperimentViewModel = _viewModel;
    }
}
{% endhighlight %}

{% highlight c# %}
unsafe class MainViewModel : ViewModelBase
{
    //...
    public static IExperimentVM ExperimentViewModel { get; set; }
    //...
}
{% endhighlight %}

