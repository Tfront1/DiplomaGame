using System;

public class UnitStats
{
    public Health Health { get; }
    public Armor Armor { get; }
    public Stamina Stamina { get; }
    public Hunger Hunger { get; }

    public delegate void UpdateEventHandler();
    public event UpdateEventHandler OnUpdate;

    public UnitStats(float health, float maxHealth, float armor, float maxArmor,
        float stamina, float maxStamina, float hunger, float maxHunger)
    {
        Health = new Health(health, maxHealth);
        Armor = new Armor(armor, maxArmor);
        Stamina = new Stamina(stamina, maxStamina);
        Hunger = new Hunger(hunger, maxHunger);
    }

    public void Update(float deltaTime)
    {
        var t1 = Stamina.CurrentValue;

        Stamina.Update(deltaTime);
        //Hunger.Update(deltaTime);

        if (Hunger.GetPercentage() < 0.2f)
        {
            Stamina.StopRegeneration();
        }
        else
        {
            Stamina.StartRegeneration();
        }
        
        if (Math.Abs(t1 - Stamina.CurrentValue) > 0.0001f)
        {
            OnUpdate?.Invoke();
        }
    }
}
