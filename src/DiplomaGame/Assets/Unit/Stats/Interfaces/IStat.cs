public interface IStat
{
    string Name { get; }
    float CurrentValue { get; }
    float MaxValue { get; }
    float MinValue { get; }
    void Modify(float amount);
    void SetValue(float value);
    float GetPercentage();
    string ToString();
    void Update(float deltaTime);
}