---
layout: page
title: User Interface
permalink: /user_interface/
---

The user interface of ZebraTrack shows a live camera feed on the right (Preview mode) after
startup. After initial background model generation by the tracker, if there is a trackable
fish object, a small image below will display a closeup of the tracked fish.  

On the upper left general information about the experiment can be entered, such as name,
 line identification or fish date of birth. The tab-control below allows selecting an
experiment type as well as setting of parameters specific to the chosen paradigm.  

The two buttons on the bottom left start an experiment (and exit Preview mode) and
restart the preview, including fresh background generation.
{% include image.html
    img="assets/scr/UI_CamImage.png"
    title="User interface"
    width="500"
    caption="Camera feed" %}
During Preview mode, it is possible to select different tracker intermediate images for
display instead of the camera feed via the combo-box below the main image.
{% include image.html
    img="assets/scr/Views.png"
    title="Tracking views"
    width="500"
    caption="Preview tracking views" %}
These views are useful to trouble-shoot tracker settings such as turnover of the background
model or the threshold value for foreground extraction.

## Experiment user interface hookup
Individual experiment types are presented on the user interface via tabs in the tab-control
on the left. Each tab should host a view specific to the given experiment which presents
controls to collect information necessary to run the experiment.  
The ZebraTrack application user interface is built using the
[View-ViewModel](http://www.markwithall.com/programming/2013/03/01/worlds-simplest-csharp-wpf-mvvm-example.html)
paradigm with individual Views (UserControls) presenting data to and collecting data from
the user while the ViewModel stores that data.  
All experiment view models are required to implement the IExperimentVM interface which
requires the implementation of one method, CreateExperiment. This method should create an
[Experimental class]({{ site.github.url }}/experiments/) based on the user selections in
the tab. 
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
        //Use information entered by user to create our experiment
    }
}
{% endhighlight %}
At the same time the currently active tab determines which experiment is run when the user
presses the start button. To make this possible the MainViewModel exposes a property
of type IExperimentVM which is used to create the appropriate experimental paradigm when
the start button is pressed.
{% highlight c# %}
unsafe class MainViewModel : ViewModelBase
{
    //...
    void Start()
    {
        if (IsRunning)
            return;
        //create experimental class
        ExperimentBase exp = null;
        if (ExperimentViewModel != null)
            exp = ExperimentViewModel.CreateExperiment(ExperimentName, FishName);
        else
        {
            System.Diagnostics.Debug.WriteLine("No experiment view model was active. Exiting.");
            return;
        }
        //...
    }
    //...
    public static IExperimentVM ExperimentViewModel { get; set; }
    //...
}
{% endhighlight %}
Whenever the user switches to a different tab in order to select a different experiment type
the Loaded event will be called on the corresponding view. We hook to this event in order
to update the MainViewModel's knowledge of the currently active experiment viewmodel:
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

