using Godot;
using game.scenes.entities;
using game.scenes.enemies;

namespace game.scenes.player;

public partial class Player : Entity
{
	[ExportGroup("Movement")]
	[Export] private float _speed = 125f;
	[Export] private float _speedModifier = 1.25f;

	[ExportGroup("Health")]
	[Export] private float _invulnerabilityTime = 0.2f;
	[Export] private ProgressBar _healthBar;

	[ExportGroup("Possession")]
	[Export] private float _possessionRange = 100f;
	[Export] private float _possessionCooldown = 3f;
	[Export] private float _possessionDamage = 1f;
	[Export] private float _possessionDamageInterval = 3f;

	[Export] private ProgressBar _cooldownPossessionBar;
	[Export] private Sprite2D _sprite;
	[Export] private CpuParticles2D _particlePossession;

	private Enemy _possessedEnemy;
	private Enemy _nearestEnemy;

	private float _invulnerabilityTimer;
	private float _possessionCooldownTimer;
	private float _possessionDamageTimer;

	private Vector2 _knockbackVelocity;

	protected override void OnEntityReady()
	{
		base.OnEntityReady();

		UpdateHealthBar();

		if (GodotObject.IsInstanceValid(_cooldownPossessionBar))
		{
			_cooldownPossessionBar.MaxValue = _possessionCooldown;
			_cooldownPossessionBar.Value = 0;
		}
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		float dt = (float)delta;

		_invulnerabilityTimer = Mathf.Max(
			0,
			_invulnerabilityTimer - dt
		);

		_possessionCooldownTimer = Mathf.Max(
			0,
			_possessionCooldownTimer - dt
		);

		UpdateCooldownBar();

		UpdatePossession(delta);

		if (Input.IsActionJustPressed("attack") &&
			GodotObject.IsInstanceValid(_possessedEnemy) &&
			GodotObject.IsInstanceValid(_nearestEnemy))
		{
			_possessedEnemy.TryAttack(_nearestEnemy);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 direction = Input.GetVector(
			"left",
			"right",
			"up",
            "down"
		);

		if (GodotObject.IsInstanceValid(_possessedEnemy))
		{
			_possessedEnemy.MovePossessed(
				direction,
				_speedModifier
			);

			GlobalPosition = _possessedEnemy.GlobalPosition;
			return;
		}

		_knockbackVelocity = _knockbackVelocity.MoveToward(
			Vector2.Zero,
			1200f * (float)delta
		);

		Velocity = direction * _speed + _knockbackVelocity;

		MoveAndSlide();
	}

	#region Health

	public override void TakeDamage(float amount)
	{
		if (GodotObject.IsInstanceValid(_possessedEnemy))
		{
			_possessedEnemy.TakeDamage(amount);
			return;
		}

		if (_invulnerabilityTimer > 0)
			return;

		_invulnerabilityTimer = _invulnerabilityTime;

		base.TakeDamage(amount);
		UpdateHealthBar();
	}

	protected override void Die()
	{
		
	}

	public void Heal(float amount)
	{
		if (amount <= 0)
			return;

		Health = Mathf.Min(
			MaxHealth,
			Health + amount
		);

		UpdateHealthBar();
	}

	public void Knockback(Vector2 attackerPosition, float force)
	{
		if (_invulnerabilityTimer > 0)
			return;

		Vector2 direction = (
			GlobalPosition - attackerPosition
		).Normalized();

		_knockbackVelocity += direction * force;
	}

	private void UpdateHealthBar()
	{
		if (!GodotObject.IsInstanceValid(_healthBar))
			return;

		_healthBar.MaxValue = MaxHealth;
		_healthBar.Value = Health;
	}

	private void UpdateCooldownBar()
	{
		if (!GodotObject.IsInstanceValid(_cooldownPossessionBar))
			return;

		_cooldownPossessionBar.Value = _possessionCooldownTimer;
	}

	#endregion

	#region Possession

	private void UpdatePossession(double delta)
	{
		if (GodotObject.IsInstanceValid(_possessedEnemy) &&
			!_possessedEnemy.IsQueuedForDeletion())
		{
			_possessionDamageTimer -= (float)delta;

			if (_possessionDamageTimer <= 0)
			{
				_possessionDamageTimer = _possessionDamageInterval;

				_possessedEnemy.TakeDamage(_possessionDamage);
				Heal(_possessionDamage);
			}
		}
		else if (_possessedEnemy != null)
		{
			// Захваченный враг был уничтожен.
			ExitPossession();
		}

		UpdateNearestEnemy();

		if (!Input.IsActionJustPressed("possession"))
			return;

		if (GodotObject.IsInstanceValid(_possessedEnemy))
		{
			ExitPossession();
			return;
		}

		if (_possessionCooldownTimer > 0)
			return;

		if (GodotObject.IsInstanceValid(_nearestEnemy))
			EnterPossession();
	}

	private Enemy GetNearestEnemy()
	{
		Vector2 origin = GlobalPosition;

		if (GodotObject.IsInstanceValid(_possessedEnemy))
			origin = _possessedEnemy.GlobalPosition;

		float range = _possessionRange;

		if (GodotObject.IsInstanceValid(_possessedEnemy))
			range = _possessedEnemy.AttackRange;

		Enemy nearest = null;
		float nearestDistance = range * range;

		foreach (Node node in GetTree().GetNodesInGroup("enemies"))
		{
			if (node is not Enemy enemy)
				continue;

			if (!GodotObject.IsInstanceValid(enemy))
				continue;

			if (enemy.IsQueuedForDeletion())
				continue;

			if (enemy.IsPossessed)
				continue;

			float distance = origin.DistanceSquaredTo(
				enemy.GlobalPosition
			);

			if (distance > nearestDistance)
				continue;

			nearestDistance = distance;
			nearest = enemy;
		}

		return nearest;
	}

	private void UpdateNearestEnemy()
	{
		if (_nearestEnemy != null)
			_nearestEnemy.SetOutline(false);

		_nearestEnemy = GetNearestEnemy();

		if (_nearestEnemy == null)
			return;

		if (GodotObject.IsInstanceValid(_possessedEnemy))
			_nearestEnemy.SetOutline(false, true);
		else
			_nearestEnemy.SetOutline(true);
	}

	private void EnterPossession()
	{
		if (!GodotObject.IsInstanceValid(_nearestEnemy))
			return;

		_possessedEnemy = _nearestEnemy;

		_possessedEnemy.SetOutline(true);

		_possessedEnemy.SetPossessed(
			true,
			OriginalCollisionLayer,
			OriginalCollisionMask
		);

		_possessionCooldownTimer = _possessionCooldown;
		_possessionDamageTimer = _possessionDamageInterval;

		_nearestEnemy = null;

		SetCollisionDisabled(true);

		if (GodotObject.IsInstanceValid(_sprite))
			_sprite.Visible = false;

		if (GodotObject.IsInstanceValid(_particlePossession))
			_particlePossession.Restart();

		UpdateNearestEnemy();
	}

	private void ExitPossession()
	{
		if (GodotObject.IsInstanceValid(_possessedEnemy))
		{
			GlobalPosition = _possessedEnemy.GlobalPosition;

			_possessedEnemy.SetPossessed(false);
			_possessedEnemy.SetOutline(false);
		}

		_nearestEnemy?.SetOutline(false);
		_possessedEnemy = null;

		Velocity = Vector2.Zero;

		SetCollisionDisabled(false);

		_invulnerabilityTimer = _invulnerabilityTime;

		if (GodotObject.IsInstanceValid(_sprite))
			_sprite.Visible = true;

		if (GodotObject.IsInstanceValid(_particlePossession))
			_particlePossession.Restart();

		_nearestEnemy = null;

		UpdateNearestEnemy();
	}

	#endregion
}
