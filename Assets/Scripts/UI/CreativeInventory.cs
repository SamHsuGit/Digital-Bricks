using System.Collections.Generic;
using UnityEngine;

public class CreativeInventory : MonoBehaviour
{

    public GameObject slotPrefab;
    World world;

    List<ItemSlot> slots = new List<ItemSlot>();

    private void Start()
    {

        world = World.Instance;

        
        for (int i = 2; i < world.blockTypes.Length; i++) // start at i = 2 to skip barrier and air blocks
        {

            GameObject newSlot = Instantiate(slotPrefab, transform);

            // fills slots with stacks of 64 blocks of each type for "creative mode"
            ItemStack stack = new ItemStack((byte)0, 64);
            ItemSlot slot = new ItemSlot(newSlot.GetComponent<UIItemSlot>(), stack);
            slot.isCreative = true;

        }
    }

}