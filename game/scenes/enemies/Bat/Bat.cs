using Godot;

namespace game.scenes.enemies.Bat;

public partial class Bat : Enemy
{
    private enum AttackState
    {
        None,
        Windup,
        Dash,
        Recovery
    }

    [Export] private Sprite2D _sprite;

    [Export] private float _hoverHeight = 60f;
    [Export] private float _hoverAmplitude = 8f;
    [Export] private float _hoverSpeed = 3f;

    [Export] private float _windupTime = 0.25f;
    [Export] private float _dashDuration = 0.3f;
    [Export] private float _recoveryTime = 0.2f;
    [Export] private float _hitRadius = 35f;

    private float _time;

    private AttackState _attackState;

    private float _attackElapsed;

    private Vector2 _attackStart;
    private Vector2 _attackEnd;

    private Node2D _attackTarget;

    protected override void MoveEnemy(double delta)
    {
        if (!IsValidTarget(PlayerReference))
            return;

        _time += (float)delta;

        Vector2 targetPosition = PlayerReference.GlobalPosition;

        targetPosition.Y -= _hoverHeight;

        targetPosition.Y += Mathf.Sin(
            _time * _hoverSpeed
        ) * _hoverAmplitude;

        Vector2 direction = GlobalPosition.DirectionTo(
            targetPosition
        );

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
                if (_attackElapsed >= _windupTime)
                {
                    _attackElapsed = 0;
                    _attackState = AttackState.Dash;
                }

                break;

            case AttackState.Dash:
                UpdateDash();
                break;

            case AttackState.Recovery:
                if (_attackElapsed >= _recoveryTime)
                    FinishAttack();

                break;
        }
    }

    private void UpdateDash()
    {
        float progress = Mathf.Clamp(
            _attackElapsed / _dashDuration,
            0f,
            1f
        );

        GlobalPosition = _attackStart.Lerp(
            _attackEnd,
            progress
        );

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
    }

    private void FinishAttack()
    {
        SetCollisionDisabled(false);

        _attackState = AttackState.None;
        _attackTarget = null;

        IsAttacking = false;
    }
}