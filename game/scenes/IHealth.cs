namespace game.scenes;

public interface IHealth
{
    float MaxHealth { get; }
    float Health { get; }

    void TakeDamage(float amount);
}