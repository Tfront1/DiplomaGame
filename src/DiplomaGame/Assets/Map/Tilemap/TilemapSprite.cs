public class TilemapSprite
{
    public int _id;
    public string _name;

    public override string ToString()
    {
        return _id + " " + _name;
    }

    public TilemapSprite(int id, string name)
    {
        _id = id;
        _name = name;
    }
}