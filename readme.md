# Generating Paths with WFC

<img src="https://user-images.githubusercontent.com/5420431/41688815-959f27fc-74bb-11e8-9b17-fe01003dc777.gif" alt="Example" width="250px" align="right" />

This project is a port of the [WFC algorithm](https://github.com/mxgmn/WaveFunctionCollapse) implemented in Unity. The goal is to generate cyclic paths around obstacles on game levels, using a modified version of the WFC algorithm.

<br />
<br />
<br />

- - -

## WFC Modifications
We want to use the WFC algorithm and modify it to generate cyclic paths around obstacles, in constrained fixed outputs. As a result, we adapt the algorithm to make it work for our needs.

### Fixed outputs
 To be able to generate paths on fixed outputs, we add a few pre-processing steps in order to populate the output with patterns that represent the obstacles' layout on the map. This is similar to setting a fixed pattern one at a time on a blank output until the map's layout is achieved.

### Stretch space
We try to imitate the input path's behavior by introducing a color (that we call stretch space) that represents the space between a path and an obstacles. This leads to paths of arbitrary length from obstacles while making patterns more strict.

### Masks 
We use masks, which are sets of colors, to be able to use any input for any outputs with obstacles of different shapes. We do this by creating patterns from the output map that uses masks to let other tiles go on these patterns. 

### Post-processing
Since paths are generated on images, we then apply a few post-processing steps to convert them into actual paths on a game level. We also enable 3 optional post-processing steps:

#### Path Filtering

#### Path Simplification

#### Path Smoothing


- - -
## Requirements

Basic requirements to use the aglorithm are described below. **Alternatively, loading the "Demo" scene gets you all necessary requirements**.

* Tilemap objects for both the input and output images. You can either instantiate those in 2 separate grid parents or in the same grid parent.

* Instance of the Map Controller script. Can be put in any GameObjet. You will need to reference the input and output tilemaps in Input Target and Output Target attributes.

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
