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

    private void Update()
    {
        // if input slot is not empty, constantly tick conversion of furnace to convert blocks into output slot
    }
    // add functions to be called here that take items as input and return what item to output
    // add functionality that can craft multiple items at once?


    // triggered when item dropped into slot marked crafting
    public void CheckCraftingSlots(int _inventoryUIMode)
    {
        if(outputSlot.itemSlot.HasItem) // ensures when slots are clicked to move from crafting, it empties the output slot
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
        string craftSlotString = "";
        for(int i = 0; i < slotsToCheck.Length; i++)
        {
            if(slotsToCheck[i].itemSlot.HasItem)
            {
                lastSlotID = slotsToCheck[i].itemSlot.stack.id;
                craftSlotString += slotsToCheck[i].itemSlot.stack.id + ",";
                craftingSlotsEmpty = false;
            }
            else
                craftSlotString += "0,";
        }
        //Debug.Log(craftSlotString);
        if(craftingSlotsEmpty)
            return; // do nothing, ie do not fill output slot if nothing in crafting slots

        // flag non matching colors in crafting slots
         // since placedBricks must always use same color bricks to craft
        bool nonmatchingcolors = false;
        byte color = 0;
        byte prevColor = 0;
        for(int i = 0; i < slotsToCheck.Length; i++)
        {
            if(slotsToCheck[i].HasItem)
                color = slotsToCheck[i].itemSlot.stack.id;
            if(color != prevColor && prevColor != 0 && color != 0)
                nonmatchingcolors = true;
            prevColor = color;
        }
        // if(nonmatchingcolors)
        //     Debug.Log("nonmatchingcolors = true");

        // for all recipes in list, if checkIntArray matches one, then output the corresponding block in output slot
        foreach (Recipe recipe in recipes)
        {
            // cannot craft placedbricks if the colors do not all match
            if(nonmatchingcolors && recipe.isPlacedBrick)
                continue;
            
            foreach (RecipeShape shape in recipe.recipeShapes)
            {
                // build recipe string
                List<int> slots = new List<int>(){};
                if(_inventoryUIMode == 1)
                    slots = new List<int>()
                    {
                        shape.slot1,
                        shape.slot2,
                        shape.slot4,
                        shape.slot5,
                    };
                else if(_inventoryUIMode == 2)
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

                // if(!nonmatchingcolors && recipe.colorless) // if colors all match, change colorless recipes to match color of crafting slots to find match
                // {
                //     recipeString.Replace("1",color.ToString());
                //     recipe.outputID = 0;
                // }

                //Debug.Log(recipeString);
                if(craftSlotString == recipeString) // if match is found exit the loop
                {
                    byte outputID;
                    if(recipe.isPlacedBrick) // placedbricks take the color of the last placed brick
                        outputID = lastSlotID;
                    else
                        outputID = recipe.outputID;
                    //Debug.Log(craftSlotString + " matches " + recipeString);
                    PutInOutputSlot(outputID, recipe.outputPlacedBrickName, recipe.isPlacedBrick, recipe.outputQty); // output the crafting recipe output item and qty
                    // bug where recipes not crafting correct item
                    return;
                }
            }
        }
    }

    public void PutInOutputSlot(byte _stackID, string _stackPlacedBrickID, bool _isPlacedBrick, int _stackQty)
    {
        ItemStack stack = new ItemStack(_stackID, _stackPlacedBrickID, _isPlacedBrick, _stackQty);
        Debug.Log("_stackPlacedBrickID = " + _stackPlacedBrickID);
        outputSlot.itemSlot.InsertStack(stack);
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
}
