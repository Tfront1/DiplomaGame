public interface IStat
{
    float CurrentValue { get; }
    float MaxValue { get; }
    float MinValue { get; }
    void Modify(float amount);
    void SetValue(float value);
    float GetPercentage();
    string ToString();

}