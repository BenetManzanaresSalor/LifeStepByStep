# Life Step by Step
A simple life simulation game developed in Unity, where the player can watch a group of entities move, grow, search, do pathfinding, eat, reproduce and die.
Additionally, you can observe some statistics of the population and change some values of its behaivour affecting its survival.
It uses the 3D cubic terrain (procedurally generated) and 2D map from the Lost Cartographer Pack (https://github.com/BenetManzanaresSalor/LostCartographerPack), another project created by Benet Manzanares Salor.

![Game screenshot](https://user-images.githubusercontent.com/47823656/99193914-3d220600-277c-11eb-80cc-b05e001ee4a3.png)

# Instructions
The game initiates at the main menu, where you can start simulation, see instructions or change game settings.
Once you select the start option, you will use the camera and world controls described below.
Below that descriptions you can find the settings menu instructions.

## Controls
### Camera
You can change the point of view using this controls:
* **W, A, S, D**:
	Move the X-Z plane, limited by the edges of the terrain and collisions.
* **Spacebar / Shift**:
	Move up and down. Limited by collisions with the ground and a height maximum.
* **Hold right click**:
	Rotate around the Y and X axis. If the right click is not pressed rotation is disabled, allowing to use the UI.

### World
You can control the world using this controls:
* **E**:
	Play or pause the simulation. All the entities and foods will stop their life processes.
* **R**:
	Regenerate the world. The terrain and its content is deleted and, then, everything is regenerated.
	The positions and attributes of the entities will change and, if the UseRandomSeed setting is enabled, so will the terrain.
* **T**:
	Toggle statistics. Change the visibility of the statistics panel, which shows information about the entities and foods.
* **Left click**:
	Select an entity or a food clicking at the 3D model or the area where is placed.
	Once the object is clicked a panel shows information about it, like energy and state.
* **Escape**:
	Toggle between simulation and main menu. Only works if the simulation is started.

## Game settings
If the settings option at the main menu is selected, the player can change the following attributes about world and entities.

### World
* **Use random seed**:
	If the seed changes every time the world is regenerated.
* **Seed**:
	If UseRandomSeed setting is disabled the world generation will use this number as seed, creating always the same terrain.
* **Entity probability**:
	Probability of create an entity at a terrain position.
	The probability has a maximum of 33,3% because it shares probability with the other possible world objects.
* **Food probability**:
	Probability of create a food at a terrain position.
	The probability has a maximum of 33,3% because it shares probability with the other possible world objects.
* **Obstacle probability**:
	Probability of create an obstacle at a terrain position.
	The probability has a maximum of 33,3% because it shares probability with the other possible world objects.

All these settings are applied when the world is regenerated.

### Entity
* **Death by age**:
	If each entity dies when it has lived a long time (about 2 or 3 minutes).
* **Show state icons**:
	If each entity shows its status with icons above it. 
* **Show energy bar**:
	If each entity shows its energy with a bar above it. The bar will become red if the energy is problematic.
* **Show target rays**:
	If each entity shows a red ray to its target.
* **Problematic energy**:
	Energy value that is considered problematic. Any lower value will cause entities to search food.
* **Search radius**:
	The radius (number of cells in each direction) in which each entity will search to eat or reproduce.

All these settings are applied when the simulation runs.


# Implementation
This project is implemented with a hierarchical structure, but conceptually the classes can also be divided between Main and Game.
A diagram showing the relationships between the main classes is below:
![ClassDiagram](https://user-images.githubusercontent.com/47823656/99194852-807f7300-2782-11eb-8f0b-aa0ce4c4837d.png)

Following, these classes are briefly explained.

## MainController
Top class of the hierarchy, controlling: MainUI, GameController and FirstPersonController.
Manages the transition from the main menu to the simulation and vice versa.
It also checks the Esc key on the keyboard, to continue the simulation if it has already been started.

### MainUI
Controls the initial, instructions and game settings menus. Each menu has his own GameObject and it is enabled only when it is selected.
All buttons that start or continue the game will call the MainController's Play method.
The rest of the buttons call methods implemented in this class.
For the settings, the GetSettings method is called by MainController every time the game is started or continued.
That method returns the current values of the menu inputs.

## FirstPersonController
Controls the player body movement and camera rotation. It is used by MainController and GameController.
In the main menu the movement is disabled, only using the camera.
In the game, movement and camera are enabled, limiting movement to terrain space and colliding with ground and objects.
Also, to rotate the camera, you must press the right mouse button.
Additionally, if the player uses the left click raycasting will be used to select a cell, calling the GameController's SelectCell method with collision info as argument.

## GameController
Manages the behavior of the game, controlling: GameUI, World and FirstPersonController.
It will be initialized by MainController, which also uses the SetEnabled method to switch from game to main menu and vice versa without UI conflicts or simulating when not necessary.

Besides, checks the keyboard input for the world controls, doing the following procedures for each one:
* **Play / Pause**:
	Toggle the World's AutomaticSteping variable and change the GameUI button icon.
* **Regenerate world**:
	Stop AutomaticSteping if is enabled, regenerate the World (including objects) and reinitialize the GameUI.
	Additionally, puts the player above the center position of the world.
* **Select world object**:
	Using the raycasting argument, check if the collision is with a WorldObject or with a WorldCell.
	Then, it sends the information of the corresponding cell to the GameUI.
	If the ray has no collision or does not touch any cell it passes a null argument to the UI, disabling the information panel.
* **Return to main menu**:
	Calls the MainController's ReturnToMain method.

Additionally, this class recives the World and Entity settings of the main menu and sends it to the World.

### GameUI
Controls the game UI, including bottom, world object and statistics panels.
Each panel shows the folloing content:
* **Bottom**: Buttons for world controls, avoiding the need to see the instructions to remember the associated keys.
	All that buttons call the methods of the GameController.
	In addition, it includes a world map (using LC_Map from the Lost Cartographer Pack) that shows the heights of terrain, entities and food.
* **WorldObject**:
	Enabled to show information about the selected world object only if it is food or entity.
	When active, displays the energy of the object and, if it is an entity, also the gender, seconds alive, normal and fast speed, and state icons.
* **Statistics**:
	Shows the following data about the simulation: number of entities, born entities, dead entities, deaths by age, deaths by energy, foods, foods energy, food energy ratio by entity and FPS.

### World
Manages of the simulation, controlling the WorldTerrain, entities and foods.
The main system used by the world to control entities and foods is the AutomaticSteping Boolean variable, which is cheched by these before doing any action.
In addition, it also applies the settings passed by GameController, storing them in cheked by WorldTerrain and entities to change their behavior.

#### WorldTerrain
Child of Lost Cartographer Pack's cubic terrain (LC_CubeTerrain), which generates the 3D procedural terrain of the world using its seed.
It creates a 3x3 chunks space (loaded in parallel), with 16 cells per chunk (48x48 cells in total).
When a chunk is loaded, it calls World's method CreateWorldObject for each cell.
The method uses two random numbers, one to choose the WorldObject type (entity, food or obstacle) and another to compare with the probability of that type.
Finally, if the value is greater than or equal to the probability, the object is instantiated at that cell.

Besides, the terrain uses the class WorldCell for each cell, allowing the correct creation and rendering of water areas.

#### Entity
Child of the WorldObject class which is the core of the simulation.
If the variable AutomaticSteping is true, World will call the Step method of each current entity.
In this method, the DoAction procedure is called, trying to do an action from a list of the called EntityAction.
An EntityAction is a struct which contains a condition (function which returns bool) and a method (function which returns float, the cost).
When the DoAction method goes through the ActionsList in order, calling to the condition function of each one.
If the condition returns true it executes the method, getting the cost of the action.
That cost can be a float number between -1 and 100.
If the value is negative, the method can continue going through the list.
Else, the actions loop ends and the value is substracted from the current energy.
An Entity has the nexts actions (in order):
* **Death by age**:
	If SecondsAlive > SecondsToOld, the entity can die by age.
	SecondsToOld is a random value between 120 and 180 (2-3 minutes) assigned at creation.
	The probability of dying is: 1 - ( SecondsToOld / SecondsAlive ).
	That possibility only is checked one time for each second alive and if the DeathByAge setting is enabled.
	If the entity don't die, the cost of the action is -1, continuing the actions loop.
* **Give birth**:
	If the entity IsPregnant (assigned at reproduction action), it can give birth if has started a movement and the previous cell is free.
	For that, a new entity is instanciated at the previous cell, assigning the hereditary attributes defined at reproduction.
	The cost has a pre-defined value grater than 0, finishing the actions loop.
* **Movement**:
	If the entity has changed its cell, it has to move from the previous cell to the new.
	To do this, the position is interpolated using the speed-related attributes.
	The speed can be normal or fast, depending on whether the entity has target.
	The cost changes with the speed, being grater for faster movements.
* **Interact with target**:
	When the entity has a target, it can interact with it if is beside and can access to it.
	The target can be food to eat or other entity for reproduction.
	If it's food, the entity will eat if its energy is not full and the food keep existing, with a cost of 0.
	Otherwise, if it's an entity and has interest in reproduction, then it will take place.
	To do that (and because the action needs to be done by both synchronously) the female calls the Reproduction method of the male in its method.
	Then, the female becomes pregnant and both have the reproduction cost, which is pre-defined as a value greater than 0.
* **Search and path to target**:
	If the entity has to eat or reproduce, it will search at the adjoining positions using the SearchRadius setting.
	Once a target is found, the entity will try to define a path to it.
	For that, the PathfindingWithReusing method from MathFunctions class is used.
	The method searchs a path to the target trying to use the last path, for optimization.
	If it has to calculate a new path, first will try with a direct-line path and, if it fails, the algorithm A*.
	When the path is returned the entity will move to the first position/cell if it exists, having a cost grater than or equal to 0.
	Otherwise, if any target is found or the path don't exists, the action returns with a cost of -1 (continuing the actions loop).
* **Grow**:
	All entities born as childs, being small in size.
	After a time alive, when don't have to eat and SecondsAlive >= SecondsToGrow (between 30 and 60 seconds), it grows.
	When that happens this action is executed, incrementing the size and returning a cost of -1 (continuing the actions loop).
* **Random movement**:
	If any of the previous actions has been executed or returned a value grater than or equal to 0, this action is executed.
	So, the condition function always returns true.
	The action will start a movement to an adjoining cell.
	For that, the MathFunctions's PseudorandomDirection method is used.
	This method returns a direction based on lastDirection and a random value.
	If the random value is greater than sameDirectionProbability it will compute a diferent direction.
	To do that, it will test alternate directions trying to minimize the turn from the lastDirection.
	Finally, the action will return a cost of 0 or -1, depending on whether there is any possible direction or not.

#### Food
Child of the WorldObject class which works as entities energy source.
It takes time to be eaten, and if its energy reaches zero, it is destroyed.
Otherwise, it regenerates a certain amount of energy per second until it is fully restored.
This functionalities only take place if the AutomaticSteping variable of the World is true.

## MathFunctions
Math helper static class implemented specifically for this project, but with generic behavior in mind.
It includes A* pathfinding algorithm and statistics about its calls.


# Future improvements
When the project continues, the following improvements will be implemented:
* **Genetic mutations at Entity reproduction**
* **Statistics related to entities attributes**
* **More world and entity settings**
