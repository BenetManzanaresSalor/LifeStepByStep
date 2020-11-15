# Lost Cartographer Pack
This project implements a generic 3D terrain and 2D map system written in C# using Unity game engine, designed to allow specific implementation of a terrain or a map using inheritance.
As examples, includes two procedural generated terrains and the corresponding maps, and they are also ready for extension using inheritance. One of the terrains implements a standard mesh and the other creates a Minecraft-like mesh, both sharing the same procedural generation based on Unity's perlin noise.


## Examples : Use what is already implemented
If you want to use this pack without touch code you can use the examples as reference:
* **LC_Terrain_Example**: Uses the LC_TerrainInstanciable and LC_MapInstanciable components fully configured.
* **LC_CubeTerrain_Example**: Uses the LC_CubeTerrainInstanciable and LC_CubeMapInstanciable components fully configured.


## Extend or modify
If you want to extend or modify this package, inheritance of the LC_Terrain, LC_CubeTerrain, or LC_Map classes is recommended.
Instead, if you require further modification, you can inherit the LC_GenericTerrain or LC_GenericMap root classes.

The terrain and map systems are described below.


## Terrains
Any terrain of the pack inherits from the abstract class LC_GenericTerrain, which defines a terrain as group of chunks that contain cells. Chunk and cell types are class parameters that must be children of the LC_Chunk and LC_Cell classes respectively.

Chunks can be created statically or dinamically:
* **Static**: The chunks will be created at the start as a square around the reference position, using the ChunkRenderDistance setting.
* **Dynamic**: The system will always create the chunks around the reference position while moving (also using the ChunkRenderDistance setting).

Also, chunks can be created synchronously or parallely:
* **Synchronous**: The chunk cells, mesh, and gameobject are created consecutively at the main thread.
* **Parallel**: The chunk cells and mesh data are created in a parallel C# Task. After, the game object and the mesh component are created at the main thread.

Last but not least, to reduce the effect on performance, you can set the maximum time that should elapse in the Update method.
This maximum is checked by the InMaxUpdateTime method at each iteration of the dynamic chunk loading and the build of parallely loaded chunks loops.


### LC_Terrain
An abstract and generic class child of LC_GenericTerrain which generates procedurally generated the terrain with a standard mesh for each chunk. The procedural generation is done with height maps created by the PerlinNoiseMap method (implemented in LC_Math class) which uses Unity's Mathf.PerlinNoise.
This class is ready for inheritance to use the desired chunk and cell types.

Additionally, the terrain can be coloured using different techniques:
* **Default UVs**: Uses the UVs computed during the mesh generation.
* **Height discrete**: Using the LC_Shader and the list of colors setted at the inspector, applies one color for each pixel of the terrain based on its height.
* **Height continuous**: Using the LC_Shader and the list of colors setted at the inspector, applies a color interpolation for each pixel of the terrain based on its height.

An example of this terrain can be found in the Examples folder, which uses the LC_TerrainInstanciable class.


### LC_CubeTerrain
An abstract and generic class child of LC_Terrain that gives the mesh a Minecraft-like style, using cubes for each cell. The heights of each cells are rounded to integers, and a generic split and merge algorithm (implemented in LC_Math class) can be used to reduce the complexity of the meshes.
This class is ready for inheritance to use the desired chunk and cell types.
An example of this terrain can be found in the Examples folder, which uses the LC_CubeTerrainInstanciable class.


## Maps
Any map of the pack inherits from the abstract class LC_GenericMap, which represents a region of a terrain in a RenderTexture. Terrain, chunk and cell types are class parameters that must be children of the LC_GenericMap, LC_Chunk and LC_Cell classes respectively.
The child class must implement the following methods:
* **GetReferencePos**: Used to determine the center of the region to map (usually the player's position). The size and resolution of the region represented are configurable.
* **GetColorPerCell**: Receives a cell and returns the color that will represent it on the map.

Additionally, if the user wants to map a region which is not loaded at the terrain, the MapNonLoadedChunks setting can be enabled. This option causes the terrain to load (but not build the mesh or gameobject) the chunks needed for the map.
Combined with a reference position this function can allow a map with free movement.

Also, just like in terrain generation, you can set the maximum time that should elapse in the Update method. This maximum is checked by the InMaxUpdateTime method at each iteration of the map update loop. If the map update stops for this reason, in the next Update call will continue from the last updated pixel.


### LC_Map
An abstract and generic class child of LC_GenericMap which implements GetColorPerCell with techniques similiar to LC_Terrain:
* **Height discrete**: Using the list of colors setted at the inspector, applies one color for each pixel of the terrain based on its height.
* **Height continuous**: Using the list of colors setted at the inspector, applies a color interpolation for each pixel of the terrain based on its height.

This class is ready for inheritance to use the desired terrain, chunk and cell types.
Both examples in the Examples folder use children of this class, specifically, the LC_MapInstanciable and LC_CubeMapInstanciable components.