---
layout: default
---

## Overview
{% include github-link.html link="haesemeyer/ZebraTrack" name="ZebraTrack" %}
is as a lightweight program to track freely swimming larval zebrafish in realtime
and perform behavioral experiments.
A simple [User interface]({{ site.github.url }}/user_interface/) relays a high-speed
camera feed and allows selecting experimental parameters as well as running experiments.
{% include image.html
    img="assets/scr/UI_CamImage.png"
    title="User interface"
    width="400"
    caption="User interface" %}
Different experimental paradigms can be implemented by extending [experiment]({{ site.github.url }}/experiments/)
base classes. This allows to flexibly
implement both open loop and closed loop experiments. Currently a very simple open-loop
"experiment" which writes position and heading information to file is implemented as an
example.

## Data acquisition and processing
{% include image.html
    img="assets/data/Tracking_Example.png"
    title="Behavior example"
    width="800"
    caption="Behavior example (14s of data at 700 Hz)" %}
Currently data acquisition is limited to camera link cameras via a National Instruments
frame grabber. However, support for general NI Vision and Point Grey cameras is planned
for a future release.

Using multi-threading data is continuously acquired. On each frame the fishes position
and heading angle are determined within less than 1 ms. This is largely possible because
tracking is restricted to an image region centered around the last fish position. The size
of the image region is determined heuristically based on imaging framerate and image
resolution together with known behavioral parameters of larval zebrafish.  
Larval zebrafish are identified as a foreground object by subtracting a background model
of the enclosure. Since lighting conditions as well as water levels in the behavioral arena
can change over the course of an experiment, the background model is continuously updated.
To avoid having a stationary fish enter the background model, the current fish location is
excluded from all background updates.



## Dependencies
ZebraTrack depends on {% include github-link.html link="haesemeyer/mhapi" name="MHApi" %}
as well as the [Intel IPP](https://software.intel.com/en-us/intel-ipp). For IPP
licenses can be obtained for free for personal and educational use. Currently, only
IPP versions 7.0 and 7.1 have been tested, which can still be downloaded from the
license center. The examples for those versions also include the necessary Visual Studio
solution for importing the dependencies. It is planned to more conveniently include
these imports in MhApi and test against newer versions of IPP.
