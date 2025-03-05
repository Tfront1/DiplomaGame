public class Building
{
	public int Id { get; set; }
	public string Name { get; set; }
	public int WidthCell { get; set; }
	public int HeightCell { get; set; }
    public int VisualWidthCell { get; set; }
    public int VisualHeightCell { get; set; }
    public float Scale { get; set; }
	public bool RandomPos { get; set; }
	public bool HasMargin { get; set; }
	public float MaxHP { get; set; }
	public int BackpackCapacity { get; set; }
}
