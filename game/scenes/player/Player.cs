
using Godot;
using game.scenes.entities;
using game.scenes.enemies;
using game.scenes.bosses;

namespace game.scenes.player;

public partial class Player : Entity
{
	[Export] private AudioStreamPlayer2D _possessionAudio;
	[Export] private Control _gameOverMenu;
	
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
	[Export] private CpuParticles2D _particlePossession;

	[ExportGroup("Attack")]
	[Export] private ProgressBar _cooldownAttackBar;

	private Enemy _possessedEnemy;
	private Enemy _nearestEnemy;
	private Boss _nearestBoss;

	private float _invulnerabilityTimer;
	private float _possessionCooldownTimer;
	private float _possessionDamageTimer;

	private Vector2 _knockbackVelocity;

	protected override void OnEntityReady()
	{
		base.OnEntityReady();

		if (GodotObject.IsInstanceValid(_cooldownAttackBar))
			_cooldownAttackBar.Visible = false;

		_possessionDamageTimer = _possessionDamageInterval;
		_possessionCooldownTimer = _possessionCooldown;

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
			0f,
			_invulnerabilityTimer - dt
		);

		_possessionCooldownTimer = Mathf.Min(
			_possessionCooldown,
			_possessionCooldownTimer + dt
		);

		UpdateCooldownBar();
		UpdatePossession(delta);

		if (!GodotObject.IsInstanceValid(_possessedEnemy))
			return;

		if (_possessedEnemy.IsDead)
		{
			ExitPossession();
			return;
		}

		if (Input.IsActionJustPressed("attack"))
		{
			if (IsValidBoss(_nearestBoss))
				_possessedEnemy.TryAttack(_nearestBoss);
			else if (IsValidEnemy(_nearestEnemy))
				_possessedEnemy.TryAttack(_nearestEnemy);
		}

		if (GodotObject.IsInstanceValid(_cooldownAttackBar))
			_cooldownAttackBar.Value = _possessedEnemy.AttackTimer;
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
			if (_possessedEnemy.IsDead)
			{
				ExitPossession();
			}
			else
			{
				_possessedEnemy.MovePossessed(
					direction,
					_speedModifier
				);

				GlobalPosition = _possessedEnemy.GlobalPosition;
				return;
			}
		}

		if (direction.X > 0f)
			Sprite.FlipH = false;
		else if (direction.X < 0f)
			Sprite.FlipH = true;

		_knockbackVelocity = _knockbackVelocity.MoveToward(
			Vector2.Zero,
			1200f * (float)delta
		);

		Velocity = direction * _speed + _knockbackVelocity;

		MoveAndSlide();
	}

	// --------------------------------------------------
	// HEALTH
	// --------------------------------------------------

	public override void TakeDamage(float amount)
	{
		if (GodotObject.IsInstanceValid(_possessedEnemy))
		{
			_possessedEnemy.TakeDamage(amount);
			return;
		}

		if (_invulnerabilityTimer > 0f)
			return;

		_invulnerabilityTimer = _invulnerabilityTime;

		base.TakeDamage(amount);

		UpdateHealthBar();
	}
	protected override void Die()
	{
		if (IsDead)
			return;

		GetTree().Paused = true;
		_gameOverMenu.Show();
	}

	public void Heal(float amount)
	{
		if (amount <= 0f || IsDead)
			return;

		Health = Mathf.Min(
			MaxHealth,
			Health + amount
		);

		UpdateHealthBar();
	}

	public void Knockback(Vector2 attackerPosition, float force)
	{
		if (_invulnerabilityTimer > 0f)
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

	// --------------------------------------------------
	// POSSESSION
	// --------------------------------------------------

	private void UpdatePossession(double delta)
	{
		_possessionDamageTimer -= (float)delta;

		if (GodotObject.IsInstanceValid(_possessedEnemy) &&
			!_possessedEnemy.IsDead)
		{
			if (_possessionDamageTimer <= 0f)
			{
				_possessionDamageTimer = _possessionDamageInterval;

				_possessedEnemy.TakeDamage(_possessionDamage);
				Heal(_possessionDamage);
			}
		}
		else if (_possessedEnemy != null)
		{
			ExitPossession();
		}

		if (!GodotObject.IsInstanceValid(_possessedEnemy))
		{
			if (_possessionDamageTimer <= 0f)
			{
				_possessionDamageTimer = _possessionDamageInterval;
				TakeDamage(_possessionDamage);
			}
		}

		UpdateNearestEnemy();

		if (!Input.IsActionJustPressed("possession"))
			return;

		if (GodotObject.IsInstanceValid(_possessedEnemy))
		{
			ExitPossession();
			return;
		}

		if (_possessionCooldownTimer < _possessionCooldown)
			return;

		if (IsValidEnemy(_nearestEnemy))
			EnterPossession();
	}

	private Enemy GetNearestEnemy()
	{
		Vector2 origin = GlobalPosition;
		float range = _possessionRange;

		if (GodotObject.IsInstanceValid(_possessedEnemy))
		{
			origin = _possessedEnemy.GlobalPosition;
			range = _possessedEnemy.AttackRange;
		}

		Enemy nearest = null;
		float nearestDistance = range * range;

		foreach (Node node in GetTree().GetNodesInGroup("enemies"))
		{
			if (node is not Enemy enemy)
				continue;

			if (!IsValidEnemy(enemy))
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

	private Boss GetNearestBoss()
	{
		if (!GodotObject.IsInstanceValid(_possessedEnemy))
			return null;

		Vector2 origin = _possessedEnemy.GlobalPosition;
		float range = _possessedEnemy.AttackRange;

		Boss nearest = null;
		float nearestDistance = range * range;

		foreach (Node node in GetTree().GetNodesInGroup("bosses"))
		{
			if (node is not Boss boss)
				continue;

			if (!IsValidBoss(boss))
				continue;

			float distance = origin.DistanceSquaredTo(
				boss.GlobalPosition
			);

			if (distance > nearestDistance)
				continue;

			nearestDistance = distance;
			nearest = boss;
		}

		return nearest;
	}

	private bool IsValidEnemy(Enemy enemy)
	{
		return GodotObject.IsInstanceValid(enemy)
			&& !enemy.IsQueuedForDeletion()
			&& !enemy.IsDead;
	}

	private bool IsValidBoss(Boss boss)
	{
		return GodotObject.IsInstanceValid(boss)
			&& !boss.IsQueuedForDeletion()
			&& !boss.IsDead;
	}

	private void UpdateNearestEnemy()
	{
		if (IsValidEnemy(_nearestEnemy))
			_nearestEnemy.SetOutline(false);

		if (IsValidBoss(_nearestBoss))
			_nearestBoss.SetOutline(false);

		_nearestEnemy = GetNearestEnemy();

		// Не вселены: ищем только врагов для вселения.
		if (!GodotObject.IsInstanceValid(_possessedEnemy))
		{
			_nearestBoss = null;

			if (IsValidEnemy(_nearestEnemy))
				_nearestEnemy.SetOutline(true);

			return;
		}

		// Вселены: отдельно ищем босса для атаки.
		_nearestBoss = GetNearestBoss();

		if (IsValidBoss(_nearestBoss))
		{
			_nearestBoss.SetOutline(true, true);
		}
		else if (IsValidEnemy(_nearestEnemy))
		{
			_nearestEnemy.SetOutline(false, true);
		}
	}

	private void EnterPossession()
	{
		if (!IsValidEnemy(_nearestEnemy))
			return;

		_possessedEnemy = _nearestEnemy;

		_possessedEnemy.Died += OnPossessedEnemyDied;

		if (GodotObject.IsInstanceValid(_cooldownAttackBar))
		{
			_cooldownAttackBar.MaxValue = _possessedEnemy.AttackCooldown;
			_cooldownAttackBar.Value = 0;
			_cooldownAttackBar.Visible = true;
		}

		_possessedEnemy.SetOutline(true);

		_possessedEnemy.SetPossessed(
			true,
			OriginalCollisionLayer,
			OriginalCollisionMask
		);

		_possessionCooldownTimer = 0f;
		_possessionDamageTimer = _possessionDamageInterval;

		_nearestEnemy = null;
		_nearestBoss = null;

		SetCollisionDisabled(true);

		if (GodotObject.IsInstanceValid(Sprite))
			Sprite.Visible = false;

		if (GodotObject.IsInstanceValid(_particlePossession))
			_particlePossession.Restart();

		if (GodotObject.IsInstanceValid(_possessionAudio))
			_possessionAudio.Play();

		UpdateNearestEnemy();
	}

	private void OnPossessedEnemyDied(Enemy enemy)
	{
		if (_possessedEnemy != enemy)
			return;

		ExitPossession();
	}

	private void ExitPossession()
	{
		Enemy enemy = _possessedEnemy;

		if (GodotObject.IsInstanceValid(enemy))
		{
			GlobalPosition = enemy.GlobalPosition;

			enemy.Died -= OnPossessedEnemyDied;

			if (!enemy.IsDead)
			{
				enemy.SetPossessed(false);
				enemy.SetOutline(false);
			}
		}

		if (IsValidEnemy(_nearestEnemy))
			_nearestEnemy.SetOutline(false);

		if (IsValidBoss(_nearestBoss))
			_nearestBoss.SetOutline(false);

		_possessedEnemy = null;
		_nearestEnemy = null;
		_nearestBoss = null;

		if (GodotObject.IsInstanceValid(_cooldownAttackBar))
			_cooldownAttackBar.Visible = false;

		Velocity = Vector2.Zero;

		SetCollisionDisabled(false);

		_invulnerabilityTimer = _invulnerabilityTime;

		if (GodotObject.IsInstanceValid(Sprite))
			Sprite.Visible = true;

		if (GodotObject.IsInstanceValid(_particlePossession))
			_particlePossession.Restart();

		if (GodotObject.IsInstanceValid(_possessionAudio))
			_possessionAudio.Play();

		UpdateNearestEnemy();
	}
	
	
}
