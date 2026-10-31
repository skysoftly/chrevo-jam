using Godot;

namespace game.scenes.enemies;

[GlobalClass]
public partial class EnemyType : Resource
{
	[Export] public string Title { get; private set; }
	[Export] public Texture2D Texture { get; private set; }
	[Export] public float MaxHealth { get; private set; }
	[Export] public float Damage { get; private set; }
	[Export] public float Speed { get; private set; }
	[Export] public float KnockbackForce { get; private set; }
	
	[Export] public AttackType AttackType { get; set; }
	[Export] public float AttackCooldown { get; set; } = 2f;
	[Export] public float AttackRange { get; set; } = 100f;
}

public enum AttackType
{
	Melee,
	Projectile,
	Jump,
	Wave
}
