Samuel Hsu 2002

TABLE OF CONTENTS
	Input Handler
	Controller
	Gun
	Health
	Collisions
	Camera
	Procedural Generation
	LDraw Importer
	Save System
	Multiplayer
	Lighting
	Saved Settings
	UI
	DEBUG

INPUT HANDLER
	Script: Input Handler
		Uses InputActions from the Unity InputSystem package.
	This sets up input action callbacks which the PlayerInput script on the player prefab references to invoke unity events.
	This allows for easy revision of control schemes through the InputAction asset.

CONTROLLER
	Script: Controller
		Defines player actions, animations, and properties.
	Spawns SceneObjects when breaking off pieces, breaking/placing blocks, shooting blocks...
	SceneObjects are used to simplify the networked spawning of objects thru Mirror.
	Actions:
		Move (WASD keys, left stick, see collisions)
		Sprint (Shift, left shoulder bumper, increases speed when held)
		Jump (Spacebar, A button, max 2 jumps)
		Look (Mouse, right stick, speeds and FOV defined by settings)
		Select (Scroll wheel, Arrow Keys, D-Pad moves through 9 hotbar slots in HUD)
		Use (E Key/X button)
		Drop (Q key/B button)
		Shoot (Left click)
		Grab (Right click)
		Options (Escape Key, Start Button toggles on/off)
		Cycle CamMode (Tab Key, Select Button)
		Show Controls (Y key, Y button toggles on/off)
	Animations:
		Defined procedurally based on 2 imported keyframes for charIdle.ldr and charRun.ldr
		Makes the appearance of animation by toggling these two imported models on/off. Chosen for stylistic reasons as well since
		most brick-films have a destinctive lower fps. Also much simpler to procedurally animate this way than to define bones
		programmatically.
	Variables:
		reach (distance player can shoot/grab, procedurally defined based on imported model)
		collider height (procedurally defined based on charIdle.ldr # pieces)
		collider radius (procedurally defined based on charIdle.ldr # pieces)

GUN
	Script: Gun
		Uses a raycast to find gameobjects with the Health Component and subtracts hp when shots are landed.
	Variables:
		fireRate (how fast player can shoot)
		damage (how much hp is damaged from each shot)

HEALTH
	Script: Health
		Calculated player HP procedurally based on # bricks in imported player model charIdle.ldr. Removes hp when triggered by Gun script
		when a shot is landed. Also subtracts hp when hunger (caused by jumping, breaking blocks)
		Respawns the player when destroyed (i.e. hp = 0)
	Variables:
		hpMax (procedurally defined based on charIdle.ldr # pieces)
		minPieces
		maxPieces (limited based on performance of min pc spec model load time, does not actually prevent import but defines soft 
		upper limit where the character's calculated move speed will equal 0 becuase it is too large/heavy)
		minBaseMoveSpeed
		maxBaseMoveSpeed
		minAnimSpeed
		maxAnimSpeed
		piecesRbMass (determines gravity's effects)
		baseAnimRate (procedurally defined based on charIdle.ldr # pieces)
		baseWalkSpeed (procedurally defined based on charIdle.ldr # pieces)
		baseSprintSpeed (procedurally defined based on charIdle.ldr # pieces)

PROCEDURAL GENERATION
	Code based on the YouTube tutorials by B3agz https://github.com/b3agz/Code-A-Game-Like-Minecraft-In-Unity
		was modified to make a brick-build procedural world
	Script: VoxelData
		Determines voxel size, max world size in chunks, chunk size
	Script: Chunk
		Handles coordinates, mesh data, generates a mesh collider
	Script: World	
		Handles procedural world generation/rendering during the GetVoxel pass, adds players to the game
		loadDistance (the distance around the player in chunks in which the world is loaded before rendering)
		drawDistance (saved to settings)
		studRenderDistanceInChunks
		solidGroundHeight
	Script: Noise
		Defines the perlin noise functions used to procedurally generate the world. Makes the entire world generate from 
		a single seed number
	Script: Structure
		Hard-coded definitions for structures like trees, mushrooms, cacti, monoliths...
	Script: Planets
		Scriptable objects which define blockID(colors) and biome values for world generation for each planet in our solarsystm.
	Script: Biomes
		Scriptable objects which define blockID(color) values for regions in a world

LDRAW IMPORTER
	Original Code by Grygory Dyadichenko (MIT License) (https://github.com/Nox7atra/LDraw_Importer_Unity) modified to import ldraw models at runtime.
	LDraw file format (CCAL 2.0)
	Script: LDrawImporterRuntime
		Defines how models are imported at runtime.
	DEFAULT LDRAW FILES
		"base.ldr"      - The player base spawned at world origin (must match for online play)
		"charIdle.ldr"  - The player character idle pose
		"charRun.ldr"   - The player character's run pose
		"projectile.ldr"- The player character's projectile that can be shot if you have crystals
	DEFAULT LDRAW FILES FILE LOCATIONS:
		Winx64: Digital Bricks_Data/StreamingAssets
		MacOS: 	Digital Bricks MacOS.app/Contents/StreamingAssets

	CUSTOM LDRAW FILE SETUP PROCESS
		NOTE: Parts library must be manually updated as new parts are added to the ldraw system.
		1. Save Stud.io filename (e.g. base.ldr)
		2. Remove all previously existing submodels (if any).
		3. All parts must be under one submodel named "filename-submodel.ldr" (e.g. base-submodel.ldr)
   		NOTE: Sometimes Stud.io has its own submodels denoted with "bl_" prefix which ldraw cannot import
		4. Export as .ldr to  the Digital Bricks_Data/ldraw/models/ folder with one of the valid filenames (e.g. base.ldr)

COLLISIONS
	Script: VoxelCollider
		Originally added to eliminate need for mesh collisions (thought to be performance heavy). Mesh Collisions were not as 
		performance heavy as originally thought so chose to use mesh collisions again.
	Variables:
		jump height 
		baseWalkSpeed (defined in "Health")
		baseSprintSpeed (defined in "Health")

SAVE SYSTEM
	Script: SaveSystem
		Gameworlds are saved as worldData (serialized chunkIDs) which are essentially a long list of chunk names and locations in the world.
		chunks are saved as chunkData (serialized blockIDs) which are essentially a long list of blockIDs for each chunk.
		The save system writes and reads the worldData and chunkData files as well as playerData which stores 
		player name, inventory, position, hp

MULTIPLAYER
	Online Network Play uses Mirror by vis2k (MIT License)
	LOCAL/SPLITSCREEN
		1. Additional controller players may join using the Xbox “A” button. Players may then leave using the options menu.

	ONLINE MULTIPLAYER
		1. All players must be using the same version of the game.
		2. Hosts must share their IP address, planet number, assets folder, and world folder with other players before playing (must synchronize asset/world data)
   			Public IP address if not on same LAN
   			LAN IP address if same LAN
		3. All hosts must port forward thru port 7777.
		4. Other players must put the shared world file in the save file location and enter the host IP address and planet number.
	NOTE: For best practice, all players should join a multiplayer game at same time (or close to) after sharing world and ldraw files manually.

	SAVE FILE LOCATION
		1. (Windows) C:\Users%userprofile%\AppData\LocalLow\Sam Hsu\Digital Bricks\saves\
		2. (MacOS) ~/Library/Application Support/Sam Hsu/Digital Bricks/saves/
		3. (UWP) %userprofile%\AppData\Local\Packages\Digital Bricks\LocalState\saves\

LIGHTING
	

SAVED SETTINGS
	player preferences for gameplay settings are stored in the settings.cfg file in C:\Digital Bricks\Digital Bricks_Data
	player preferences include in-game options menu selections for
		ip Address
		Volume
		Look Speed
		Look Accel
		FOV
		InvertY
		InvertX

UI
	

DEBUG
	Please report all bugs and on GitHub and share your Player.log file (please check if the issue was already reported): https://github.com/SamHsuGit/LDPlay/issues

	Player.log FILE LOCATIONS
		Windows: C:\Users%userprofile%\AppData\LocalLow\Sam Hsu\Digital Bricks\Player.log"
		MacOS: "~/Library/Application Support/Sam Hsu/Digital Bricks/Player.log"
		UWP = "%userprofile%\AppData\Local\Packages\Digital BricksLocalState\Player.log"
	