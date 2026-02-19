using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{

    public GameObject slotPrefab;
    World world;

    List<ItemSlot> slots = new List<ItemSlot>();

    private static int numberOfInventorySlots = 27;

    public ItemSlot[] inventorySlots;

    private void Start()
    {

        inventorySlots = new ItemSlot[numberOfInventorySlots];

        world = World.Instance;

        
        for (int i = 0; i < numberOfInventorySlots; i++) // create inventory storage slots
        {
            
            GameObject newSlot = Instantiate(this.slotPrefab, transform);
            ItemSlot slot = new ItemSlot(newSlot.GetComponent<UIItemSlot>());
            inventorySlots[i] = slot;

            // // fills slots with stacks of 64 blocks of each type for "creative mode"
            // // chose not to do this since eventually will have more blocks than inventory slots
            // if(SettingsStatic.LoadedSettings.developerMode && i > 1 && i <= numberOfInventorySlots)
            // {
            //     ItemStack stack = new ItemStack((byte)i, 64);
            //     inventorySlots[i - 2].InsertStack(stack);
            //     slot.isCreative = true;
            // }
        }
    }

}