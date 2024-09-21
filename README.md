# Tool for adapting .dmi files
This tool is designed to edit .dmi files with the potential to adapt them to any shape, size, offsets and other parameters imposed on all selected files through the config editor, storing tabular pixel data and their offsets.

## The following were used for implementation:
* Interface [WPF](https://github.com/dotnet/wpf) framework.
* Framework for processing DMI files [DMI Sharp](https://github.com/bobbah/DMISharp)

### For .NET 7
These tool require Visual Studio 2022(v17.7), Visual Studio 2022 for Mac (v17.6) to build, test, and deploy, and also require the .NET 7 SDK.

[Get a free copy of Visual Studio 2022 Community Edition](https://www.visualstudio.com/wpf-vs)

[.NET 7 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)

## Usage

### Pages
The program has 3 pages:
* Home - [WIP] Page for working with the workspace, saving the latest user settings, loading presets.

* Edit - Page for editing configs by changing pixels on preview canvases.
* Data - Page for processing files for selected configs.

### Home
[WIP]

### Edit

### Data

## Delelopment
The program is divided into controllers, resources, models and auxiliary classes for more convenient access to the code. Inside the code there are divisions into regions for even more convenient navigation and separation.

### View Models
In addition to Views that contain WPF pages, they use:
* Dashboard View Model - [WIP]
* States Editor View Model - a model for implementing changes to config selection, preview states, working with files and loading them, saving configs and storing State Item's.
* Data View Model - a model for implementing changes to the selected config, displaying Tree View with all selected files that will be displayed and processed in the future.
* Settings View Model - a model for settings of the current theme.
* Main Window View Model - a model for navigating through pages.

### Models
* Config Item - a model for a config that stores the path to a table for quick access.
* State Item - a model for a state with a .DMI file that stores a preview, path, file name and state name.
* Data Image State - a model for working with preview images that superimpose "preview, landmark and overlay" states on top of each other for further visualization.
* State Edit Type - model of enumeration of various types:
** StateEditType - Editing mode of the main preview window
** StateQuantityType - Type of state editing.
** StateImageType - Type of preview element.
** StateImageSideType - Side of the preview element.
** SelectMode - Current mode of the pixel moving tool.
** StatusBarType - Type of the status bar element.
** StatePreviewType - Selected preview type for overlaying states.

### Controllers
* Environment Controller - Workspace and environment initialization controller.
* Draw Controller - Image processing controller that stores all functions for drawing on the canvas and editing pixels.
* Editor Controller - Controller of current canvas editing modes and button logic.
* Mouse Controller - Controller for processing pressed mouse buttons and finding the cursor position on the canvas.
* States Controller - Controller that stores information about current states, modes, configs and statuses of elements used by the entire program.
* Status Bar Controller - Controller of information displayed on the status bar.

### Processors
DMI State Processor - File processor from these states for selected configs.

### Helpers
Image Encoder - DMI State processor in Writeable Bitmap for display and editing on the "Edit" page.
Files Searcher - Search engine for the correct directory.

## Contact
Questions or want to help with the implementation? You can contact me on discord: **PHANTOMRU** (don't confuse the nickname on GitHub, there "m == rn" xdd)






# License
Unless otherwise mentioned, the samples are released under the [MIT license](https://github.com/PhantornRU/AdaptiveSpritesDMItool/blob/main/LICENSE)
