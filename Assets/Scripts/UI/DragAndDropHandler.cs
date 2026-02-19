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

    public CreativeInventory creativeInventory;

    private void Start()
    {
        cursorItemSlot = new ItemSlot(cursorSlot);
    }

    private void Update() {

        if (!controller.inInventoryUI)
        {
            creativeInventory.gameObject.SetActive(false);
            return;
        }
        creativeInventory.gameObject.SetActive(true);

        cursorSlot.transform.position = Input.mousePosition;

        if (Input.GetMouseButtonDown(0))//controller.inputHandler.mine) // controller.inputHandler.mine causes 2 clicks, one on press, one on release...
        {
            HandleSlotClick(CheckForSlot());
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

        if (clickedSlot != null && clickedSlot.itemSlot.isCreative) {

            cursorItemSlot.EmptySlot();
            cursorItemSlot.InsertStack(clickedSlot.itemSlot.stack);

        }

        if (clickedSlot != null && !cursorSlot.HasItem && clickedSlot.HasItem) {

            cursorItemSlot.InsertStack(clickedSlot.itemSlot.TakeAll());
            return;

        }

        if (clickedSlot != null && cursorSlot.HasItem && !clickedSlot.HasItem) {

            clickedSlot.itemSlot.InsertStack(cursorItemSlot.TakeAll());
            return;

        }

        if (clickedSlot != null && cursorSlot.HasItem && clickedSlot.HasItem) {

            if (cursorSlot.itemSlot.stack.id != clickedSlot.itemSlot.stack.id) {

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