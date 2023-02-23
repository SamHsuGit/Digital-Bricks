using UnityEngine;

public class Toolbar : MonoBehaviour
{
    public GameObject player;
    public GameObject optionsMenu;
    public UIItemSlot[] slots;
    public RectTransform highlight;
    public int slotIndex = 0;
    public byte creativeBlockID = 2;
    public bool setNavigate = false;

    CanvasGroup optionsMenuCanvasGroup;
    InputHandler inputHandler;
    Controller controller;

    private void Awake()
    {
        for (byte i = 2; i < 11; i++)
        {
            UIItemSlot s = slots[i - 2];
            ItemSlot slot = new ItemSlot(s, null);
        }

        optionsMenuCanvasGroup = optionsMenu.GetComponent<CanvasGroup>();
        inputHandler = player.GetComponent<InputHandler>();
        controller = player.GetComponent<Controller>();
    }

    private void Start()
    {
        if (SettingsStatic.LoadedSettings.creativeMode || World.Instance.worldData.creativeMode)
            EmptyAllSlots();
        else
            SetInventoryFromSave();
    }

    private void EmptyAllSlots()
    {
        for(int i = 0; i < controller.toolbar.slots.Length; i++)
        {
            if(SettingsStatic.LoadedSettings.creativeMode && i == 0)
                ResetCreativeSlot();
            else
                controller.toolbar.slots[i].itemSlot.EmptySlot();
        }
    }

    private void ResetCreativeSlot()
    {
        creativeBlockID = 2;
        ItemStack creativeStack = new ItemStack(creativeBlockID, creativeBlockID);
        controller.toolbar.slots[0].itemSlot.EmptySlot();
        controller.toolbar.slots[0].itemSlot.InsertStack(creativeStack);
    }

    private void SetInventoryFromSave() // moved from player to Toolbar to ensure the slots exist before trying to set inventory from save
    {
        int[] playerStats;
        if (Settings.Platform != 2)
            playerStats = controller.player.playerStats; // load current player stats from save file
        else
            playerStats = SaveSystem.GetDefaultPlayerStats(player);

        // Set player inventory
        for (int i = 4; i < 22; i += 2)
        {
            int slotIndex = (i - 4) / 2;
            UIItemSlot slot = controller.toolbar.slots[slotIndex];
            int blockID = playerStats[i];
            int qty = playerStats[i + 1];

            if (blockID != 0)
            {
                ItemStack stack = new ItemStack((byte)blockID, qty);
                if (slot.itemSlot.HasItem)
                    slot.itemSlot.EmptySlot();
                slot.itemSlot.InsertStack(stack);

                // for creative slot, set slot index to saved blockID
                if (slotIndex == 0)
                    creativeBlockID = (byte)blockID; // set creative slot to saved value
            }
            else
            {
                // for creative mode and slot and blockID < 2
                if (SettingsStatic.LoadedSettings.creativeMode && slotIndex == 0 && blockID < 2)
                {
                    // if no saved blockID, then set creative slot to blockID 2
                    ResetCreativeSlot();
                }
            }
        }
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
            
            if (SettingsStatic.LoadedSettings.creativeMode && slotIndex == 0 && (inputHandler.navUp || inputHandler.navDown))
            {
                if (inputHandler.navUp)
                {
                    creativeBlockID++;
                }
                if (inputHandler.navDown)
                {
                    creativeBlockID--;
                }
                
                if (creativeBlockID > World.Instance.blockTypes.Length - 1) // limit blockIndex to range of defined blocks
                    creativeBlockID = (byte)(World.Instance.blockTypes.Length - 1);
                if (creativeBlockID < 2) // cannot select air or barrier blocks
                    creativeBlockID = 2;
                if (creativeBlockID == 25 || creativeBlockID == 26 && inputHandler.navUp) // cannot select reserved blocktypes 25 and 26
                    creativeBlockID = 27;
                if (creativeBlockID == 25 || creativeBlockID == 26 && inputHandler.navDown) // cannot select reserved blocktypes 25 and 26
                    creativeBlockID = 24;

                slots[slotIndex].itemSlot.EmptySlot();
                ItemStack stack = new ItemStack(creativeBlockID, creativeBlockID);
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
                if(Settings.OnlinePlay)
                    controller.CmdSpawnObject(0, blockID, position);
                else
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