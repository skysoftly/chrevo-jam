
using System.Collections.Generic;
using Godot;

namespace game.scenes.enemies.Bat;

public partial class Bat : Enemy
{
    private enum AttackState
    {
        None,
        Windup,
        Attack,
        Recovery
    }

    [ExportGroup("References")]
    [Export] private Area2D _attackArea;
    [Export] private CollisionShape2D _attackShape;

    [ExportGroup("Attack Settings")]
    [Export] private float _windupTime = 0.2f;
    [Export] private float _attackDuration = 1f;
    [Export] private float _recoveryTime = 0.1f;

    private AttackState _attackState = AttackState.None;
    private float _attackElapsed;

    private readonly HashSet<ulong> _hitTargets = new();

    public override void _Ready()
    {
        base._Ready();

        _attackArea.CollisionMask = 4u;
        _attackArea.Monitoring = false;
        _attackShape.Disabled = true;
    }

    protected override void MoveEnemy(double delta)
    {
        if (!IsValidTarget(PlayerReference))
            return;

        Vector2 direction = GlobalPosition.DirectionTo(
            PlayerReference.GlobalPosition
        );

        if (direction.X != 0)
            Sprite.FlipH = direction.X < 0;

        float distance = GlobalPosition.DistanceTo(
            PlayerReference.GlobalPosition
        );

        Vector2 separation = GetSeparationDirection();

        if (distance <= StopDistance)
            direction = Vector2.Zero;

        Vector2 movement = direction
            + separation * SeparationForce / Speed;

        Velocity = movement.LimitLength(1f) * Speed;
        MoveAndSlide();
    }

    protected override void StartAttack(Node2D target)
    {
        _attackElapsed = 0;
        _attackState = AttackState.Windup;

        IsAttacking = true;
        Velocity = Vector2.Zero;

        _hitTargets.Clear();
    }

    protected override void UpdateAttack(double delta)
    {
        _attackElapsed += (float)delta;

        switch (_attackState)
        {
            case AttackState.Windup:
                UpdateWindup();
                break;

            case AttackState.Attack:
                UpdateAttackState();
                break;

            case AttackState.Recovery:
                UpdateRecovery();
                break;
        }
    }

    private void UpdateWindup()
    {
        if (_attackElapsed < _windupTime)
            return;

        _attackElapsed = 0;
        _attackState = AttackState.Attack;

        AnimationPlayer.Play("Attack");
        PlayAttackSound();

        _attackArea.SetDeferred("monitoring", true);
        _attackShape.SetDeferred("disabled", false);
    }

    private void UpdateAttackState()
    {
        if (_attackElapsed < _attackDuration)
            return;

        DisableAttackArea();

        _attackElapsed = 0;
        _attackState = AttackState.Recovery;
    }

    private void UpdateRecovery()
    {
        if (_attackElapsed < _recoveryTime)
            return;

        FinishAttack();
    }

    private void FinishAttack()
    {
        DisableAttackArea();

        AnimationPlayer.Play("Idle");

        _attackState = AttackState.None;
        IsAttacking = false;
    }

    private void DisableAttackArea()
    {
        _attackArea.SetDeferred("monitoring", false);
        _attackShape.SetDeferred("disabled", true);
    }

    private void OnAttackAreaBodyEntered(Node2D body)
    {
        if (body is not IHealth health)
            return;

        if (!_hitTargets.Add(body.GetInstanceId()))
            return;

        health.TakeDamage(Damage);
    }

    private void PlayAttackSound()
    {
        AudioManager.Instance.PlaySound("BatAttack");
    }

    public override void SetPossessed(
        bool possessed,
        uint playerLayer = 0,
        uint playerMask = 0)
    {
        base.SetPossessed(possessed, playerLayer, playerMask);

        _attackArea.CollisionMask = possessed ? 8u : 4u;
    }
}
