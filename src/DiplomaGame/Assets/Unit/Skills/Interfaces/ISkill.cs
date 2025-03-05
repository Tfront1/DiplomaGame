public interface ISkill
{
    string Name { get; }
    float CurrentLevel { get; }
    float MaxLevel { get; }
    float Experience { get; }
    float ExperienceToNextLevel { get; }

    void AddExperience(float amount);
    bool CanLevelUp();
    void LevelUp();
    float GetProgressPercentage();
    string ToString();
}