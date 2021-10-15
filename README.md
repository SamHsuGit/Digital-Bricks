# BrickFormers
A Fan-Made LEGO Open-World RPG game
Samuel Hsu 2021

MINIMUM SYSTEM REQUIREMENTS RAM: 6 GB VRAM: 128 MB CPU Cores: 2 Clock Speed: 1.8 GHz Free Disk Space: 100 MB for save files

INSTALLATION INSTRUCTIONS Windows: Open the file: "BrickFormers - A Fan-Made Game.exe" Mac: Follow instructions at https://www.youtube.com/watch?v=sWSySXfR17c

SAVE FILE LOCATION Hosts must share their IP address, seed, and world file with other players before starting a multiplayer match. Windows: "C:\Users%userprofile%\AppData\LocalLow\tformers3\BrickFormers - A Fan-Made Game\saves" Mac: "~/Library/Application Support/tformers3/BrickFormers - A Fan-Made Game/saves/" UWP: "%userprofile%\AppData\Local\Packages\BrickFormers - A Fan-Made Game\LocalState\saves"

CHANGELOG 0.27.0.0 21w30 10/11/21 10:30 AM Removed Bloom (was causing severe performance drags on older CPUs) Revised Cacti color

0.26.0.0 21w29 9/29/21 8:18 AM Revised Cloud Generation offset

0.25.0.0 21w39 9/28/21 10:59 PM Revised Tree generation

0.24.0.0 21w38 9/24/21 1:06 PM Reduced initial save file size significantly

0.23.0.0 21w38 9/23/21 10:18 PM Aligned held brick rotation with all other bricks in world Replaced World name with seed

0.22.0.0 21w38 9/20/21 6:51 PM Added torch to help players see in the dark Made night even darker by turning off ambient light during night Added Smudges Normal map to block materials Added brick breaking function to shoot button Added brick placement controls to simulate grabbing bricks Moon/Sun Intensity modified to provide better reflections Material roughness/smoothness updated to have more accurate specular reflections Added moon Syncronized world sounds to day/night cycle Added 24hr autosave for servers Changed savegame button to call save on server Changed savegame button to save spawn point Syncronized player name/color for multiplayer Updated player animations Added giant trees Implemented hunger into hp system to encourage gathering of food Added PlayerStats to SaveSystem Added Player position, hp, inventory to playerStats Revised Brick Placement Sounds Added Menu Sounds Added character limb color customization Revised lava/water generation to optimize performance Added emission textures to crystals Added voxelBoundObjects (crystals, grass, mushroom, bamboo, flowers) Increased movement speed when walking on stone (roads) Corrected autojump to only occur when sprinting Corrected colliders Limited Jumps to 2 Moved char color selection to char select menu Added seed and world render distance to char select menu Added World Seed to randomize world

0.21.0.0 21w31 8/8/21 9:55 PM Corrected vehicle collider Corrected Leaves and Wood Textures Added Brick Pickup System Added Char Select Menu Updated tree shape Updated tree and mushroom colors Added autojump for easier terrain traversal

0.20.0.0 21w31 8/3/21 11:30 PM Added mushrooms Added seasonal tree leaves Revised Fog Added Bloom Added DoF Added procedural studs

0.19.0.0 21w31 8/2/21 11:30 PM Added Ravines

0.18.0.0 21w30 7/29/21 5:30 PM Added Filesave System

0.17.0.0 21w30 7/28/21 5:30 PM Added Water Block Revised Soundtrack Revised Cloud Generation

0.16.0.0 21w30 7/26/21 11:56 PM Added fog Shortened day/night cycle Revised cloud generation

0.15.0.0 21w29 7/21/21 2:00 PM Added global illumination Updated Options menu Updated Block Textures Updated Block Shading Added Day/Night Cycle

0.14.0.0 21w29 7/20/21 1:56 PM Added clouds

0.13.0.0 21w28 7/16/21 4:56 PM Revised Settings Menu AJdded Graphics Quality Settings

0.12.0.0 21w28 7/15/21 5:42 PM Added world modification with blocks Scaled player geometry

0.11.0.0 21w28 7/14/21 12:43 AM Replaced Game Scene

0.10.0.0 21w25 6/26/21 6:47 PM Removed score counter

0.9.0.0 21w284 6/16/21 2:54 PM Added PC/Mac Crossplay

0.8.0.0 21w23 6/11/21 1:42 AM Upgraded to Unity 2020.3.11f1 Added Splitscreen Multiplayer Updated destruction physics logic Added Options Menu usage for gamepads Added Controls Text Added InvertY options Added Reticle Color Feedback Revised App Icons Code Consolidation Overhaul Options Menu UI Bug Fixes

0.7.0.0 21w21 5/26/21 3:00 AM Updated Movement mechanics Added destruction physics for Minifig

0.6.0.0 21w20 5/23/21 9:00 PM Default Mode changed to robot mode for easier play w/o holding buttons Added FPS Camera view Added FPS Camera Sensitivity Option Added Targeting Reticle

0.5.0.0 21w29 5/13/21 2:08 AM Upgraded to Unity 2020.3.7f1 Added Minfig Character Added Character Selection Menu Changed default test scene to simple platform Removed Lego Microgame Assets Rescaled objects Updated Gravity/Player Mass/Speed Values Added Player Nametags Revised Soundtrack Code Consolidation Overhaul

0.4.0.0 21w18 5/6/21 12:50 PM Added multiplayer animations Adjusted bloom and DoF for more clear visuals Re-adjusted default look sensitivity Changed default test scene to exterior platform Added skybox Added box rigidbody objects

0.3.1.0 21w17 5/2/21 9:13 PM Increased default look sensitivity

0.3.0.0 21w17 5/1/21 11:15 PM Added Basic Online Multiplayer with synced player movement

0.2.1.0 21w15 4/17/21 11:38 AM Corrected options menu so controls menu text is completely on-screen

0.2.0.0 21w15 4/17/21 10:53 AM Upgraded to Unity 2019.4.23f1 Changed from SRP to URP Implented Post Processing Effects (Bloom, Motion-Blur, Depth of Focus) Overlaid scratches/details to materials Added Titlescreen Animation (Rendered in Blender) Changed platform from WebGL to PC, Mac, Linux Standalone Changed Default Level to be Interior Room

0.1.0.0 21w11 3/15/21 1:33 AM Unity 2019.4.15f1 Initial Release for WebGL Submitted to Unity X Lego (Lego Ideas) MicroGame Contest Implemented basic UI Options Menu Added Gamepad controller support Added BrickFormer Character Added Character Animations Added Character destruction physics logic Added Box Spawner Added LEGO brick pickups Added Soundtrack and Crickets SFX