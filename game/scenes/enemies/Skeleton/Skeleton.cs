
using game.scenes.world;
using Godot;

namespace game.scenes.enemies.Skeleton;

public partial class Skeleton : Enemy
{
    [ExportGroup("Projectiles")]
    [Export] private PackedScene _enemyProjectile;
    [Export] private PackedScene _playerProjectile;
    [Export] private float _reloadTime = 2.5f;

    [ExportGroup("Movement")]
    [Export] private float _desiredDistance = 180f;
    [Export] private float _distanceTolerance = 25f;

    private float _attackElapsed;

    protected override void MoveEnemy(double delta)
    {
        if (!IsValidTarget(PlayerReference))
            return;

        Vector2 toPlayer = GlobalPosition.DirectionTo(
            PlayerReference.GlobalPosition
        );

        float distance = GlobalPosition.DistanceTo(
            PlayerReference.GlobalPosition
        );

        Vector2 direction = Vector2.Zero;

        if (distance > _desiredDistance + _distanceTolerance)
            direction = toPlayer;
        else if (distance < _desiredDistance - _distanceTolerance)
            direction = -toPlayer;

        if (toPlayer.X != 0)
            Sprite.FlipH = toPlayer.X < 0;

        Vector2 separation = GetSeparationDirection();

        Vector2 movement = direction +
            separation * SeparationForce / Speed;

        Velocity = movement.LimitLength(1f) * Speed;

        MoveAndSlide();
    }

    protected override void StartAttack(Node2D target)
    {
        _attackElapsed = 0f;

        IsAttacking = true;
        Velocity = Vector2.Zero;

        AnimationPlayer.Play("Attack");
    }

    protected override void UpdateAttack(double delta)
    {
        _attackElapsed += (float)delta;

        if (_attackElapsed < _reloadTime)
            return;

        Shoot();

        AnimationPlayer.Play("Idle");

        IsAttacking = false;
    }

    private void Shoot()
    {
        PackedScene scene = IsPossessed
            ? _playerProjectile
            : _enemyProjectile;

        if (scene == null)
            return;

        ShootDirection(scene, Vector2.Up);
        ShootDirection(scene, Vector2.Down);
        ShootDirection(scene, Vector2.Left);
        ShootDirection(scene, Vector2.Right);
    }

    private void ShootDirection(PackedScene scene, Vector2 direction)
    {
        if (scene.Instantiate() is not Projectile projectile)
            return;

        GetTree().CurrentScene.AddChild(projectile);
        projectile.GlobalPosition = GlobalPosition;
        projectile.SetDirection(direction);
    }
}
