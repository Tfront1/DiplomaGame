public class Stamina : BaseStat
{
    private float _regenerationRate;
    private bool _isRegenerating = false;
    private bool _isGettingTired = false;
    private float _tiredRate = 0.1f;

    public Stamina(float initialStamina, float maxStamina, float regenerationRate = 1f)
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
            Modify(_tiredRate * deltaTime);
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
        _isGettingTired = true;
        _tiredRate = tiredRate;
    }

    public void StopGetTired(float tiredRate)
    {
        _isGettingTired = false;
        _tiredRate = tiredRate;
    }
}