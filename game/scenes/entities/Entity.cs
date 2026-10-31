using Godot;
using game.scenes;

namespace game.scenes.entities;

public abstract partial class Entity : CharacterBody2D, IHealth
{
    [ExportGroup("Health")]
    [Export] public float MaxHealth { get; set; } = 5f;
    [Export] private ProgressBar _healthBar;
    [Export] private float _healthBarHideDelay = 2f;

    public float Health { get; protected set; }

    public bool IsDead { get; protected set; }
    
    public uint OriginalCollisionLayer { get; private set; }
    public uint OriginalCollisionMask { get; private set; }

    protected uint ActiveCollisionLayer;
    protected uint ActiveCollisionMask;

    private bool _collisionDisabled;
    private float _healthBarTimer;

    public override void _Ready()
    {
        Health = MaxHealth;

        OriginalCollisionLayer = CollisionLayer;
        OriginalCollisionMask = CollisionMask;

        ActiveCollisionLayer = CollisionLayer;
        ActiveCollisionMask = CollisionMask;

        if (GodotObject.IsInstanceValid(_healthBar))
        {
            _healthBar.MaxValue = MaxHealth;
            _healthBar.Value = Health;
            _healthBar.Visible = false;
        }

        OnEntityReady();
    }

    public override void _Process(double delta)
    {
        if (_healthBarTimer <= 0)
            return;

        _healthBarTimer -= (float)delta;

        if (_healthBarTimer <= 0 &&
            GodotObject.IsInstanceValid(_healthBar))
        {
            _healthBar.Visible = false;
        }
    }

    protected virtual void OnEntityReady()
    {
    }

    public virtual void TakeDamage(float amount)
    {
        if (amount <= 0 || Health <= 0)
            return;

        Health = Mathf.Max(0, Health - amount);

        UpdateHealthBar();

        if (Health <= 0)
            Die();
    }

    protected void UpdateHealthBar()
    {
        if (!GodotObject.IsInstanceValid(_healthBar))
            return;

        _healthBar.MaxValue = MaxHealth;
        _healthBar.Value = Health;

        _healthBar.Visible = true;
        _healthBarTimer = _healthBarHideDelay;
    }

    protected virtual void Die()
    {
        IsDead = true;
        QueueFree();
    }

    protected void SetCollisionDisabled(bool disabled)
    {
        _collisionDisabled = disabled;
        ApplyCollisionState();
    }

    private void ApplyCollisionState()
    {
        if (_collisionDisabled)
        {
            CollisionLayer = 0;
            CollisionMask = 0;
        }
        else
        {
            CollisionLayer = ActiveCollisionLayer;
            CollisionMask = ActiveCollisionMask;
        }
    }

    public virtual void SetPossessed(
        bool possessed,
        uint playerLayer = 0,
        uint playerMask = 0)
    {
        if (possessed)
        {
            ActiveCollisionLayer = playerLayer;
            ActiveCollisionMask = playerMask;
        }
        else
        {
            ActiveCollisionLayer = OriginalCollisionLayer;
            ActiveCollisionMask = OriginalCollisionMask;
        }

        ApplyCollisionState();
    }
}