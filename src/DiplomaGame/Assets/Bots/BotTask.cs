namespace Bots
{
    public class BotTask
    {
        public object Target { get; set; }
        public BotTaskType Type { get; set; }
    }

    public enum BotTaskType
    {
        Build,
        Craft,
        GatherResource,
        DefendTown,
        Attack,
        Duel,
        BringBackResources
    }
}