using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DragAndDropHandler : MonoBehaviour {

    [SerializeField] private UIItemSlot cursorSlot = null;
    private ItemSlot cursorItemSlot;

    [SerializeField] private GraphicRaycaster m_Raycaster = null;
    private PointerEventData m_PointerEventData;
    [SerializeField] private EventSystem m_EventSystem = null;

    public Controller controller;

    public Inventory inventory;

    private UIItemSlot[] slotArray;

    private void Start()
    {
        cursorItemSlot = new ItemSlot(cursorSlot);
    }

    private void Update()
    {
        if(!controller.inInventoryUI) // only do following if controller.inInventoryUI = true
            return;

        cursorSlot.transform.position = Input.mousePosition;

        bool clicked = Input.GetMouseButtonDown(0);

        // controller.inputHandler.mine causes 2 clicks, one on press, one on release...
        if (!controller.inputHandler.sprint && clicked)//controller.inputHandler.mine)
        {
            HandleLeftClickSlot(CheckForSlot());
        }
        else if(controller.inputHandler.sprint && clicked)
        {
            HandleStackQuickMove(CheckForSlot());
        }
        else if (Input.GetMouseButtonDown(1)) // if right clicked
        {
            HandleRightClickSlot(CheckForSlot());
        }
    }

    public void OnInventory() // toggle bool to track state
    {
        inventory.gameObject.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ReturnToGame()
    {
        inventory.gameObject.SetActive(false);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void HandleRightClickSlot(UIItemSlot clickedSlot)
    {
        if(clickedSlot == null)
            return;

        if(cursorSlot.itemSlot.HasItem && !clickedSlot.HasItem) // if right clicked empty slot and holding items
        {
            // drop 1 item into slot and subtract one from stack
            cursorSlot.itemSlot.Take(1);
            ItemStack newStack = new ItemStack(cursorSlot.itemSlot.stack.id, 1);
            clickedSlot.itemSlot.InsertStack(newStack);
            if(cursorSlot.itemSlot.stack.amount <= 0)
                cursorSlot.itemSlot.EmptySlot();
        }
        else if (!cursorSlot.itemSlot.HasItem && clickedSlot.HasItem && clickedSlot.itemSlot.stack.amount > 1) // if right clicked stack of items with more than 1 item
        {
            int originalAmount = clickedSlot.itemSlot.stack.amount;
            int amount = Mathf.CeilToInt(clickedSlot.itemSlot.stack.amount / 2f); // try and divide by 2
            int makeUpAmount = 0;
            if((clickedSlot.itemSlot.stack.amount % 2) != 0) // if not divisible by 2 (e.g. 3)
                makeUpAmount = 1; // take away a little extra to not give extra blocks
            clickedSlot.itemSlot.Take(originalAmount - amount + makeUpAmount);
            ItemStack newStack = new ItemStack(clickedSlot.itemSlot.stack.id, amount);
            cursorSlot.itemSlot.InsertStack(newStack);
        }
        else if (cursorSlot.itemSlot.HasItem && clickedSlot.HasItem) // right clicking a slot with item while holding item
        {
            ItemStack cursorStack = cursorSlot.itemSlot.stack;
            ItemStack clickedStack = clickedSlot.itemSlot.stack;
            // try and add one of the blocks to destination slot if blockIDs match, do not do if would put over the maximum
            if(cursorStack.id == clickedStack.id && clickedStack.amount + 1 <= World.Instance.blockTypes[clickedStack.id].stackMax)
            {
                cursorSlot.itemSlot.Take(1); // drop off only one item
                clickedSlot.itemSlot.Give(1);
                if(cursorSlot.itemSlot.HasItem && cursorSlot.itemSlot.stack.amount <= 0)
                    cursorSlot.itemSlot.EmptySlot();
            }
        }
    }

    private void HandleStackQuickMove(UIItemSlot clickedSlot)
    {
        if(clickedSlot == null)
            return;
        
        ItemStack clickedStack = clickedSlot.itemSlot.stack;

        if(clickedSlot.inHotbar)
            slotArray = inventory.inventorySlots;
        else
            slotArray = controller.toolbar.slots;
        // move entire stack to first empty slot in inventory slots
        for(int i = 0; i < slotArray.Length; i++)
        {
            int stackMax = World.Instance.blockTypes[clickedStack.id].stackMax; // cache stack max value

            if(!slotArray[i].itemSlot.HasItem) // if the inventory slot does not have a stack
            {
                slotArray[i].itemSlot.InsertStack(clickedStack); // insert the stack at this position
                clickedSlot.itemSlot.EmptySlot(); // empty slot
                return;
            }
            else if (slotArray[i].itemSlot.stack.id == clickedStack.id) // if inventory slot already has item (check for overflow exceeding stack limit)
            {
                // do not do if putting values would exceed the stack max, instead continue thru loop and move on to next slot
                // if adding the items would not exceed the stack limit
                if(slotArray[i].itemSlot.stack.amount + clickedStack.amount <= stackMax)
                {
                    slotArray[i].itemSlot.Give(clickedStack.amount); // add to stack
                    clickedSlot.itemSlot.EmptySlot(); // empty slot
                    return;
                }
                else // IF OVERFLOW and adding the items would exceed stack limit, max slot and loop again to next slot[i] with reduced clicked stack amount
                {
                    ItemStack stack = new ItemStack(clickedStack.id, stackMax);
                    slotArray[i].itemSlot.InsertStack(stack); // max out this slot
                    clickedStack.amount = stackMax - slotArray[i].itemSlot.stack.amount; // loop again to next slot with reduced clicked stack amount

                    // ERROR this is putting stack of amount zero in next open slot
                }
            }
        }
    }

    private void HandleLeftClickSlot (UIItemSlot clickedSlot) {

        if (clickedSlot == null && cursorSlot.HasItem) // if clicked air while holding block
        {
            byte blockID = cursorSlot.itemSlot.stack.id;
            for(int i = 0; i <= cursorSlot.itemSlot.stack.amount; i++)
            {
                controller.toolbar.SpawnObject(blockID); // spawn blocks
            }
            cursorSlot.itemSlot.EmptySlot(); // empty cursor slot
        }
        else if (clickedSlot == null || clickedSlot.itemSlot == null)
            return;

        if (clickedSlot!= null && !cursorSlot.HasItem && !clickedSlot.HasItem) // if no items to move then exit
            return;

        if (clickedSlot != null && clickedSlot.itemSlot.isCreative) // not used???
        {
            cursorItemSlot.EmptySlot();
            cursorItemSlot.InsertStack(clickedSlot.itemSlot.stack);
        }

        if (clickedSlot != null && !cursorSlot.HasItem && clickedSlot.HasItem) // pickup stack
        {
            cursorItemSlot.InsertStack(clickedSlot.itemSlot.TakeAll());
            return;
        }

        if (clickedSlot != null && cursorSlot.HasItem && !clickedSlot.HasItem) // if holding item and empty slot, put items in empty slot
        {
            clickedSlot.itemSlot.InsertStack(cursorItemSlot.TakeAll());
            return;
        }

        if (clickedSlot != null && cursorSlot.HasItem && clickedSlot.HasItem) // if holding item and slot has item, drop stack into slot
        {
            // if (cursorSlot.itemSlot.stack.id != clickedSlot.itemSlot.stack.id) // if items do not match, swap item stacks
            // {
            //     ItemStack oldCursorSlot = cursorSlot.itemSlot.TakeAll();
            //     ItemStack oldSlot = clickedSlot.itemSlot.TakeAll();

            //     clickedSlot.itemSlot.InsertStack(oldCursorSlot);
            //     cursorSlot.itemSlot.InsertStack(oldSlot);
            // }
            if (cursorSlot.itemSlot.stack.id == clickedSlot.itemSlot.stack.id) // if same block id, try and add up items in clicked slot to stack maximum
            {
                int stackMax = World.Instance.blockTypes[clickedSlot.itemSlot.stack.id].stackMax; // cache max stack value for clicked slot

                if(cursorSlot.itemSlot.stack.amount > stackMax - clickedSlot.itemSlot.stack.amount) // if adding would put over max
                {
                    int difference = stackMax - clickedSlot.itemSlot.stack.amount; // save how many are left
                    //ItemStack tempStack = new ItemStack(clickedSlot.itemSlot.stack.id, difference); // create temp stack with how many are left
                    clickedSlot.itemSlot.stack.amount = stackMax; // max out clicked slot
                    cursorSlot.itemSlot.stack.amount -= difference; // reduce cursor slot by difference

                    // force text values to update
                    clickedSlot.UpdateSlot();
                    cursorSlot.UpdateSlot();
                }
                else // else just add the items to the stack normally
                {
                    clickedSlot.itemSlot.Give(cursorSlot.itemSlot.stack.amount);
                    cursorSlot.itemSlot.EmptySlot();
                }
                // catch check for cursor slot has amount zero or less, just remove all items
                if(cursorSlot.itemSlot.HasItem && cursorSlot.itemSlot.stack.amount <= 0)
                    cursorSlot.itemSlot.EmptySlot();
            }
        }
    }

    private UIItemSlot CheckForSlot () {

        m_PointerEventData = new PointerEventData(m_EventSystem);
        m_PointerEventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        m_Raycaster.Raycast(m_PointerEventData, results);

        foreach (RaycastResult result in results)
        {
            //Debug.Log("found object " + result.gameObject.name + " of tag: " + result.gameObject.tag);
            if (result.gameObject.tag == "UIItemSlot")
            {
                return result.gameObject.GetComponent<UIItemSlot>();
            }
        }
        return null;
    }
}