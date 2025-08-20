public class Health : BaseStat
{
    public Health(float initialHealth, float maxHealth) : base("Health", initialHealth, maxHealth) { }

    public void Attack(float damage)
    {
        _currentValue -= damage;
    }
}