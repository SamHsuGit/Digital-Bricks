using UnityEngine;

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

    public int[] blocksInSlots;

    public GameObject[] uiMenus;

    private void Awake()
    {
        
    }

    private void Start()
    {
        
    }

    private void Update()
    {
        // if input slot is not empty, constantly tick conversion of furnace to convert blocks into output slot
    }
    // add functions to be called here that take items as input and return what item to output
    // add functionality that can craft multiple items at once?


    // triggered when item dropped into slot marked crafting
    public void CheckCraftingSlots(int _inventoryUIMode)
    {
        switch(_inventoryUIMode)
        {
            case 1: // if item dropped in 2x2 crafting slot
                {
                    // for all loaded 2x2 recipes, if item sequence in slots match recipe, output 1 item into output slot
                    break;
                }
            case 2: // if item dropped in to 3x3 crafting slot
                {
                    // for all loaded 3x3 recipes, if item sequence in slots match recipe, output 1 item into output slot
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

    public void PutInOutputSlot(byte stackID, int stackQty)
    {

        ItemStack stack = new ItemStack(stackID, stackQty);
        outputSlot.itemSlot.InsertStack(stack);
    }
}
