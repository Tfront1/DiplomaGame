using UnityEngine;

public class UnitStats : IUnitStats
{
    public Health Health { get; }
    public Armor Armor { get; }
    public Stamina Stamina { get; }
    public Hunger Hunger { get; }

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
        Stamina.Update(deltaTime);
        Hunger.Update(deltaTime);

        if (Hunger.GetPercentage() < 0.2f)
        {
            Stamina.StopRegeneration();
        }
        else
        {
            Stamina.StartRegeneration();
        }

        Debug.Log($"Stamina: {Stamina}\n Hunger: {Hunger}");
    }
}
