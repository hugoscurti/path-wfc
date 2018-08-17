# Generating Paths with WFC

<img src="https://user-images.githubusercontent.com/5420431/41688815-959f27fc-74bb-11e8-9b17-fe01003dc777.gif" alt="Example" width="250px" align="right" />

This project is a port of the [WFC algorithm](https://github.com/mxgmn/WaveFunctionCollapse) implemented in Unity. The goal is to generate cyclic paths around obstacles on game levels, using a modified version of the WFC algorithm.

<br />
<br />
<br />

- - -

## WFC Modifications
Our goal was to use the WFC algorithm and modify it to generate cyclic paths around obstacles, in constrained fixed outputs. As a result, we adapted the algorithm to make it work for our needs.

### Fixed outputs
 To be able to generate paths on fixed outputs, we added a few pre-processing steps in order to populate the output with patterns that represent the obstacles' layout on the map. This is similar to setting a fixed pattern one at a time on a blank output until the map's layout is achieved.

### Stretch space
We tried to replicate the input path's behavior by introducing a color (that we call stretch space) that represents the space between a path and an obstacle. This leads to paths of arbitrary length from obstacles while making patterns more strict.

### Masks 
We used masks, which basically are sets of colors, to be able to use any input in conjunction with any output with obstacles of different shapes. We do this by creating patterns from the output map that uses masks on non-obstacle areas to let other tiles go on these patterns. 

### Post-processing
Since paths are generated on images, we then enable a few post-processing steps to convert them into actual usable paths on a game level. We also enable 3 optional post-processing steps:

#### Path Filtering
Enables the option to remove paths that are smaller than a certain threshold (e.g. small paths that don't go around an obstacle). The threshold can be adjusted by users in the editor.

#### Path Simplification
We use the [Ramer-Douglas-Peucker algorithm](https://en.wikipedia.org/wiki/Ramer%E2%80%93Douglas%E2%80%93Peucker_algorithm) in order to remove frequent redundant vertices to simplify the resulting path. The algorithm goes through vertices and remove those that are too close to the line segment formed by its surrounding vertices, only if the resulting line doesn't go through an obstacle. The threshold used to determine wether a vertex should be removed or not can also be specified by users in the editor. 

#### Path Smoothing
Since we are working in a pixelated 2d environment, generated paths tend to be jagged. We enable path smoothing using [Chaikin's curve generation algorithm](http://graphics.cs.ucdavis.edu/education/CAGDNotes/Chaikins-Algorithm/Chaikins-Algorithm.html). We let users specify the number of iterations to apply, up to 5 iterations.


- - -
## Requirements

Basic requirements to use the aglorithm are described below. **Alternatively, loading the "Demo" scene gets you all necessary requirements**.

* Sprite renderers for both the input and output images. A box collider 2d component is required on the output if you want to select specific patterns during or before execution. 

* Instance of the Map Controller script. Can be put in any GameObjet. You will need to reference the input and output gameobjects in Input Target and Output Target attributes.

    * Alternatively, you can set a background sprite renderer which, if set, will be put behind the output renderer so that it acts as a background. This is only useful when dealing with masks, since masks are visually represented by a transparent color.

* Instance of the Path Overlap Controller script. Can be put in any GameObject. You will need to reference the Map Controller script in the Map Controller attribute.

* For post processing, an Instance of the Post Processing Controller script is necessary. GameObjects for Map, Obstacles and Paths are needed and should be referenced in the Containers attribute.  Map should be a Plane GameObject, whereas Paths and Obstacles can be empty GameObjects. Path Overlap Controller must be referenced in the Path Overlap Controller attribute. Cube prefab should be referenced in the Obstacle attribute and a material for line renderers should be referenced in Line Material attribute.


## Usage

### Main Execution

* In the Map Controller Script, Select an input and an output via the input/output selectors and click on "Load Maps";

* In the Path Overlap Controller Script, select the desired attributes in *Model Attributes* and click on "Instantiate";

* Select the desired attributes in *Execution* and click on "Play";

    * Alternatively, you can execute the first propagate independently before clicking "Play", however this is mostly to see the intermediate result after the first propagation.

* At any time during the execution, you can click on "Pause" to pause the execution, and resume it by clicking again on "Play".

* Click on "Reset" after an execution to reset values and start another execution.

### Post-processing

* After an execution is successful, in the Post Processing Controller script, select the desired attributes in *Attributes* and click on *Generate Paths*.

* In the Agent Controller Script, you can select a path in the dropdown list and click on *Set agent* to instantiate an agent that will walk through the selected path.

- - -
## Known issues

* Using crossing paths as input works relatively well when using the main algorithm. However, extracting paths from the image into line renderers generate errors, since the algorithm doesn't know how to handle crossing paths yet.

* Using Instantiate doesn't deallocate the last instantiated Path Overlap Model, therefore making the algorithm stack up in memory, especially for large maps.

* In Post Processing Controller, interrupting a path generation with *Clear* doesn't stop the path generation Jobs. Moreover, starting a new one while the previous one is not finished will result in NativeArray memory leak errors.


- - -
## Credits

* Initial WFC algorithm used can be found [here](https://github.com/mxgmn/WaveFunctionCollapse).

* All the .map files used in the output folders come from [Moving AI](http://movingai.com/benchmarks/).
