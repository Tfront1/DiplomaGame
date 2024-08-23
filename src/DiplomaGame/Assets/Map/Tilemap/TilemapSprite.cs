public class TilemapSprite
{
    public int _id;

    public override string ToString()
    {
        return _id.ToString();
    }

    public TilemapSprite(int id)
    {
        _id = id;
    }
}