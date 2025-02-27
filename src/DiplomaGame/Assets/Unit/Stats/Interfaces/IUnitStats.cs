public interface IUnitStats
{
    Health Health { get; }
    Armor Armor { get; }
    Stamina Stamina { get; }
    Hunger Hunger { get; }
    void Update(float deltaTime);
}