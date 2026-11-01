using System;
using Godot;
using game.scenes.entities;

namespace game.scenes.enemies;

public abstract partial class Enemy : Entity
{
	[ExportGroup("References")]
	[Export] protected AnimationPlayer AnimationPlayer { get; set; }

	[Export] public Node2D PlayerReference { get; set; }

	[ExportGroup("Movement")]
	[Export] public float Speed { get; set; } = 100f;
	[Export] public float StopDistance { get; set; } = 5f;

	[ExportGroup("Damage")]
	[Export] public float AttackRange { get; set; } = 50f;
	[Export] public float AttackCooldown { get; set; } = 2f;
	[Export] public float Damage { get; set; } = 1f;
	[Export] public float DamageCooldown { get; set; } = 0.2f;

	[ExportGroup("Separation")]
	[Export] public float SeparationRadius { get; set; } = 35f;
	[Export] public float SeparationForce { get; set; } = 180f;

	[ExportGroup("Death")]
	[Export] private float _deathFadeDelay = 10f;
	[Export] private float _deathFadeDuration = 1.5f;

	private float _damageTimer;

	public event Action<Enemy> Died;

	public bool IsInvulnerable { get; protected set; }
	public bool IsPossessed { get; private set; }
	public bool IsAttacking { get; protected set; }

	public float AttackTimer { get; protected set; }

	protected override void OnEntityReady()
	{
		base.OnEntityReady();

		AddToGroup("enemies");

		if (GodotObject.IsInstanceValid(AnimationPlayer))
		{
			AnimationPlayer.Play("Idle");

			double length = AnimationPlayer.GetAnimation("Idle").Length;
			AnimationPlayer.Seek(GD.Randf() * length, true);
		}
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		if (IsDead)
			return;

		float dt = (float)delta;

		_damageTimer = Mathf.Max(0f, _damageTimer - dt);

		AttackTimer = Mathf.Min(
			AttackCooldown,
			AttackTimer + dt
		);

		if (IsPossessed || IsAttacking)
			return;

		if (!IsValidTarget(PlayerReference))
			return;

		float distance = GlobalPosition.DistanceTo(
			PlayerReference.GlobalPosition
		);

		if (distance <= AttackRange && AttackTimer >= AttackCooldown)
			TryAttack(PlayerReference);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (IsDead)
			return;

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

			if (other.IsDead || other.IsPossessed || other.IsQueuedForDeletion())
				continue;

			Vector2 offset = GlobalPosition - other.GlobalPosition;
			float distance = offset.Length();

			if (distance <= 0f || distance >= SeparationRadius)
				continue;

			float strength = Mathf.Pow(
				1f - distance / SeparationRadius,
				2f
			);

			separation += offset.Normalized() * strength;
		}

		return separation;
	}

	public override void TakeDamage(float amount)
	{
		if (IsDead || IsInvulnerable || _damageTimer > 0f)
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
		if (IsDead || IsAttacking || AttackTimer < AttackCooldown)
			return false;

		if (!IsValidTarget(target))
			return false;

		AttackTimer = 0f;

		StartAttack(target);

		return true;
	}

	public void MovePossessed(Vector2 direction, float speedMultiplier = 1f)
	{
		if (IsDead || !IsPossessed || IsAttacking)
			return;

		if (direction.X > 0f)
			Sprite.FlipH = false;
		else if (direction.X < 0f)
			Sprite.FlipH = true;

		Velocity = direction * Speed * speedMultiplier;

		MoveAndSlide();
	}

	public override void SetPossessed(
		bool possessed,
		uint playerLayer = 0,
		uint playerMask = 0)
	{
		if (IsDead && possessed)
			return;

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

	// --------------------------------------------------
	// DEATH
	// --------------------------------------------------

	protected override void Die()
	{
		if (IsDead)
			return;

		IsDead = true;
		HideHealthBar();

		RemoveFromGroup("enemies");
		RemoveFromGroup("bosses");

		Velocity = Vector2.Zero;

		IsAttacking = false;
		IsInvulnerable = true;
		IsPossessed = false;

		CollisionLayer = 0;
		CollisionMask = 0;

		SetProcess(false);
		SetPhysicsProcess(false);

		DisableChildCollisions(this);

		ResetShaderEffects();

		Died?.Invoke(this);

		if (GodotObject.IsInstanceValid(AnimationPlayer) &&
			AnimationPlayer.HasAnimation("Die"))
		{
			AnimationPlayer.Play("Die");
		}

		DeathSequence();
	}

	private void DisableChildCollisions(Node node)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is Area2D area)
			{
				area.SetDeferred("monitoring", false);
				area.SetDeferred("monitorable", false);
			}

			if (child is CollisionShape2D shape)
				shape.SetDeferred("disabled", true);

			DisableChildCollisions(child);
		}
	}

	private async void DeathSequence()
	{
		if (GodotObject.IsInstanceValid(AnimationPlayer) &&
			AnimationPlayer.HasAnimation("Die"))
		{
			await ToSignal(
				AnimationPlayer,
				AnimationPlayer.SignalName.AnimationFinished
			);
		}

		if (!GodotObject.IsInstanceValid(this))
			return;

		if (!GodotObject.IsInstanceValid(Sprite))
		{
			QueueFree();
			return;
		}

		await ToSignal(
			GetTree().CreateTimer(_deathFadeDelay),
			SceneTreeTimer.SignalName.Timeout
		);

		Tween tween = CreateTween();

		tween.SetTrans(Tween.TransitionType.Linear);

		tween.TweenProperty(
			this,
			"modulate:a",
			0f,
			_deathFadeDuration
		);

		await ToSignal(tween, Tween.SignalName.Finished);

		if (GodotObject.IsInstanceValid(this))
			QueueFree();
	}
}
