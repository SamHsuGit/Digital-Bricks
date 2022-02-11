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
    public byte blockIndex = 2;
    public bool setNavigate = false;

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

        blockIndex = 2;
        // reset player creative slot
        slots[0].itemSlot.EmptySlot();
        ItemStack stack = new ItemStack(blockIndex, 2);
        slots[0].itemSlot.InsertStack(stack);
    }

    public void toggleNavigate()
    {
        setNavigate = true;
    }

    private void Update()
    {
        if (player == null)
            return;

        if(optionsMenuCanvasGroup.alpha != 1 && (setNavigate || inputHandler.scrollWheel != Vector2.zero))
        {
            if (setNavigate)
                setNavigate = false;

            if (inputHandler.navLeft || inputHandler.scrollWheel.y > 0)
                slotIndex--;
            if (inputHandler.navRight || inputHandler.scrollWheel.y < 0)
                slotIndex++;

            if (slotIndex > slots.Length - 1)
                slotIndex = 0;
            if (slotIndex < 0)
                slotIndex = slots.Length - 1;

            highlight.position = slots[slotIndex].slotIcon.transform.position;
            
            if (slotIndex == 0 && (inputHandler.navUp || inputHandler.navDown))
            {
                if (inputHandler.navUp)
                {
                    blockIndex++;
                }
                if (inputHandler.navDown)
                {
                    blockIndex--;
                }
                
                if (blockIndex > World.Instance.blocktypes.Length - 1) // limit blockIndex to range of defined blocks
                    blockIndex = (byte)(World.Instance.blocktypes.Length - 1);
                if (blockIndex < 2) // cannot select air or barrier blocks
                    blockIndex = 2;
                if (blockIndex == 25 || blockIndex == 26 && inputHandler.navUp) // cannot select reserved blocktypes 25 and 26
                    blockIndex = 27;
                if (blockIndex == 25 || blockIndex == 26 && inputHandler.navDown) // cannot select reserved blocktypes 25 and 26
                    blockIndex = 24;

                slots[slotIndex].itemSlot.EmptySlot();
                ItemStack stack = new ItemStack(blockIndex, blockIndex);
                slots[slotIndex].itemSlot.InsertStack(stack);
            }
            
        }
    }

    public void DropItemsFromSlot(int slotIndexValue)
    {
        if(player != null && slots[slotIndexValue].itemSlot.stack != null)
        {
            int amount = slots[slotIndexValue].itemSlot.stack.amount;
            for (int i = 0; i < amount; i++) // for each item in slot
            {
                Vector3 position = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z) + player.transform.forward * 4;
                byte blockID = slots[slotIndexValue].itemSlot.stack.id;
                controller.SpawnObject(0, blockID, position);
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