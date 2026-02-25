public class ItemStack
{
    public byte id;
    public string placedBrickID;
    public int placedBrickColorID;
    public int amount;

    public ItemStack(byte id, string placedBrickID, int placedBrickColorID, int amount)
    {
        this.id = id;
        this.placedBrickID = placedBrickID;
        this.placedBrickColorID = placedBrickColorID;
        this.amount = amount;
    }
}