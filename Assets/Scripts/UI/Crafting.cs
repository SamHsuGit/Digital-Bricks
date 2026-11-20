using UnityEngine;
using System.Collections.Generic;
using System;

public class Crafting : MonoBehaviour
{
    public UIItemSlot craft2x2TopLeft;
    public UIItemSlot craft2x2TopRight;
    public UIItemSlot craft2x2BotLeft;
    public UIItemSlot craft2x2BotRight;
    public UIItemSlot fuelSlot;
    public UIItemSlot inputSlot;
    public UIItemSlot outputSlot;
    public UIItemSlot destroySlot;
    public UIItemSlot craft3x3TopLeft;
    public UIItemSlot craft3x3TopMid;
    public UIItemSlot craft3x3TopRight;
    public UIItemSlot craft3x3Left;
    public UIItemSlot craft3x3Center;
    public UIItemSlot craft3x3Right;
    public UIItemSlot craft3x3BotLeft;
    public UIItemSlot craft3x3BotMid;
    public UIItemSlot craft3x3BotRight;
    public UIItemSlot[] craft2x2Slots;
    public UIItemSlot[] craft3x3Slots;
    public Recipe[] recipes;
    private UIItemSlot[] slotsToCheck;
    public GameObject[] uiMenus;
    public GameMenu gameMenu;
    //public string craftSlotStringColor;
    public string craftSlotStringID;

    //private int[] toolBlockIDs = new int[]
    //{
    //    6, // wood
    //    21, // cobblestone
    //    22, // crystal light green
    //    24, // crystal dark green
    //    26, // crystal blue
    //    28, // crystal orange
    //    30, // crystal red
    //    53, // steel ingot
    //};

    //private int[] crystalBlockIDs = new int[] // only crystal (not ore) can be used to craft pickaxes since single instance used to craft placedBricks
    //{
    //    22, // crystal light green
    //    24, // crystal dark green
    //    26, // crystal blue
    //    28, // crystal orange
    //    30, // crystal red
    //    53, // steel ingot
    //};

    //public int[] itemNames = new int[]
    //{
    //    3841, // pickaxe
    //    //52, // crystal
    //    //30374, // stick
    //};

    private void Update()
    {
        // if input slot is not empty, constantly tick conversion of furnace to convert blocks into output slot
    }
    // add functions to be called here that take items as input and return what item to output
    // add functionality that can craft multiple items at once?


    // triggered when item dropped into slot marked crafting
    public void CheckCraftingSlots(int _inventoryUIMode)
    {
        if(AllCraftingSlotsEmpty()) // ensures when slots are clicked to move from crafting, it empties the output slot
            outputSlot.itemSlot.EmptySlot();
        
        switch(_inventoryUIMode)
        {
            case 1: // if item dropped in 2x2 crafting slot
                {
                    // for all loaded 2x2 recipes, if item sequence in slots match recipe, output 1 item into output slot
                    CheckSlots(_inventoryUIMode);
                    break;
                }
            case 2: // if item dropped in to 3x3 crafting slot
                {
                    // for all loaded 3x3 recipes, if item sequence in slots match recipe, output 1 item into output slot
                    CheckSlots(_inventoryUIMode);
                    break;
                }
            case 3: // if item dropped into furnace
                {
                    // if furnace has no fuel, return
                    // for all furnace recipes if item in input slot does not match an input recipe, return
                    // output slot is not empty AND doesn't match recipe of input item, return
                    // otherwise place 1 converted item into ouptput slot based on recipe
                    break;
                }
        }
    }

    public bool AllCraftingSlotsEmpty()
    {
        if(CraftingSlotsEmpty(craft2x2Slots) && CraftingSlotsEmpty(craft3x3Slots))
            return true;
        else
            return false;
    }

    private bool CraftingSlotsEmpty(UIItemSlot[] _slotsToCheck)
    {
        for (int i = 0; i < _slotsToCheck.Length; i++)
        {
            if(_slotsToCheck[i].itemSlot.HasItem)
            {
                //Debug.Log("item detected in " + i);
                return false;
            }
        }
        return true;
    }

    private void CheckSlots(int _inventoryUIMode)
    {
        
        if(_inventoryUIMode == 1)
            slotsToCheck = craft2x2Slots;
        else if (_inventoryUIMode == 2)
            slotsToCheck = craft3x3Slots;
        
        if(slotsToCheck == null)
            return;

        bool craftingSlotsEmpty = true;
        byte lastSlotID = 0;

        // build an array of block ids that can be compared to the recipeShapes
        //craftSlotStringColor = ""; // reset every time check slots
        craftSlotStringID = ""; // reset every time check slots
        for (int i = 0; i < slotsToCheck.Length; i++)
        {
            if(slotsToCheck[i].itemSlot.HasItem)
            {
                lastSlotID = slotsToCheck[i].itemSlot.stack.id;
                if(slotsToCheck[i].itemSlot.stack.placedBrickID != 0)
                {
                    //craftSlotStringColor += slotsToCheck[i].itemSlot.stack.placedBrickID.ToString() + ",";
                    craftSlotStringID += slotsToCheck[i].itemSlot.stack.placedBrickID.ToString() + ",";
                }
                else
                {
                    //craftSlotStringColor += World.Instance.blockTypes[slotsToCheck[i].itemSlot.stack.id].colorID + ","; // build array of ids in crafting slots using colors
                    craftSlotStringID += World.Instance.blockTypes[slotsToCheck[i].itemSlot.stack.id].id + ","; // build array of ids in crafting slots using ids
                }
                craftingSlotsEmpty = false;
            }
            else
            {
                //craftSlotStringColor += "0,";
                craftSlotStringID += "0,";
            }
        }
        //Debug.Log(craftSlotString);
        if(craftingSlotsEmpty)
            return; // do nothing, ie do not fill output slot if nothing in crafting slots

        // GATHER INFO ON CRAFTING SLOTS
        // flag non matching colors in crafting slots
        // since placedBricks must always use same color bricks to craft
        bool nonmatchingBlockIDs = false;
        byte _blockID = 0;
        byte _prevBlockID = 0;
        int slotsWithItemsCount = 0;
        int slotsWithPlacedBricksCount = 0;
        for(int i = 0; i < slotsToCheck.Length; i++)
        {
            if(slotsToCheck[i].HasItem)
            {
                _blockID = slotsToCheck[i].itemSlot.stack.id;
                //color = World.Instance.blockTypes[slotsToCheck[i].itemSlot.stack.id].colorID; // updated to use color for placedBricks (e.g. sticks, only flagged voxel ids can output colored placedbricks)
                slotsWithItemsCount++;
                if(slotsToCheck[i].itemSlot.stack.isPlacedBrick)
                    slotsWithPlacedBricksCount++;
            }
            if(_blockID != _prevBlockID && _prevBlockID != 0 && _blockID != 0)
                nonmatchingBlockIDs = true;
            _prevBlockID = _blockID;
        }
        if(slotsWithItemsCount < 2) // if 0 or 1 items in all crafting slots, then nonmatching colors cannot be true
            nonmatchingBlockIDs = false;
        // if(nonmatchingcolors)
        //     Debug.Log("nonmatchingcolors = true");

        // LOOP THRU ALL LOADED RECIPES AND SHAPES
        // for all recipes in list, if checkIntArray matches one, then output the corresponding block in output slot
        foreach (Recipe recipe in recipes)
        {
            //int isItem = GetItemNumber(recipe.outputPlacedBrickName);

            // cannot craft placedbricks if the colors do not all match
            // allow non matching colors for special items crafting (e.g. pickaxe)
            if(recipe.isPlacedBrick && nonmatchingBlockIDs)// && isItem == 0)
                continue;
            
            foreach (RecipeShape shape in recipe.recipeShapes)
            {
                // build recipe string
                List<int> slots = new List<int>(){};
                if(shape.is2x2) // only check first 4 slots if marked 2x2 recipeShape
                    slots = new List<int>()
                    {
                        shape.slot1,
                        shape.slot2,
                        shape.slot3,
                        shape.slot4,
                    };
                else
                    slots = new List<int>()
                    {
                        shape.slot1,
                        shape.slot2,
                        shape.slot3,
                        shape.slot4,
                        shape.slot5,
                        shape.slot6,
                        shape.slot7,
                        shape.slot8,
                        shape.slot9,
                    };
                string recipeString = string.Join(",", slots) + ",";

                // if colors all match, allow placed bricks to adapt to any color
                // need to allow non matching colors and placedbrick recipes thru for special item crafting (e.g. pickaxe)
                if(slotsWithItemsCount > 0 && recipe.isPlacedBrick && !nonmatchingBlockIDs)// && isItem == 0) // if has placed bricks in slot
                {
                    //color = World.Instance.blockTypes[color].colorID; // created a colorID for each blockType so that duplicate colors can be avoided, for crystals, voxel id should be referenced as the colorID
                    if(slotsWithPlacedBricksCount == 0 && World.Instance.blockTypes[_blockID].craftPlacedBricks) // only apply to solid color voxels, not placedBricks (applying to placedBricks messes up recipe checks)
                        recipeString = recipeString.Replace("1",_blockID.ToString()); // special case to craft placedBricks from voxels
                    recipe.outputID = _blockID;
                    //Debug.Log("replaced placedBrick values with " + color.ToString());
                }

                // if(recipe.name == "3841 Pickaxe")
                //Debug.Log("checking " + recipe.name + " if " + craftSlotString + " matches " + recipeString);
                string craftSlotStringToCheck;

                //if (recipe.isPlacedBrick)
                //    craftSlotStringToCheck = craftSlotStringColor;
                //else
                    craftSlotStringToCheck = craftSlotStringID;

                if (craftSlotStringToCheck == recipeString) // if match is found execute following code and exit the loop checking each recipe
                {
                    byte outputID;
                    if (recipe.isPlacedBrick || recipe.outputPlacedBrickName == 1) // change output id to match if crafting placedbricks or voxel
                        outputID = _blockID;
                    else // regular voxel recipe
                        outputID = recipe.outputID;
                    //Debug.Log("MATCH FOUND! " + craftSlotString + " matches recipe " + recipe.name + " " + recipeString + " which outputs " + recipe.outputID);

                    //if (isItem != 0) // if item is identified code for specific items
                    //{
                    //    // SPECIAL override for crafting with placedbricks
                    //    // placedbricks not used for crafting
                    //    // ANY items intended for crafting MUST be stored as a voxel with VBO to be able to get rid of the item
                    //    if (recipe.outputPlacedBrickName == 3841) // pickaxe = 3841 INDEX = 0
                    //    {
                    //        // can only craft pickaxe under certain conditions
                    //        // top 3 slots must have items
                    //        if (slotsToCheck[0].HasItem && slotsToCheck[1].HasItem && slotsToCheck[2].HasItem)
                    //        {
                    //            int firstSlotID = World.Instance.blockTypes[slotsToCheck[0].itemSlot.stack.id].colorID; // use first slot (top left) for definition of color
                    //            int secondSlotID = World.Instance.blockTypes[slotsToCheck[1].itemSlot.stack.id].colorID;
                    //            int thirdSlotID = World.Instance.blockTypes[slotsToCheck[2].itemSlot.stack.id].colorID;

                    //            bool matchingTopColors = false;
                    //            if (firstSlotID == secondSlotID && secondSlotID == thirdSlotID)
                    //                matchingTopColors = true;

                    //            //bool matchesToolBlockID = false;

                    //            for (int i = 0; i < toolBlockIDs.Length; i++)
                    //            {
                    //                // only output pickaxe for valid colors
                    //                if (firstSlotID == toolBlockIDs[i] && matchingTopColors)
                    //                {
                    //                    //matchesToolBlockID = true;
                    //                    //break;
                    //                    PutInOutputSlot((byte)firstSlotID, recipe.outputPlacedBrickName, recipe.isPlacedBrick, recipe.outputQty); // output pickaxe
                    //                    return;
                    //                }
                    //            }
                    //        }
                    //    }
                    //}

                    // Override for crafting a voxel
                    if (recipe.outputPlacedBrickName == 1)
                    {
                        //Debug.Log("Crafting back into voxel with color " + outputID);
                        PutInOutputSlot(outputID, 0, false, recipe.outputQty); // force slot to turn into voxel by setting isPlacedBrick flag to false
                    }
                    else
                        PutInOutputSlot(outputID, recipe.outputPlacedBrickName, recipe.isPlacedBrick, recipe.outputQty);
                    return;
                }
            }
        }

        outputSlot.itemSlot.EmptySlot(); // if made it here, no matching crafting recipe so empty the output slot
    }

    private byte OverrideBlockID(byte _inputID)
    {
        // temporary manual hard code override
        // wood needs to have separate crafting ID than brown dirt but must stack with other brown placedBricks
        byte _returnValue = _inputID;
        switch (_inputID)
        {
            case 14:
                {
                    _returnValue = 6;
                    break;
                }
        }

        return _returnValue;
    }

    //private int GetItemNumber(int _outputPlacedBrickName)
    //{
    //    for(int i = 0; i < itemNames.Length; i++)
    //    {
    //        if(_outputPlacedBrickName == itemNames[i])
    //            return itemNames[i];
    //    }

    //    return 0;
    //}

    public void PutInOutputSlot(byte _stackID, int _stackPlacedBrickID, bool _isPlacedBrick, int _stackQty)
    {
        ItemStack stack = new ItemStack(_stackID, _stackPlacedBrickID, _isPlacedBrick, _stackQty);
        //Debug.Log("_stackPlacedBrickID = " + _stackPlacedBrickID);
        outputSlot.itemSlot.InsertStack(stack);
        gameMenu.controller.brickPlaceDown.Play(); // eventually change to unique crafting sound
    }

    public void ClickedOutputSlot()
    {
        // subtract one from each crafting slot, if crafting slot is empty, empty slot
        foreach(UIItemSlot slot in slotsToCheck)
        {
            if(slot.itemSlot.HasItem)
            {
                slot.itemSlot.Take(1);
                if(slot.itemSlot.stack.amount <= 0)
                    slot.itemSlot.EmptySlot();
            }
        }
    }

    public void ClickedDestroySlot(UIItemSlot slot)
    {
        if (slot.itemSlot.HasItem)
            slot.itemSlot.EmptySlot();
    }

    public void RightClickedDestroySlot(UIItemSlot slot)
    {
        if (slot.itemSlot.HasItem)
        {
            slot.itemSlot.Take(1);
            if (slot.itemSlot.stack.amount <= 0)
                slot.itemSlot.EmptySlot();
        }
    }
}
