
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

    [ExportGroup("References")]
    [Export] private CpuParticles2D _particles;
    [Export] private Area2D _attackArea;
    [Export] private CollisionShape2D _rangePumpkin;

    [ExportGroup("Attack Settings")]
    [Export] private float _windupTime = 0.2f;
    [Export] private float _jumpDuration = 0.45f;
    [Export] private float _recoveryTime = 0.25f;
    [Export] private float _jumpHeight = 35f;
    [Export] private float _attackAreaDuration = 0.1f;

    private AttackState _attackState = AttackState.None;

    private float _attackElapsed;

    private Vector2 _attackStart;
    private Vector2 _attackEnd;

    public override void _Ready()
    {
        base._Ready();

        _attackArea.CollisionMask = 4u;
        _attackArea.Monitoring = false;
        _rangePumpkin.Disabled = true;
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

        Vector2 movement = direction + separation * SeparationForce / Speed;

        Velocity = movement.LimitLength(1f) * Speed;
        MoveAndSlide();
    }

    protected override void StartAttack(Node2D target)
    {
        _attackStart = GlobalPosition;
        _attackEnd = target.GlobalPosition;

        _attackElapsed = 0;
        _attackState = AttackState.Windup;

        IsAttacking = true;
        IsInvulnerable = false;
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

        IsInvulnerable = true;
    }

    private void UpdateJump()
    {
        float progress = Mathf.Clamp(
            _attackElapsed / _jumpDuration,
            0f,
            1f
        );

        GlobalPosition = _attackStart.Lerp(_attackEnd, progress);

        float height = Mathf.Sin(progress * Mathf.Pi) * _jumpHeight;
        Sprite.Position = new Vector2(0, -height);

        if (progress < 1f)
            return;

        Sprite.Position = Vector2.Zero;

        _attackArea.SetDeferred("monitoring", true);
        _rangePumpkin.SetDeferred("disabled", false);

        _particles.Restart();

        _attackElapsed = 0;
        _attackState = AttackState.Recovery;

        IsInvulnerable = false;

        AudioManager.Instance.PlaySound("PumpkinJump");
    }

    private void UpdateRecovery()
    {
        if (_attackElapsed >= _attackAreaDuration)
        {
            _attackArea.SetDeferred("monitoring", false);
            _rangePumpkin.SetDeferred("disabled", true);
        }

        if (_attackElapsed < _recoveryTime)
            return;

        FinishAttack();
    }

    private void FinishAttack()
    {
        Sprite.Position = Vector2.Zero;

        _attackArea.SetDeferred("monitoring", false);
        _rangePumpkin.SetDeferred("disabled", true);

        SetCollisionDisabled(false);

        _attackState = AttackState.None;
        IsAttacking = false;
    }

    public override void SetPossessed(
        bool possessed,
        uint playerLayer = 0,
        uint playerMask = 0)
    {
        base.SetPossessed(possessed, playerLayer, playerMask);

        _attackArea.CollisionMask = possessed ? 8u : 4u;
    }

    private void OnAttackAreaBodyEntered(Node2D body)
    {
        if (body is IHealth health)
            health.TakeDamage(Damage);
    }
}
