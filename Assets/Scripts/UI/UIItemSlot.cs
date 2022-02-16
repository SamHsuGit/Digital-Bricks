using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIItemSlot : MonoBehaviour
{

    public bool isLinked = false;
    public ItemSlot itemSlot;
    public Image slotImage;
    public Image slotIcon;
    public Text slotAmount;

    public bool HasItem
    {
        get
        {
            if (itemSlot == null)
                return false;
            else
                return itemSlot.HasItem;
        }
    }

    public void Link(ItemSlot _itemSlot)
    {
        itemSlot = _itemSlot;
        isLinked = true;
        itemSlot.LinkUISlot(this);
        UpdateSlot();
    }

    public void UnLink()
    {
        itemSlot.unLinkUISlot();
        itemSlot = null;
        UpdateSlot();
    }

    public void UpdateSlot()
    {
        if (itemSlot != null && itemSlot.HasItem)
        {
            slotIcon.sprite = World.Instance.blocktypes[itemSlot.stack.id].icon;
            slotAmount.text = itemSlot.stack.amount.ToString();
            slotIcon.enabled = true;
            slotAmount.enabled = true;
        }
        else
            Clear();
    }

    public void Clear()
    {
        slotIcon.sprite = null;
        slotAmount.text = "";
        slotIcon.enabled = false;
        slotAmount.enabled = false;
    }

    private void OnDestroy()
    {
        if (itemSlot != null)
            itemSlot.unLinkUISlot();
    }
}

public class ItemSlot
{
    public ItemStack stack = null;
    private UIItemSlot uiItemSlot = null;

    public bool isCreative;

    public ItemSlot(UIItemSlot _uiItemSlot)
    {
        stack = null;
        uiItemSlot = _uiItemSlot;
        uiItemSlot.Link(this);
    }

    public ItemSlot(UIItemSlot _uiItemSlot, ItemStack _stack)
    {
        stack = _stack;
        uiItemSlot = _uiItemSlot;
        uiItemSlot.Link(this);
    }

    public void LinkUISlot(UIItemSlot uiSlot)
    {
        uiItemSlot = uiSlot;
    }

    public void unLinkUISlot()
    {
        uiItemSlot = null;
    }

    public void EmptySlot()
    {
        stack = null;
        if (uiItemSlot != null)
            uiItemSlot.UpdateSlot();
    }

    public int Take(int amt)
    {
        if (amt > stack.amount) // if amount request to take is more than whats available in stack
        {
            int _amt = stack.amount;
            //EmptySlot();
            return _amt;
        }
        else if (amt <= stack.amount) // if amount request to take is less than or equal to whats available in stack
        {
            stack.amount -= amt;
            uiItemSlot.UpdateSlot();
            return amt;
        }
        else
        {
            //EmptySlot();
            return amt;
        }
    }

    public int Give(int amt)
    {
        stack.amount += amt;
        uiItemSlot.UpdateSlot();
        return amt;
    }

    public ItemStack TakeAll()
    {

        ItemStack handOver = new ItemStack(stack.id, stack.amount);
        EmptySlot();
        return handOver;

    }

    public void InsertStack(ItemStack _stack)
    {
        stack = _stack;
        uiItemSlot.UpdateSlot();
    }

    public bool HasItem
    {
        get
        {
            if (stack != null)
                return true;
            else
                return false;
        }
    }
}