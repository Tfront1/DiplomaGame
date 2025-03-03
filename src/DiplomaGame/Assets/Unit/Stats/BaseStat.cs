using UnityEngine;

public class BaseStat : IStat
{
    private float _currentValue;
    private float _maxValue;
    private float _minValue;

    public string Name { get; private set; }
    public float CurrentValue => _currentValue;
    public float MaxValue => _maxValue;
    public float MinValue => _minValue;
    
    public BaseStat(string name, float initialValue, float maxValue, float minValue = 0)
    {
        Name = name;
        _maxValue = maxValue;
        _minValue = minValue;
        _currentValue = Mathf.Clamp(initialValue, minValue, maxValue);
    }

    public virtual void Modify(float amount)
    {
        SetValue(_currentValue + amount);
    }

    public virtual void SetValue(float value)
    {
        _currentValue = Mathf.Clamp(value, _minValue, _maxValue);
    }

    public float GetPercentage()
    {
        return _currentValue / _maxValue;
    }

    public override string ToString()
    {
        return $"{CurrentValue:F1}/{MaxValue:F1} ({GetPercentage():P1})";
    }

    public virtual void Update(float deltaTime)
    {
    }
}