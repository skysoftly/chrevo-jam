using Godot;
using game.scenes.entities;

namespace game.scenes.enemies;

public abstract partial class Enemy : Entity
{
	[Export] protected Sprite2D Sprite { get; set; }
	[Export] public Node2D PlayerReference { get; set; }

	[Export] public float Speed { get; set; } = 100f;
	[ExportGroup("Damage")]
	[Export] public float AttackRange { get; set; } = 50f;
	[Export] public float AttackCooldown { get; set; } = 2f;
	[Export] public float Damage { get; set; } = 1f;
	[Export] public float DamageCooldown { get; set; } = 0.2f;

	private float _damageTimer;
	
	[ExportGroup("Separation")]
	[Export] public float SeparationRadius { get; set; } = 35f;
	[Export] public float SeparationForce { get; set; } = 180f;
	
	[ExportGroup("Outline")]
	[Export] private ShaderMaterial _outlineMaterial;

	private ShaderMaterial _materialInstance;
	
	public bool IsPossessed { get; private set; }
	public bool IsAttacking { get; protected set; }

	private float _attackTimer;

	protected override void OnEntityReady()
	{
		base.OnEntityReady();

		AddToGroup("enemies");

		GD.Print($"Enemy: {Name}");
		GD.Print($"Sprite: {Sprite?.GetPath() ?? "NULL"}");
		GD.Print($"Material: {_outlineMaterial?.ResourcePath ?? "NULL"}");
		
		if (_outlineMaterial != null && Sprite != null)
		{
			_materialInstance = (ShaderMaterial)_outlineMaterial.Duplicate();
			Sprite.Material = _materialInstance;

			SetOutline(false);
		}
		else
		{
			GD.PushWarning($"{Name}: Outline Sprite or Material is missing!");
		}
	}

	public override void _Process(double delta)
	{
		_damageTimer = Mathf.Max(0, _damageTimer - (float)delta);
		
		_attackTimer = Mathf.Max(
			0,
			_attackTimer - (float)delta
		);

		if (IsPossessed || IsAttacking)
			return;

		if (!IsValidTarget(PlayerReference))
			return;

		float distance = GlobalPosition.DistanceTo(
			PlayerReference.GlobalPosition
		);

		if (distance <= AttackRange && _attackTimer <= 0)
		{
			TryAttack(PlayerReference);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (IsAttacking)
		{
			UpdateAttack(delta);
			return;
		}

		if (IsPossessed)
			return;

		MoveEnemy(delta);
	}
	protected Vector2 GetSeparationDirection()
	{
		Vector2 separation = Vector2.Zero;

		foreach (Node node in GetTree().GetNodesInGroup("enemies"))
		{
			if (node == this || node is not Enemy other)
				continue;

			if (other.IsQueuedForDeletion())
				continue;

			Vector2 offset = GlobalPosition - other.GlobalPosition;
			float distance = offset.Length();

			if (distance <= 0 || distance >= SeparationRadius)
				continue;

			float strength = 1f - distance / SeparationRadius;

			separation += offset.Normalized() * strength;
		}

		return separation;
	}

	public override void TakeDamage(float amount)
	{
		// Во время атаки враг неуязвим.
		if (IsAttacking)
			return;

		// Ограничиваем частоту получения урона.
		if (_damageTimer > 0)
			return;

		_damageTimer = DamageCooldown;

		base.TakeDamage(amount);
	}
	
	protected abstract void MoveEnemy(double delta);

	protected abstract void StartAttack(Node2D target);

	protected virtual void UpdateAttack(double delta)
	{
	}

	public bool TryAttack(Node2D target)
	{
		if (IsAttacking || _attackTimer > 0)
			return false;

		if (!IsValidTarget(target))
			return false;

		_attackTimer = AttackCooldown;

		StartAttack(target);

		return true;
	}

	public void MovePossessed(Vector2 direction, float speedMultiplier = 1f)
	{
		if (!IsPossessed || IsAttacking)
			return;

		Velocity = direction * Speed * speedMultiplier;

		MoveAndSlide();
	}

	public override void SetPossessed(
		bool possessed,
		uint playerLayer = 0,
		uint playerMask = 0)
	{
		IsPossessed = possessed;

		Velocity = Vector2.Zero;

		base.SetPossessed(
			possessed,
			playerLayer,
			playerMask
		);
	}

	protected bool IsValidTarget(Node2D target)
	{
		return GodotObject.IsInstanceValid(target)
			   && !target.IsQueuedForDeletion();
	}

	protected void DamageTarget(Node2D target)
	{
		if (!IsValidTarget(target))
			return;

		if (target is IHealth health)
			health.TakeDamage(Damage);
	}
	
	public void SetOutline(bool enabled, bool attack = false)
	{
		if (_materialInstance == null)
			return;

		_materialInstance.SetShaderParameter("enabled", enabled);
		_materialInstance.SetShaderParameter("attack", attack);
	}
}
