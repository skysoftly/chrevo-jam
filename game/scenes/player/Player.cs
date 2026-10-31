using System;
using System.Collections.Generic;
using System.Linq;
using game.scenes.enemies;
using Godot;

namespace game.scenes.player;

public partial class Player : CharacterBody2D
{
	#region Movement

	private float _speed = 125f;
	private float _speedModifier = 1.25f;
	private Vector2 _knockbackVelocity;


	private Vector2 GetDirection()
	{
		return Input.GetVector("left", "right", "up", "down");
	}

	private void MoveHandler(double delta)
	{
		_knockbackVelocity = _knockbackVelocity.MoveToward(
			Vector2.Zero, 1200f * (float)delta
		);
		
		if (_possessionEnemy == null)
		{
			Velocity = GetDirection() * _speed * _speedModifier + _knockbackVelocity;
			MoveAndSlide();
		}
		else
		{
			Velocity = Vector2.Zero;

			if (IsInstanceValid(_possessionEnemy))
			{
				_possessionEnemy.Velocity = GetDirection() * _possessionEnemy.Speed * _speedModifier + _knockbackVelocity;
				
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

	[Export] public float MaxHealth { get; set; } = 5f;

	private float Health { get; set; }

	private float _invulnerability = 0.4f;
	private float _invulnerabilityTimer;

	private void UpdateHealthBar()
	{
		var healthBar = GetNode<ProgressBar>("%Health");
		healthBar.MaxValue = MaxHealth;
		healthBar.Value = Health;
	}

	public void TakeDamage(float amount, Node2D attacker)
	{
		if (attacker == _possessionEnemy)
		{
			return;
		}
		if (_invulnerabilityTimer > 0)
			return;

		_invulnerabilityTimer = _invulnerability;

		GD.Print("Damage " + amount);

		Health = Math.Max(0, Health - amount);
		UpdateHealthBar();

		if (Health == 0)
			Die();
	}

	private void Die()
	{
		GD.Print("Die ");
	}

	#endregion

	#region Possession

	private readonly List<Enemy> _nearbyEnemies = new();

	private Enemy _possessionEnemy;
	private Enemy _nearestEnemy;
	
	private bool _isPossessingAnimation;
	
	private Enemy GetNearestEnemy()
	{
		return _nearbyEnemies
			.Where(IsInstanceValid)
			.OrderBy(enemy => GlobalPosition.DistanceSquaredTo(enemy.GlobalPosition))
			.FirstOrDefault();
	}

	private void UpdateNearestEnemy()
	{
			Enemy nearest = GetNearestEnemy();

			if (_nearestEnemy == nearest)
				return;

			_nearestEnemy?.SetOutline(false);

			_nearestEnemy = nearest;

			_nearestEnemy?.SetOutline(true);
		
	}

	private void PossessionHandler()
	{
		if (Input.IsActionJustPressed("possession"))
		{
			if (_possessionEnemy == null)
			{
				if (IsInstanceValid(_nearestEnemy))
				{
					_possessionEnemy = _nearestEnemy;
					_possessionEnemy.IsPossessed = true;
					_sprite.Visible = false;
				}
			}
			else
			{
				_possessionEnemy.IsPossessed = false;
				_possessionEnemy = null;
				_sprite.Visible = true;
				_invulnerabilityTimer = _invulnerability * 2;
			}
		}
		
		if (_possessionEnemy == null)
			UpdateNearestEnemy();
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
	
	public override void _Ready()
	{
		Health = MaxHealth;
		UpdateHealthBar();
	}

	public override void _Process(double delta)
	{
		_invulnerabilityTimer = Math.Max(
			0, _invulnerabilityTimer - (float)delta
		);


		PossessionHandler();
	}

	public override void _PhysicsProcess(double delta)
	{
		MoveHandler(delta);
	}

	#endregion
}
