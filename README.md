# Bezier Curve

## Overview

This project is a 3D Bezier Curve tool for use in Unity. Its purpose is to visualize and edit a Bezier curve with an arbitrary number of segments in the 3D scene view. The main code logic is under Assets > Scripts.

## Features
- Move anchors and handles by hand using the mouse
- Handles automatically react to adjustments, maintaining smooth curvature
- Add and delete anchors by hand with mouse clicks and the shift/control keys
- Read positions, tangents, and normals from the curve by time and by distance
- Toggle closed loop status for the curve
- Cache and interpolate frames (position, tangent, and normal at a given curve location) for speedy retrieval
- Minimize exaggerated rotation of normals caused by naive normal calculations
- Provide simple public APIs for other Unity scripts to access curve features

## Setup and Installation
You can clone the repo for this project using the url https://github.com/joshua926/BezierCurveTool or you can download the BezierCurveTool.unitypackage in the root directory of the repo and add it to your Unity Project. If you add the asset package to your own project, you must have a recent version of the Burst package installed.

## Instructions
- Add the Curve script to a GameObject to see a curve rendered in the scene view.
- Click and drag with the mouse on any anchors or handles along the curve to move them.
- Control click on an anchor to delete it.
- Shift click a position on the curve to insert an anchor.
- Shift click nearby the curve to add an anchor to the end.
- In the inspector, set the 'Cache Frames Per Segment' field to increase or decrease the accuracy of cached frames.
- In the inspector, you can toggle the 'Preview Settings' foldout to alter how the curve is rendered in the scene view. This has no impact in builds.
- You can reference the curve in other scripts. There are several public methods you can call to add and delete anchors, move anchors, move handles, and get positions, tangents, and normals at points along the curve.

## Design
There are several code objects the Curve MonoBehaviour class depends on internally. The Anchor struct represents an anchor and its handles in the curve along with logic related to setting and aligning handles. The Curve class itself contains an array of Anchors. The Segment struct is another representation of the points that make up the curve. It is used to handle the fundamental Bezier math involved in calculating positions, tangents, accelerations, etc. The Frame struct represents a reference frame along the curve that includes its position, tangent (forward), and normal (up). The Cache class is an abstraction to isolate all logic involved in calculating and interpolating between frames.

## Known Issues
- When curve has isLoop and autoSetHandles set to true, sometimes inserting an anchor in a segment adds it to a nearby segment instead.
- When curve has isLoop set to true, the start and end normals don't quite match exactly.
