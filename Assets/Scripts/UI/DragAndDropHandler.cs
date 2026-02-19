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
            HandleSlotClick(CheckForSlot());
        }
        else if(controller.inputHandler.sprint && clicked)
        {
            HandleStackQuickMove(CheckForSlot());
        }
        else if (Input.GetMouseButtonDown(1)) // if right clicked
        {
            HandlePartialStackMove(CheckForSlot());
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

    private void HandlePartialStackMove(UIItemSlot clickedSlot)
    {
        if(clickedSlot == null)
            return;

        if(cursorSlot.itemSlot.HasItem && !clickedSlot.HasItem) // if right clicked empty slot and holding items
        {
            // drop 1 item into slot and subtract one from stack
            cursorSlot.itemSlot.Take(1);
            if(cursorSlot.itemSlot.stack.amount <= 0)
                cursorSlot.itemSlot.EmptySlot();
            ItemStack newStack = new ItemStack(cursorSlot.itemSlot.stack.id, 1);
            clickedSlot.itemSlot.InsertStack(newStack);
        }
        else if (!cursorSlot.itemSlot.HasItem && clickedSlot.HasItem) // if right clicked stack of items
        {
            int originalAmount = clickedSlot.itemSlot.stack.amount;
            int amount = Mathf.FloorToInt(clickedSlot.itemSlot.stack.amount / 2f);
            clickedSlot.itemSlot.Take(amount);
            ItemStack newStack = new ItemStack(clickedSlot.itemSlot.stack.id, originalAmount - amount);
            cursorSlot.itemSlot.InsertStack(newStack);
        }
    }

    private void HandleStackQuickMove(UIItemSlot clickedSlot)
    {
        if(clickedSlot == null)
            return;
        
        ItemStack clickedStack = clickedSlot.itemSlot.stack;
        UIItemSlot[] slotArray;

        if(clickedSlot.inHotbar)
        {
            slotArray = inventory.inventorySlots;
        }
        else
        {
            slotArray = controller.toolbar.slots;
            // for(int i = 0; i < slotArray.Length; i++)
            // {
            //     if(!slotArray[i].HasItem) // if the toolbar slot does not have a stack
            //     {
            //         slotArray[i].itemSlot.InsertStack(clickedSlot.itemSlot.stack); // insert the stack at this position
            //         clickedSlot.itemSlot.EmptySlot(); // empty slot
            //     }
            // }
        }
        // move entire stack to first empty slot in inventory slots
        for(int i = 0; i < slotArray.Length; i++)
        {
            if(!slotArray[i].itemSlot.HasItem) // if the inventory slot does not have a stack
            {
                slotArray[i].itemSlot.InsertStack(clickedStack); // insert the stack at this position
                clickedSlot.itemSlot.EmptySlot(); // empty slot
            }
            else if (slotArray[i].itemSlot.stack.id == clickedStack.id) // if inventory slot already has item
            {
                slotArray[i].itemSlot.Give(clickedStack.amount); // add to stack
            }
        }
    }

    private void HandleSlotClick (UIItemSlot clickedSlot) {

        if (clickedSlot == null && cursorSlot.HasItem) // if clicked air while holding block
        {
            byte blockID = cursorSlot.itemSlot.stack.id;
            for(int i = 0; i < cursorSlot.itemSlot.stack.amount; i++)
            {
                controller.toolbar.SpawnObject(blockID); // spawn blocks
            }
            cursorSlot.itemSlot.EmptySlot(); // empty cursor slot
        }
        else if (clickedSlot == null || clickedSlot.itemSlot == null)
            return;

        if (clickedSlot!= null && !cursorSlot.HasItem && !clickedSlot.HasItem) // if no items to move then exit
            return;

        if (clickedSlot != null && clickedSlot.itemSlot.isCreative)
        {
            cursorItemSlot.EmptySlot();
            cursorItemSlot.InsertStack(clickedSlot.itemSlot.stack);
        }

        if (clickedSlot != null && !cursorSlot.HasItem && clickedSlot.HasItem) // pickup stack
        {
            cursorItemSlot.InsertStack(clickedSlot.itemSlot.TakeAll());
            return;
        }

        if (clickedSlot != null && cursorSlot.HasItem && !clickedSlot.HasItem)
        {
            clickedSlot.itemSlot.InsertStack(cursorItemSlot.TakeAll());
            return;
        }

        if (clickedSlot != null && cursorSlot.HasItem && clickedSlot.HasItem) // drop stack into slot
        {
            if (cursorSlot.itemSlot.stack.id != clickedSlot.itemSlot.stack.id)
            {
                ItemStack oldCursorSlot = cursorSlot.itemSlot.TakeAll();
                ItemStack oldSlot = clickedSlot.itemSlot.TakeAll();

                clickedSlot.itemSlot.InsertStack(oldCursorSlot);
                cursorSlot.itemSlot.InsertStack(oldSlot);
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