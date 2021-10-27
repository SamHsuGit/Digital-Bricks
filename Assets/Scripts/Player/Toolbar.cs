using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Toolbar : MonoBehaviour
{
    public GameObject player;
    public GameObject optionsMenu;
    public UIItemSlot[] slots;
    public RectTransform highlight;
    public int slotIndex = 0;

    CanvasGroup optionsMenuCanvasGroup;
    InputHandler inputHandler;
    Controller controller;

    private void Awake()
    {
        optionsMenuCanvasGroup = optionsMenu.GetComponent<CanvasGroup>();
        inputHandler = player.GetComponent<InputHandler>();
        controller = player.GetComponent<Controller>();
    }

    private void Start()
    {
        
        for (byte i = 2; i < 11; i++)
        {
            UIItemSlot s = slots[i - 2];
            ItemSlot slot = new ItemSlot(s, null);
        }
    }

    private void Update()
    {
        if (player == null)
            return;

        if(optionsMenuCanvasGroup.alpha != 1)
        {
            if (inputHandler.navigate != Vector2.zero || inputHandler.scrollWheel != Vector2.zero)
            {
                if (inputHandler.navigate.x < 0 || inputHandler.navigate.y < 0 || inputHandler.scrollWheel.y > 0)
                    slotIndex--;
                if (inputHandler.navigate.x > 0 || inputHandler.navigate.y > 0 || inputHandler.scrollWheel.y < 0)
                    slotIndex++;
            }

            if (slotIndex > slots.Length - 1)
                slotIndex = 0;
            if (slotIndex < 0)
                slotIndex = slots.Length - 1;

            highlight.position = slots[slotIndex].slotIcon.transform.position;
        }
    }

    public void DropItemsFromSlot(int slotIndexValue)
    {
        if(player != null && slots[slotIndexValue].itemSlot.stack != null)
        {
            int amount = slots[slotIndexValue].itemSlot.stack.amount;
            for (int i = 0; i < amount; i++) // for each item in slot
            {
                Vector3 pos = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z) + player.transform.forward * 4;
                byte blockID = slots[slotIndexValue].itemSlot.stack.id;
                if(!Settings.OnlinePlay)
                    controller.SpawnVoxelRbAtPos(pos, blockID);
                else
                    controller.CmdSpawnRbFromInventory(pos, blockID);
            }
            slots[slotIndexValue].itemSlot.EmptySlot();
        }
    }

    public void EmptyToolbar()
    {
        for (int i = 0; i < slots.Length; i++) // for all slots in toolbar
        {
            slots[i].itemSlot.EmptySlot();
        }
    }
}