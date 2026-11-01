
using game.scenes.world;
using Godot;

namespace game.scenes.enemies.Skeleton;

public partial class Skeleton : Enemy
{
    [ExportGroup("Projectiles")]
    [Export] private PackedScene _enemyProjectile;
    [Export] private PackedScene _playerProjectile;

    [ExportGroup("Attack")]
    [Export] private string _attackAnimation = "Attack";
    [Export] private float _shootDelay = 0.5f;
    [Export] private float _attackEndDelay = 0.3f;

    [ExportGroup("Movement")]
    [Export] private float _desiredDistance = 180f;
    [Export] private float _distanceTolerance = 25f;
    [Export] private float _retreatDistance = 130f;

    private float _attackElapsed;
    private bool _hasShot;
    private Vector2 _attackDirection;

    protected override void MoveEnemy(double delta)
    {
        if (!IsValidTarget(PlayerReference))
        {
            Velocity = Vector2.Zero;
            PlayIdle();
            return;
        }

        Vector2 toPlayer = GlobalPosition.DirectionTo(
            PlayerReference.GlobalPosition
        );

        float distance = GlobalPosition.DistanceTo(
            PlayerReference.GlobalPosition
        );

        Vector2 direction = Vector2.Zero;

        if (distance > _desiredDistance + _distanceTolerance)
        {
            direction = toPlayer;
        }
        else if (distance < _retreatDistance)
        {
            direction = -toPlayer;
        }

        if (toPlayer.X != 0)
            Sprite.FlipH = toPlayer.X < 0;

        Vector2 separation = GetSeparationDirection();

        Vector2 movement = direction +
            separation * SeparationForce / Speed;

        Velocity = movement.LimitLength(1f) * Speed;

        MoveAndSlide();

        UpdateMovementAnimation();
    }

    protected override void StartAttack(Node2D target)
    {
        _attackElapsed = 0f;
        _hasShot = false;

        IsAttacking = true;
        Velocity = Vector2.Zero;

        _attackDirection = GlobalPosition.DirectionTo(
            target.GlobalPosition
        );

        if (target.GlobalPosition.X != GlobalPosition.X)
            Sprite.FlipH = target.GlobalPosition.X < GlobalPosition.X;

        if (GodotObject.IsInstanceValid(AnimationPlayer) &&
            AnimationPlayer.HasAnimation(_attackAnimation))
        {
            AnimationPlayer.Play(_attackAnimation);
        }
        else
        {
            GD.PushWarning(
                $"{Name}: Animation '{_attackAnimation}' not found!"
            );
        }
    }

    protected override void UpdateAttack(double delta)
    {
        _attackElapsed += (float)delta;

        if (!_hasShot && _attackElapsed >= _shootDelay)
        {
            Shoot();
            _hasShot = true;
        }

        if (_hasShot && _attackElapsed >= _shootDelay + _attackEndDelay)
        {
            IsAttacking = false;
            UpdateMovementAnimation();
        }
    }

    private void Shoot()
    {
        PackedScene scene = IsPossessed
            ? _playerProjectile
            : _enemyProjectile;

        if (scene == null)
            return;

        AudioManager.Instance.PlaySound("BoneShoot");
        
        // Четыре основных направления
        ShootDirection(scene, Vector2.Up);
        ShootDirection(scene, Vector2.Down);
        ShootDirection(scene, Vector2.Left);
        ShootDirection(scene, Vector2.Right);

        // Пятый снаряд летит прямо в target
        ShootDirection(scene, _attackDirection);
    }

    private void ShootDirection(PackedScene scene, Vector2 direction)
    {
        if (scene.Instantiate() is not Projectile projectile)
            return;

        GetTree().CurrentScene.AddChild(projectile);

        projectile.GlobalPosition = GlobalPosition;
        projectile.SetDirection(direction);
    }

    private void UpdateMovementAnimation()
    {
        if (Velocity.LengthSquared() > 1f)
            PlayWalk();
        else
            PlayIdle();
    }

    private void PlayIdle()
    {
        if (AnimationPlayer == null ||
            !AnimationPlayer.HasAnimation("Idle"))
            return;

        if (AnimationPlayer.CurrentAnimation != "Idle")
            AnimationPlayer.Play("Idle");
    }

    private void PlayWalk()
    {
        if (AnimationPlayer == null ||
            !AnimationPlayer.HasAnimation("Walk"))
            return;

        if (AnimationPlayer.CurrentAnimation != "Walk")
            AnimationPlayer.Play("Walk");
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (IsPossessed && !IsAttacking && !IsDead)
            UpdateMovementAnimation();
    }
}
