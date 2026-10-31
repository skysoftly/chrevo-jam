using Godot;

namespace game.scenes.enemies.Pumpkin;

public partial class Pumpkin : Enemy
{
    private enum AttackState
    {
        None,
        Windup,
        Jump,
        Recovery
    }

    [Export] private CpuParticles2D _particles;

    [Export] private float _windupTime = 0.2f;
    [Export] private float _jumpDuration = 0.45f;
    [Export] private float _recoveryTime = 0.25f;
    [Export] private float _jumpHeight = 35f;
    [Export] private float _hitRadius = 45f;

    private AttackState _attackState = AttackState.None;

    private float _attackElapsed;

    private Vector2 _attackStart;
    private Vector2 _attackEnd;

    private Node2D _attackTarget;

    protected override void MoveEnemy(double delta)
    {
        if (!IsValidTarget(PlayerReference))
            return;
        
        Vector2 direction = GlobalPosition.DirectionTo(
            PlayerReference.GlobalPosition
        );

        Vector2 separation = GetSeparationDirection();

        direction = (direction + separation * 1.5f).Normalized();

        Velocity = direction * Speed;

        MoveAndSlide();
    }

    protected override void StartAttack(Node2D target)
    {
        _attackTarget = target;

        _attackStart = GlobalPosition;
        _attackEnd = target.GlobalPosition;

        _attackElapsed = 0;
        _attackState = AttackState.Windup;

        IsAttacking = true;

        Velocity = Vector2.Zero;

        SetCollisionDisabled(true);
    }

    protected override void UpdateAttack(double delta)
    {
        _attackElapsed += (float)delta;

        switch (_attackState)
        {
            case AttackState.Windup:
                UpdateWindup();
                break;

            case AttackState.Jump:
                UpdateJump();
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
        _attackState = AttackState.Jump;

    }

    private void UpdateJump()
    {
        float progress = Mathf.Clamp(
            _attackElapsed / _jumpDuration,
            0f,
            1f
        );

        GlobalPosition = _attackStart.Lerp(
            _attackEnd,
            progress
        );

        float height = Mathf.Sin(progress * Mathf.Pi) * _jumpHeight;

        if (GodotObject.IsInstanceValid(Sprite))
            Sprite.Position = new Vector2(0, -height);

        if (progress < 1f)
            return;

        if (IsValidTarget(_attackTarget))
        {
            float distance = GlobalPosition.DistanceTo(
                _attackTarget.GlobalPosition
            );

            if (distance <= _hitRadius)
                DamageTarget(_attackTarget);
        }

        _attackElapsed = 0;
        _attackState = AttackState.Recovery;
        
        if (GodotObject.IsInstanceValid(_particles))
            _particles.Restart();
    }

    private void UpdateRecovery()
    {
        if (_attackElapsed < _recoveryTime)
            return;

        FinishAttack();
    }

    private void FinishAttack()
    {
        if (GodotObject.IsInstanceValid(Sprite))
            Sprite.Position = Vector2.Zero;

        SetCollisionDisabled(false);

        _attackState = AttackState.None;
        _attackTarget = null;


        IsAttacking = false;
    }

    protected override void Die()
    {
        if (GodotObject.IsInstanceValid(Sprite))
            Sprite.Position = Vector2.Zero;

        base.Die();
    }
}