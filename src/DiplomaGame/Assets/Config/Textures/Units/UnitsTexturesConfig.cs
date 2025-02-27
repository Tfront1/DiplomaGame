using System.Collections.Generic;

public static class UnitsTexturesConfig
{
    public static string TexturesPath { get; set; }
    public static List<UnitsGroup> UnitsGroupsList { get; set; }
    
    public class UnitsGroup
    {
        public int UnitId { get; set; }
        public string UnitFolder { get; set; }
        public List<ActionGroup> ActionGroupsList { get; set; }
    }

    public class ActionGroup
    {
        public int ActionId { get; set; }
        public string ActionName { get; set; }
        public string ActionFolder { get; set; }
        public List<UnitTexture> UnitTexturesList { get; set; }
    }

    public class UnitTexture
    {
        public int Id { get; set; }
        public string TextureFileName { get; set; }
    }
}
