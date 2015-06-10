# Houdini Engine for Unity
Houdini Engine for Unity is a Unity plug-in that allows deep integration of
Houdini technology into Unity through the use of Houdini Engine.

This plug-in brings Houdini's powerful and flexible procedural workflow into
Unity through Houdini Digital Assets. Artists can interactively adjust the
asset's parameters inside Unity, and use Unity geometries as an asset's inputs.
Houdini's procedural engine will then "cook" the asset and the results will be
available right inside Unity.

The easiest way for artists to access the plug-in is to download the latest
production build of Houdini or the [FREE Houdini Apprentice Learning
Edition](http://www.sidefx.com/index.php?option=com_download&task=apprentice&Itemid=208)
and install the Unity plug-in along with the Houdini interactive software.
Houdini Digital Assets created in either Houdini or Apprentice can then be
loaded into Unity through the plug-in. A growing library of Digital Assets for
use in Unity will be available at the [Orbolt Smart 3D Asset
Store](http://www.orbolt.com/unity).

For more information:

* [Houdini Engine for Unity](http://www.sidefx.com/unity)
* [FAQ](http://www.sidefx.com/index.php?option=com_content&task=view&id=2618&Itemid=393)
* [SideFX Labs](http://labs.sidefx.com)

For support and reporting bugs:

* [SideFX Labs forum](http://www.sidefx.com/index.php?option=com_forum&Itemid=172&page=viewforum&f=46)
* [Bug Submission](http://www.sidefx.com/index.php?option=com_content&task=view&id=768&Itemid=239)

## Supported Unity versions
Currently, the supported Unity versions are:

* 4.6
* 5.1

The plug-in works with the 64-bit/32-bit Windows Unity Editor, but only the 64-bit Max OS X Unity 5+ Editor.

## Installing from Source
1. Fork this repository to your own Github account using the Fork button at the top.
1. Clone the forked repository where you'd normally save your Unity projects.
1. Download and install the correct build of Houdini. You must have the exact build number and version as HOUDINI_MAJOR, HOUDINI_MINOR, and HOUDINI_BUILD int values in Assets/Houdini/Scripts/HoudiniVersion.cs. You can get the correct build from: http://www.sidefx.com/index.php?option=com_download&Itemid=208 (you might need to wait for the build to finish and show up if you're updating to the very latest version of the plugin)
1. Open Unity and open a new project by selecting the folder containing your cloned repository (the folder containing the Assets folder).
1. Restart Unity.
1. Ensure Houdini Engine loaded successfully by going to the "Houdini Engine" top menu and selecting "Installation Info" and making sure all the versions match.
