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

    private void Update() {

        if (!controller.inInventoryUI)
        {
            inventory.gameObject.SetActive(false);
            return;
        }
        inventory.gameObject.SetActive(true);

        cursorSlot.transform.position = Input.mousePosition;

        bool clicked = Input.GetMouseButtonDown(0);

        if (clicked)//controller.inputHandler.mine) // controller.inputHandler.mine causes 2 clicks, one on press, one on release...
        {
            HandleSlotClick(CheckForSlot());
        }
        if(controller.inputHandler.sprint && clicked)
        {
            HandleStackQuickMove(CheckForSlot());
        }
    }

    private void HandleStackQuickMove(UIItemSlot clickedSlot)
    {
        if(clickedSlot == null)
            return;

        if(cursorSlot.itemSlot.HasItem)
        {
            // drop 1 item into slot and subtract one from stack
        }
        else if(clickedSlot.inHotbar)
        {
            // move entire stack to first empty slot in inventory slots
            for(int i = 0; i < inventory.inventorySlots.Length; i++)
            {
                if(!inventory.inventorySlots[i].HasItem) // if the inventory slot does not have a stack
                {
                    inventory.inventorySlots[i].InsertStack(clickedSlot.itemSlot.stack); // insert the stack at this position
                    clickedSlot.itemSlot.EmptySlot(); // empty slot
                }
            }
        }
        else
        {
            for(int i = 0; i < controller.toolbar.slots.Length; i++)
            {
                if(!controller.toolbar.slots[i].HasItem) // if the toolbar slot does not have a stack
                {
                    controller.toolbar.slots[i].itemSlot.InsertStack(clickedSlot.itemSlot.stack); // insert the stack at this position
                    clickedSlot.itemSlot.EmptySlot(); // empty slot
                }
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