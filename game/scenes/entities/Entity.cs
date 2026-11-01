
using System;
using Godot;
using game.scenes;

namespace game.scenes.entities;

public abstract partial class Entity : CharacterBody2D, IHealth
{
    [ExportGroup("Health")]
    [Export] public float MaxHealth { get; set; } = 5f;
    [Export] private ProgressBar _healthBar;

    public float Health { get; protected set; }

    public bool IsDead { get; protected set; }

    public uint OriginalCollisionLayer { get; private set; }
    public uint OriginalCollisionMask { get; private set; }

    protected uint ActiveCollisionLayer;
    protected uint ActiveCollisionMask;

    private bool _collisionDisabled;

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
        
        GD.Print($"{Name}: HP bar = {_healthBar}, Max = {_healthBar?.MaxValue}");
    }

    public override void _Process(double delta)
    {
    }

    protected virtual void OnEntityReady()
    {
    }

    public virtual void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0 || Health <= 0)
            return;

        Health = Mathf.Max(0, Health - amount);

        UpdateHealthBar();

        if (Health <= 0)
            Die();
        
        GD.Print($"{Name}: TakeDamage called, HP = {Health}/{MaxHealth}");
    }

    protected void UpdateHealthBar()
    {
        if (!GodotObject.IsInstanceValid(_healthBar) || IsDead)
            return;

        _healthBar.MaxValue = MaxHealth;
        _healthBar.Value = Health;
        _healthBar.Visible = true;
    }

    protected virtual void Die()
    {
        if (IsDead)
            return;

        IsDead = true;

        HideHealthBar();
        SetCollisionDisabled(true);
        QueueFree();
    }

    protected void HideHealthBar()
    {
        if (GodotObject.IsInstanceValid(_healthBar))
            _healthBar.Visible = false;
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
    
    protected void ShowHealthBar()
    {
        if (GodotObject.IsInstanceValid(_healthBar))
            _healthBar.Visible = true;
    }
}
