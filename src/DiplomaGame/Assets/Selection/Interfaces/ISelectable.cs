namespace Selection.Interfaces
{
    public interface ISelectable
    {
        bool IsDestroyed { get; }
        void OnSelect();
        void OnDeselect();
    }
}