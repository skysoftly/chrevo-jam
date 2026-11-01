using Godot;

namespace game.scenes.world;

public partial class Projectile : Area2D
{
    [Export] private float _speed = 180f;
    [Export] private float _damage = 10f;
    [Export] private float _lifetime = 5f;

    private Vector2 _direction = Vector2.Right;
    private float _lifeTimer;

    public override void _Ready()
    {
        _lifeTimer = _lifetime;
        BodyEntered += OnBodyEntered;
    }

    public void SetDirection(Vector2 direction)
    {
        _direction = direction.Normalized();

        if (_direction != Vector2.Zero)
            Rotation = _direction.Angle();
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        GlobalPosition += _direction * _speed * dt;

        _lifeTimer -= dt;

        if (_lifeTimer <= 0f)
            QueueFree();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is IHealth health)
            health.TakeDamage(_damage);

        QueueFree();
    }
}