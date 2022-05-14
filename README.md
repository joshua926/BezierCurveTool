# Bezier Curve

## Overview

This project is a 3D Bezier Curve tool for use in Unity. Its purpose is to visualize and edit a Bezier curve with an arbitrary number of segments in the 3D scene view.

## Features
- Move anchors and handles by hand with mouse
- Handles automatically react to adjustments, maintaining smooth curvature
- Add and delete anchors by hand with mouse clicks and the shift/control keys
- Read positions, tangents, and normals from the curve by time and by distance
- Toggle closed loop status for the curve
- Cache and interpolate frames (position, tangent, and normal at a given curve location) for speedy retrieval
- Minimize exaggerated rotation of normals caused by naive normal calculations
- Provide simple public APIs for other Unity scripts to access curve features

## Setup and Installation
You can clone the repo for this project using the url https://github.com/joshua926/BezierCurve or you can download the BezierCurve.unitypackage in the root directory of the repo and add it to your Unity Project. If you add the asset package to your own project, you must have a recent version of the Burst package installed.

## Instructions
Add the 