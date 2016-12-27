---
layout: default
---

## Overview
[ZebraTrack](https://github.com/haesemeyer/ZebraTrack) is as a lightweight program
to track freely swimming larval zebrafish in realtime and perform behavioral experiments.
A simple [User interface]({{ github.site.url }}/user_interface/) relays a high-speed
camera feed and allows selecting experimental parameters as well as running experiments.
{% include image.html
    img="assets/scr/UI_CamImage.png"
    title="User interface"
    width="400"
    caption="User interface" %}
Different experimental paradigms can be implemented by extending [experiment]({{ github.site.url }}/exp_class/)
base classes. This allows to flexibly
implement both open loop and closed loop experiments. Currently a very simple open-loop
"experiment" which writes position and heading information to file is implemented as and
example.

## Data acquisition and processing
Currently data acquisition is limited to camera link cameras via a National Instruments
frame grabber. However, support for general NI Vision and Point Grey cameras is planned
for the near future.

## Dependencies
ZebraTrack depends on {% include github-link.html link="haesemeyer/mhapi" name="MHApi" %}
as well as the [Intel IPP](https://software.intel.com/en-us/intel-ipp). For IPP
licenses can be obtained for free for personal and educational use. Currently, only
IPP versions 7.0 and 7.1 have been tested, which can still be downloaded from the
license center. The examples for those versions also include the necessary Visual Studio
solution for importing the dependencies. It is planned to more conveniently include
these imports in MhApi and test against newer versions of IPP.
