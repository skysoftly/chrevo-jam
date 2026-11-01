using System;
using Godot;
using game.scenes;

namespace game.scenes.entities;

public abstract partial class Entity : CharacterBody2D, IHealth
{
    [ExportGroup("References")]
    [Export] protected Sprite2D Sprite { get; set; }

    [ExportGroup("Health")]
    [Export] public float MaxHealth { get; set; } = 5f;
    [Export] private ProgressBar _healthBar;

    [ExportGroup("Outline")]
    [Export] private ShaderMaterial _outlineMaterial;
    [Export] private float _hitDuration = 0.15f;

    public float Health { get; protected set; }

    public bool IsDead { get; protected set; }

    public uint OriginalCollisionLayer { get; private set; }
    public uint OriginalCollisionMask { get; private set; }

    protected uint ActiveCollisionLayer;
    protected uint ActiveCollisionMask;

    private bool _collisionDisabled;

    private ShaderMaterial _materialInstance;
    private float _hitTimer;

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

        SetupOutline();

        OnEntityReady();

        GD.Print($"{Name}: HP bar = {_healthBar}, Max = {_healthBar?.MaxValue}");
    }

    public override void _Process(double delta)
    {
        if (_hitTimer <= 0f)
            return;

        _hitTimer -= (float)delta;

        if (_hitTimer <= 0f)
        {
            _hitTimer = 0f;
            SetHit(false);
        }
    }

    protected virtual void OnEntityReady()
    {
    }

    private void SetupOutline()
    {
        if (_outlineMaterial == null || !GodotObject.IsInstanceValid(Sprite))
        {
            GD.PushWarning($"{Name}: Outline Sprite or Material is missing!");
            return;
        }

        _materialInstance = (ShaderMaterial)_outlineMaterial.Duplicate();
        Sprite.Material = _materialInstance;

        SetOutline(false);
        SetHit(false);
    }

    public void SetOutline(bool enabled, bool attack = false)
    {
        if (_materialInstance == null || IsDead)
            return;

        _materialInstance.SetShaderParameter("enabled", enabled);
        _materialInstance.SetShaderParameter("attack", attack);
    }

    private void SetHit(bool enabled)
    {
        _materialInstance?.SetShaderParameter("hit", enabled);
    }

    protected void ResetShaderEffects()
    {
        _hitTimer = 0f;

        if (_materialInstance == null)
            return;

        _materialInstance.SetShaderParameter("enabled", false);
        _materialInstance.SetShaderParameter("attack", false);
        _materialInstance.SetShaderParameter("hit", false);
    }
    
    public virtual void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0 || Health <= 0)
            return;

        _hitTimer = _hitDuration;
        SetHit(true);

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

        ResetShaderEffects();

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