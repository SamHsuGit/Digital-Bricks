public class ItemStack
{
    public byte id;
    public int placedBrickID;
    public bool isPlacedBrick;
    public int amount;

    public ItemStack(byte _id, int _placedBrickID, bool _isPlacedBrick, int _amount)
    {
        this.id = _id;
        this.placedBrickID = _placedBrickID;
        this.isPlacedBrick = _isPlacedBrick;
        this.amount = _amount;
    }
}