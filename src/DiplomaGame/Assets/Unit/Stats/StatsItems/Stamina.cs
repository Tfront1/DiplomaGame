public class Stamina : BaseStat
{
    private float _regenerationRate;
    private bool _isRegenerating = false;
    private bool _isGettingTired = false;
    private float _tiredRate = 0.1f;

    public Stamina(float initialStamina, float maxStamina, float regenerationRate = 0.5f)
        : base("Stamina", initialStamina, maxStamina)
    {
        _regenerationRate = regenerationRate;
    }

    public override void Update(float deltaTime)
    {
        if (!_isGettingTired && _isRegenerating && CurrentValue < MaxValue)
        {
            Modify(_regenerationRate * deltaTime);
        }
        else if (_isGettingTired)
        {
            Modify(-_tiredRate * deltaTime);
        }
    }
    
    public void StopRegeneration()
    {
        _isRegenerating = false;
    }

    public void StartRegeneration()
    {
        _isRegenerating = true;
    }

    public void StartGetTired(float tiredRate)
    {
        StopRegeneration();
        _isGettingTired = true;
        _tiredRate = tiredRate;
    }

    public void StopGetTired()
    {
        StartRegeneration();
        _isGettingTired = false;
    }
}