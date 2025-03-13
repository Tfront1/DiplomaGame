using System.Collections.Generic;

[System.Serializable]
public class ActionDto
{
	public string name;
	public string type;
    public string interactions;
    public string expectedControlType;

	public List<ActionBindingDto> bindings;
}