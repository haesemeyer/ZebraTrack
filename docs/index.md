---
layout: default
---

## Overview
[ZebraTrack](https://github.com/haesemeyer/ZebraTrack) is as a lightweight program
to track larval zebrafish in realtime and perform behavioral experiments.
{% include image.html
    img="assets/scr/UI_CamImage.png"
    title="User interface"
    width="300"
    url="/user_interface/"
    caption="User interface" %}

## Dependencies
ZebraTrack depends on [MhApi](https://github.com/haesemeyer/mhapi) as well as the
[Intel IPP](https://software.intel.com/en-us/intel-ipp) for which licenses can be
obtained for free for personal and educational use. Currently, only IPP versions
7.0 and 7.1 have been tested, which can still be downloaded from the license center.
The examples for those versions also include the necessary Visual Studio solution for
importing the dependencies. It is planned to more conveniently include these imports
in MhApi and test against newer versions of IPP.
