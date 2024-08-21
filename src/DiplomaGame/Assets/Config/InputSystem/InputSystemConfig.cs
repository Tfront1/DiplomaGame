using System.Collections.Generic;

public static class InputSystemConfig
{
    public static List<ActionMapConfig> ActionMap { get; set; }

    public class ActionMapConfig
    {
        public string MapName { get; set; }
        public List<ActionsConfig> Actions { get; set; }
    }

    public class ActionsConfig
    {
        public string ActionName { get; set; }
        public string ActionType { get; set; }
        public string ExpectedControlType { get; set; }

        public List<ActionBindingsConfig> ActionBindings { get; set; }

    }

    public class ActionBindingsConfig
    {
        public string BindingPath { get; set; }
        public string BindingGroup { get; set; }
    }

}

