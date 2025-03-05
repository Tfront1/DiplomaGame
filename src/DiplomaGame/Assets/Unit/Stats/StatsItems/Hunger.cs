public class Hunger : BaseStat
{
    private float _hungerRate;

    public Hunger(float initialHunger = 0, float maxHunger = 100, float hungerRate = 0.1f)
        : base("Hunger", initialHunger, maxHunger)
    {
        _hungerRate = hungerRate;
    }

    public override void Update(float deltaTime)
    {
        Modify(-_hungerRate * deltaTime);
    }
}