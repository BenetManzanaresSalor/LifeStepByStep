# Life Step by Step
A simple life simulation game developed in Unity, where the player can watch a group of entities move, grow, search, do pathfinding, eat, reproduce and die. Additionally, you can observe some statistics of the population and change some values of its behaivour affecting its survival.
It uses the 3D cubic terrain (procedurally generated) and 2D map from the Lost Cartographer Pack (https://github.com/BenetManzanaresSalor/LostCartographerPack), another project created by Benet Manzanares Salor.


# Intructions
The game initiates at the main menu, where you can start simulation, see instructions or change settings.
Once you select the start option, you will use the camera and world controls described below.
Below that descriptions you can find the settings menu instructions.

## Controls
### Camera
You can change the point of view to the world with:
* **W, A, S, D**: Move the X-Z plane, limited by the edges of the terrain and collisions.
* **Spacebar / Shift**: Move up and down. Limited by collisions with the ground and a height maximum.
* **Hold right click**: Rotate around the Y and X axis. If the right click is not pressed rotation is disabled, allowing to use the UI.

### World
You can control the world with:
* **E**: Play or pause the simulation. All the entities and foods will stop their life processes.
* **R**: Regenerate the world. The terrain and its content is deleted and, then, everything is regenerated. The positions and attributes of the entities will change and, if the UseRandomSeed setting is enabled, so will the terrain.
* **T**: Toggle statistics. Change the visibility of the statistics panel, which shows information about the entities and foods.
* **Left click**: Select an entity or a food clicking at the 3D model or the area where is placed. Once the object is clicked a panel shows information about it, like energy and state.
* **Escape**: Toggle between simulation and main menu. Only works if the simulation is started.

## Settings
If the settings option at the main menu is selected, the player can change the following attributes about world and entities.

### World
* **Use random seed**: If the seed changes every time the world is regenerated.
* **Seed**: If UseRandomSeed setting is disabled the world generation will use this number as seed, creating always the same terrain.
* **Entity probability**: Probability of create an entity at a terrain position. The probability is maxed at 33,3% because shares probability with the other possible world objects.
* **Food probability**: Probability of create a food at a terrain position. The probability is maxed at 33,3% because shares probability with the other possible world objects.
* **Obstacle probability**: Probability of create an obstacle at a terrain position. The probability is maxed at 33,3% because shares probability with the other possible world objects.

### Entity
* **Death by age**: If each entity dies when it has lived a long time (about 2 or 3 minutes).
* **Show state icons**: If each entity shows its status with icons above it. 
* **Show energy bar**: If each entity shows its energy with a bar above it. The bar will become red if the energy is problematic.
* **Show target rays**: If each entity shows a red ray to its target.
* **Problematic energy**: Energy value that is considered problematic. Any lower value will cause entities to search food.
* **Search radius**: The radius (number of cells in each direction) in which each entity will search to eat or reproduce.

All this settings are applied when the simulation is running.


# Implementation


## MainController


### MainUI


## GameController


### GameUI


### World


#### Food


#### Entity


# Future improvements
