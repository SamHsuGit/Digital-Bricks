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
    public string craftSlotStringID;

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
        craftSlotStringID = ""; // reset every time check slots
        for (int i = 0; i < slotsToCheck.Length; i++)
        {
            if(slotsToCheck[i].itemSlot.HasItem)
            {
                lastSlotID = slotsToCheck[i].itemSlot.stack.id;
                if(slotsToCheck[i].itemSlot.stack.placedBrickID != 0)
                    craftSlotStringID += slotsToCheck[i].itemSlot.stack.placedBrickID.ToString() + ",";
                else
                    craftSlotStringID += World.Instance.blockTypes[slotsToCheck[i].itemSlot.stack.id].id + ","; // build array of ids in crafting slots using ids
                craftingSlotsEmpty = false;
            }
            else
                craftSlotStringID += "0,";
        }
        //Debug.Log(craftSlotStringID);
        if (craftingSlotsEmpty)
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
        // if(nonmatchingBlockIDs)
        //     Debug.Log("nonmatchingBlockIDs = true");

        // LOOP THRU ALL LOADED RECIPES AND SHAPES
        // for all recipes in list, if checkIntArray matches one, then output the corresponding block in output slot
        foreach (Recipe recipe in recipes)
        {
            // cannot craft placedbricks if the colors do not all match
            // allow non matching colors for special items crafting (e.g. pickaxe)
            if(recipe.isPlacedBrick && nonmatchingBlockIDs)// && isItem == 0)
                continue;
            
            foreach (RecipeShape shape in recipe.recipeShapes)
            {
                // build recipe string
                List<int> slots = new List<int>(){};
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
                    if(slotsWithPlacedBricksCount == 0 && World.Instance.blockTypes[_blockID].craftPlacedBricks) // only apply to solid color voxels, not placedBricks (applying to placedBricks messes up recipe checks)
                        recipeString = recipeString.Replace("1",_blockID.ToString()); // special case to craft placedBricks from voxels
                    recipe.outputID = _blockID;
                    //Debug.Log("replaced placedBrick values with " + _blockID.ToString());
                }

                string craftSlotStringToCheck;

                craftSlotStringToCheck = craftSlotStringID;

                if (craftSlotStringToCheck == recipeString) // if match is found execute following code and exit the loop checking each recipe
                {
                    byte outputID;
                    if (recipe.isPlacedBrick || recipe.outputPlacedBrickName == 1) // change output id to match if crafting placedbricks or voxel
                        outputID = _blockID;
                    else // regular voxel recipe
                        outputID = recipe.outputID;
                    //Debug.Log("MATCH FOUND! " + craftSlotString + " matches recipe " + recipe.name + " " + recipeString + " which outputs " + recipe.outputID);

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
