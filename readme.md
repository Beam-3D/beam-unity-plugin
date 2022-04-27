# Beam Unity plugin

## Prequisites

### Web setup

Follow the instructions [here](./web-readme.md)

### Unity prequisites

You will need to install glTFFast first in order to use the Beam plugin. You can follow the instructions here https://github.com/atteneder/glTFast#installing

## Installation

Simply add the repository from a Git URL via package manager using `https://github.com/Beam-3D/beam-unity-plugin.git`

## Usage

Once you've added the plugin, you can start using it from the 'Beam' entry which is now present in the menu bar.

You'll need to log in with the same account used in the web and make sure the 'demo' environment is selected from the dropdown in the bottom of the window.

Then select the project you created in the web setup step and you should see all your slots ready to place.

### Known issues / limitations

- The terminology of Unit & Slot are used inconsistently between the web and the Unity plugin. They both mean the same thing.
- Currently no support for instantiated prefabs. The BeamUnitInstance scripts must be present when the scene starts or the 'StartSession' command is issued from the client.
- Analytics requires colliders. We automatically generated colliders for anything Beamed in so we can correctly capture gaze and interaction based analytics. This can potentially be slow so if you do have performance issues on load we reccomend using a loading screen or handling this seperately.
- Plugin upgrades may cause loss of placed units. As the plugin is currently in Alpha, there are still potential breaking changes which could cause issues when upgrading the version of the plugin
- The default quality level sometimes defaults to 720p but the default when uploading assets to the web is 1080p. To fix this just make sure your LOD settings are both set to 1080p unless you are actively using LOD.
