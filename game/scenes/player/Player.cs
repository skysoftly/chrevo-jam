using System;
using System.Collections.Generic;
using System.Linq;
using game.scenes.enemies;
using Godot;

namespace game.scenes.player;

public partial class Player : CharacterBody2D, IHealth
{
	#region Movement

	private float _speed = 125f;
	private float _speedModifier = 1.25f;
	private Vector2 _knockbackVelocity;


	private Vector2 GetDirection()
	{
		return Input.GetVector("left", "right", "up", "down");
	}

	public uint OriginalCollisionLayer { get; set; }
	public uint OriginalCollisionMask { get; set; }

	private void MoveHandler(double delta)
	{
		_knockbackVelocity = _knockbackVelocity.MoveToward(
			Vector2.Zero, 1200f * (float)delta
		);

		if (_possessionEnemy == null)
		{
			Velocity = GetDirection() * _speed * _speedModifier;
			MoveAndSlide();
		}
		else
		{
			Velocity = Vector2.Zero;

			if (IsInstanceValid(_possessionEnemy))
			{
				_possessionEnemy.Velocity = GetDirection() * _possessionEnemy.Speed * _speedModifier;

				_possessionEnemy.MoveAndSlide();

				GlobalPosition = _possessionEnemy.GlobalPosition;
			}
		}
	}

	public void Knockback(Vector2 attackerPosition, float force)
	{
		if (_invulnerabilityTimer > 0)
			return;

		Vector2 direction = (GlobalPosition - attackerPosition).Normalized();
		_knockbackVelocity += direction * force;
	}

	#endregion

	#region Health

	[Export] private ProgressBar _healthBar;
	[Export] public float MaxHealth { get; set; } = 5f;

	public float Health { get; private set; }

	private float _invulnerability = 0.2f;
	private float _invulnerabilityTimer;

	private void UpdateHealthBar()
	{
		_healthBar.MaxValue = MaxHealth;
		_healthBar.Value = Health;
	}

	public void TakeDamage(float amount)
	{
		if (_invulnerabilityTimer > 0)
			return;

		_invulnerabilityTimer = _invulnerability;

		Health = Math.Max(0, Health - amount);
		UpdateHealthBar();

		if (Health == 0)
			Die();
	}


	public void Heal(float amount)
	{
		Health = Math.Min(MaxHealth, Health + amount);
		UpdateHealthBar();
	}

	private void Die()
	{
		GD.Print("Die ");
	}

	private float _possessionDamage = 1;

	private void PossessionDamage()
	{
		if (IsInstanceValid(_possessionEnemy))
		{
			_possessionEnemy.TakeDamage(_possessionDamage);
			Heal(_possessionDamage);
		}
		else
		{
			TakeDamage(_possessionDamage);
		}
	}

	#endregion

	#region Possession

	[Export] private ProgressBar _cooldownPossessionBar;
	private readonly List<Enemy> _nearbyEnemies = new();

	private Enemy _possessionEnemy;
	private Enemy _nearestEnemy;


	private float _possessionDamageTime = 3f;
	private float _possessionDamageTimer;
	
	private float _possessionCooldownTime = 3f;
	private float _possessionCooldownTimer;
	
	[Export] private float _possessionRange = 100f;
	private Enemy GetNearestEnemy()
	{
		Vector2 origin = IsInstanceValid(_possessionEnemy)
			? _possessionEnemy.GlobalPosition
			: GlobalPosition;

		float range = IsInstanceValid(_possessionEnemy)
			? _possessionEnemy.Type.AttackRange
			: _possessionRange;

		return GetTree().GetNodesInGroup("enemies")
			.OfType<Enemy>()
			.Where(enemy => IsInstanceValid(enemy))
			.Where(enemy => !enemy.IsQueuedForDeletion())
			.Where(enemy => !enemy.IsPossessed)
			.Where(enemy => origin.DistanceSquaredTo(enemy.GlobalPosition) <= range * range)
			.OrderBy(enemy => origin.DistanceSquaredTo(enemy.GlobalPosition))
			.FirstOrDefault();
	}
	
	private void UpdateNearestEnemy()
	{
		Enemy nearest = GetNearestEnemy();

		if (_nearestEnemy == nearest)
			return;

		if (IsInstanceValid(_nearestEnemy))
		{
			_nearestEnemy.SetOutlineAttack(false);
			_nearestEnemy.SetOutline(false);
		}

		_nearestEnemy = nearest;

		if (!IsInstanceValid(_nearestEnemy))
			return;

		if (!IsInstanceValid(_possessionEnemy))
		{
			_nearestEnemy.SetOutline(true);
		}
		else
		{
			_nearestEnemy.SetOutlineAttack(true);
		}
	}

	private void PossessionHandler(double delta)
	{
		_possessionDamageTimer -= (float)delta;
		if (_possessionDamageTimer <= 0)
		{
			_possessionDamageTimer = _possessionDamageTime;
			PossessionDamage();
		}
		
		
		_possessionCooldownTimer = Math.Max(0, _possessionCooldownTimer - (float)delta);
		_cooldownPossessionBar.Value = _possessionCooldownTimer;
		
		if (_possessionEnemy != null && !IsInstanceValid(_possessionEnemy))
		{
			ExitPossession();
		}

		if (Input.IsActionJustPressed("possession"))
		{
			if (_possessionEnemy == null)
			{
				if (IsInstanceValid(_nearestEnemy) && _possessionCooldownTimer == 0)
				{
					EnterPossession();
				}
			}
			else
			{
				ExitPossession();
			}
		}


		UpdateNearestEnemy();
	}

	private void EnterPossession()
	{
		_possessionCooldownTimer = _possessionCooldownTime;
		_possessionEnemy = _nearestEnemy;
		_possessionEnemy.SetPossessed(true);
		_sprite.Visible = false;
		_nearestEnemy = null;
		GlobalPosition = _possessionEnemy.GlobalPosition;
		_possessionDamageTimer = _possessionDamageTime;

		_possessionEnemy.CollisionLayer = CollisionLayer;
		_possessionEnemy.CollisionMask = CollisionMask;

		
		CollisionLayer = 0;
		CollisionMask = 0;
		
		_particlePossession.Restart();
	}

	private void ExitPossession()
	{
		CollisionLayer = OriginalCollisionLayer;
		CollisionMask = OriginalCollisionMask;

		if (IsInstanceValid(_possessionEnemy))
		{
			_possessionEnemy.CollisionLayer = _possessionEnemy.OriginalCollisionLayer;
			_possessionEnemy.CollisionMask = _possessionEnemy.OriginalCollisionMask;
			_possessionEnemy.SetPossessed(false);
			_possessionEnemy.SetOutline(false);
		}

		_possessionEnemy = null;

		_possessionDamageTimer = _possessionDamageTime;
		_sprite.Visible = true;
		_invulnerabilityTimer = _invulnerability;

		if (IsInstanceValid(_nearestEnemy))
		{
			_nearestEnemy.SetOutlineAttack(false);
			_nearestEnemy.SetOutline(false);
		}

		_nearestEnemy = null;
		
		_particlePossession.Restart();
	}


	private void OnPossessionAreaBodyEntered(Node2D body)
	{
		if (body is Enemy enemy)
			_nearbyEnemies.Add(enemy);
	}

	private void OnPossessionAreaBodyExited(Node2D body)
	{
		if (body is Enemy enemy)
			_nearbyEnemies.Remove(enemy);
	}

	#endregion

	#region Godot Lifecycle

	[Export] private Sprite2D _sprite;
	[Export] private CpuParticles2D _particlePossession;

	public override void _Ready()
	{
		Health = MaxHealth;

		OriginalCollisionLayer = CollisionLayer;
		OriginalCollisionMask = CollisionMask;

		_cooldownPossessionBar.MaxValue = _possessionCooldownTime;
		
		UpdateHealthBar();
	}

	public override void _Process(double delta)
	{
		_invulnerabilityTimer = Math.Max(
			0, _invulnerabilityTimer - (float)delta
		);


		PossessionHandler(delta);

		if (Input.IsActionJustPressed("attack"))
		{
			if (_possessionEnemy != null && IsInstanceValid(_possessionEnemy))
			{
				if (_nearestEnemy != null && IsInstanceValid(_nearestEnemy))
					_possessionEnemy.TryAttack(_nearestEnemy);
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		MoveHandler(delta);
	}

	#endregion
}
