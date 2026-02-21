using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{

    public GameObject slotPrefab;

    List<ItemSlot> slots = new List<ItemSlot>();

    public UIItemSlot[] inventorySlots;

    private void Start()
    {
        for (int i = 0; i < inventorySlots.Length; i++) // create inventory storage slots
        {
            ItemSlot slot = new ItemSlot(inventorySlots[i].gameObject.GetComponent<UIItemSlot>()); // create a data driven ItemSlot for each slot
        }
    }

}