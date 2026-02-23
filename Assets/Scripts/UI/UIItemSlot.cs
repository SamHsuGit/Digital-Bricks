using UnityEngine;
using UnityEngine.UI;

public class UIItemSlot : MonoBehaviour
{

    public bool isLinked = false;
    public bool inHotbar = false;

    public bool isCrafting = false;

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

    public void Link(ItemSlot itemSlot)
    {
        this.itemSlot = itemSlot;
        isLinked = true;
        this.itemSlot.LinkUISlot(this);
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
        if (itemSlot != null && itemSlot.HasItem && itemSlot.stack.id <= World.Instance.blockTypes.Length)
        {
            slotIcon.sprite = World.Instance.blockTypes[itemSlot.stack.id].icon;
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

    public ItemSlot(UIItemSlot uiItemSlot)
    {
        stack = null;
        this.uiItemSlot = uiItemSlot;
        this.uiItemSlot.Link(this);
    }

    public ItemSlot(UIItemSlot uiItemSlot, ItemStack stack)
    {
        this.stack = stack;
        this.uiItemSlot = uiItemSlot;
        this.uiItemSlot.Link(this);
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
            return _amt; // do not change value
        }
        else if (amt <= stack.amount) // if amount request to take is less than or equal to whats available in stack
        {
            stack.amount -= amt; // subtract one from amount
            uiItemSlot.UpdateSlot();
            return amt;
        }
        // else if (stack.amount - amt <= 0) // if removing the amount would result in negative or zero, empty slot completely (causes issue diving by zero?)
        // {
        //     EmptySlot();
        //     return amt;
        // }
        else
            return amt;
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

    public void InsertStack(ItemStack stack)
    {
        this.stack = stack;
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