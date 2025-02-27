public class Hunger : BaseStat
{
    private float _hungerRate;

    public Hunger(float initialHunger, float maxHunger, float hungerRate = 0.1f)
        : base(initialHunger, maxHunger)
    {
        this._hungerRate = hungerRate;
    }

    public void Update(float deltaTime)
    {
        Modify(-_hungerRate * deltaTime);
    }
}