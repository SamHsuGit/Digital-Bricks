public class ItemStack
{
    public byte id;
    public string placedBrickID;
    public int amount;

    public ItemStack(byte id, string placedBrickID, int amount)
    {
        this.id = id;
        this.placedBrickID = placedBrickID;
        this.amount = amount;
    }
}