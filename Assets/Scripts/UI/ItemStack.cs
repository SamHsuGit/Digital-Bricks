public class ItemStack
{
    public byte id;
    public string placedBrickID;
    public bool isPlacedBrick;
    public int amount;

    public ItemStack(byte _id, string _placedBrickID, bool _isPlacedBrick, int _amount)
    {
        this.id = _id;
        this.placedBrickID = _placedBrickID;
        this.isPlacedBrick = _isPlacedBrick;
        this.amount = _amount;
    }
}