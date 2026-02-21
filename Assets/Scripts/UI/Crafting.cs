using UnityEngine;

public class Crafting : MonoBehaviour
{
    public GameObject crafting2x2;
    public GameObject processing;
    public GameObject crafting3x3;
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
    

    private void Awake()
    {
        
    }

    private void Start()
    {
        
    }

    private void Update()
    {
        
    }
    // add functions to be called here that take items as input and return what item to output
    // add functionality that can craft multiple items at once?


    public void CheckCraftingSlots()
    {
        
    }

    public void PutInOutputSlot(byte stackID, int stackQty)
    {
        ItemStack stack = new ItemStack(stackID, stackQty);
        outputSlot.itemSlot.InsertStack(stack);
    }
}
