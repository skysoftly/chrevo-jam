using System;
using game.scenes.player;
using Godot;

namespace game.scenes.enemies;

public partial class Enemy : CharacterBody2D, IHealth
{
    [Export] public CharacterBody2D PlayerReference;
    [Export] private Sprite2D _sprite;
    [Export] private ProgressBar _healthBar;
    
    [Export] private float _separationRadius = 32f;
    [Export] private float _separationStrength = 0.2f;

    public float Speed;
    private float _damage;
    private float _knockbackForce;

    public bool IsPossessed { get; set; }
    public bool IsAttacking { get; private set; }

    private EnemyType _type;

    public EnemyType Type
    {
        get => _type;
        set
        {
            _type = value;
            _sprite.Texture = _type.Texture;
            _damage = _type.Damage;
            Speed = _type.Speed;
            _knockbackForce = _type.KnockbackForce;
            MaxHealth = _type.MaxHealth;
        }
    }

    public uint OriginalCollisionLayer { get; private set; }
    public uint OriginalCollisionMask { get; private set; }
    private Vector2 GetSeparation()
    {
        Vector2 separation = Vector2.Zero;
    
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node == this || node is not Enemy enemy)
                continue;
    
            Vector2 offset = GlobalPosition - enemy.GlobalPosition;
            float distance = offset.Length();
    
            if (distance < 0.001f || distance >= _separationRadius)
                continue;
    
            float weight = 1f - distance / _separationRadius;
    
            separation += offset.Normalized() * weight;
        }
    
        return separation;
    }
    
    #region Health

    [Export] public float MaxHealth { get; private set; }
    public float Health { get; private set; }

    private float _invulnerability = 0.1f;
    private float _invulnerabilityTimer;

    private void UpdateHealthBar()
    {
        _healthBar.MaxValue = MaxHealth;
        _healthBar.Value = Health;
    }

    public void TakeDamage(float amount)
    {
        if (_invulnerabilityTimer > 0)
            return;

        _invulnerabilityTimer = _invulnerability;

        Health = Math.Max(0, Health - amount);
        UpdateHealthBar();

        if (Health == 0)
            Die();
    }

    private void Die()
    {
        QueueFree();
    }

    #endregion

    #region Movement

    private void MoveHandler()
    {
        if (IsPossessed || IsAttacking)
        {
            Velocity = Vector2.Zero;
            MoveAndSlide();
            return;
        }

        Vector2 direction = PlayerReference.GlobalPosition - GlobalPosition;

        if (direction.Length() > 5)
            direction = direction.Normalized();
        else
            direction = Vector2.Zero;
        
        Vector2 separation = GetSeparation() * _separationStrength;

        direction = (direction + separation).Normalized();
        Vector2 targetVelocity = direction * Speed;

        Velocity = Velocity.MoveToward(
            targetVelocity,
            300f * (float)GetPhysicsProcessDeltaTime()
        );
        MoveAndSlide();
    }

    #endregion

    #region Attacks

    [Export] private CollisionShape2D _pumpkinCollider;
    private float _attackTimer;
    [Export] private Area2D _attackArea;

    private void UpdateAttack(double delta)
    {
        _attackTimer -= (float)delta;
        if (IsPossessed || IsAttacking)
            return;

        if (_type == null || PlayerReference == null)
            return;

        
        TryAttack(PlayerReference);
    }

    public void TryAttack(Node2D target)
    {
        if (!IsInstanceValid(target))
            return;
        if (_attackTimer > 0)
        {
            return;
        }
        
        float distance = GlobalPosition.DistanceTo(target.GlobalPosition);
        
        if (distance > _type.AttackRange)
            return;
        
        if (IsAttacking || !IsInstanceValid(target))
            return;

        Vector2 direction = (target.GlobalPosition - GlobalPosition).Normalized();
        AttackHandler(target, direction);
    }

    private void AttackHandler(Node2D target, Vector2 direction)
    {
        _attackTimer = _type.AttackCooldown;
        switch (_type.AttackType)
        {
            case AttackType.Melee:
                MeleeAttack(target, direction);
                break;

            case AttackType.Projectile:
                ProjectileAttack(direction);
                break;

            case AttackType.Jump:
                if (target != null)
                    PumpkinJumpAttack(target);
                break;

            case AttackType.Wave:
                WaveAttack(direction);
                break;
        }
    }

    private void MeleeAttack(Node2D target, Vector2 direction)
    {
        // if (target is IHealth health)
        //     health.TakeDamage(_damage);
    }

    private void ProjectileAttack(Vector2 direction)
    {
        // TODO: Создание снаряда.
    }

    [Export] private CpuParticles2D _particlePumpkin;
    private async void PumpkinJumpAttack(Node2D target)
    {
        IsAttacking = true;

        Vector2 startPosition = GlobalPosition;
        Vector2 targetPosition = target.GlobalPosition;

        CollisionMask = 0;
        CollisionLayer = 0;
        
        float duration = 0.5f;
        float elapsed = 0f;

        await ToSignal(GetTree().CreateTimer(0.2f), SceneTreeTimer.SignalName.Timeout);
        _particlePumpkin.Restart();
        while (elapsed < duration)
        {
            if (!IsInsideTree() || !IsInstanceValid(target))
                break;

            elapsed += (float)GetPhysicsProcessDeltaTime();

            float t = Mathf.Clamp(elapsed / duration, 0f, 1f);

            GlobalPosition = startPosition.Lerp(targetPosition, t);

            float height = Mathf.Sin(t * Mathf.Pi) * 35f;
            _sprite.Position = new Vector2(0, -height);

            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        }

        if (!IsInsideTree())
            return;

        _sprite.Position = Vector2.Zero;
        
        // TODO: Урон по области при приземлении.
        
        
        _pumpkinCollider.Disabled = false;
        _particlePumpkin.Restart();
        await ToSignal(GetTree().CreateTimer(0.05f), SceneTreeTimer.SignalName.Timeout);
        _pumpkinCollider.Disabled = true;
        
        CollisionMask = OriginalCollisionMask;
        CollisionLayer = OriginalCollisionLayer;

        IsAttacking = false;
    }

    private void WaveAttack(Vector2 direction)
    {
        // TODO: Создание волны.
    }
    
    private void DealDamage(Node2D body)
    {
        if (body is IHealth health)
            health.TakeDamage(_damage);
    }

    #endregion

    #region Visuals

    public void SetOutline(bool enabled)
    {
        if (_sprite.Material is ShaderMaterial material)
            material.SetShaderParameter("enabled", enabled);
    }

    public void SetOutlineAttack(bool enabled)
    {
        if (_sprite.Material is ShaderMaterial material)
            material.SetShaderParameter("attack", enabled);
    }

    #endregion

    public void SetPossessed(bool possessed)
    {
        IsPossessed = possessed;
        
        if (possessed)
            _attackArea.CollisionMask |= 1u << 3;
        else
            _attackArea.CollisionMask &= ~(1u << 3);
    }

    #region Godot Lifecycle

    public override void _Ready()
    {
        _sprite.Material = _sprite.Material.Duplicate() as ShaderMaterial;

        AddToGroup("enemies");

        Health = MaxHealth;

        OriginalCollisionLayer = CollisionLayer;
        OriginalCollisionMask = CollisionMask;

        UpdateHealthBar();
    }

    public override void _Process(double delta)
    {
        _invulnerabilityTimer = Math.Max(
            0,
            _invulnerabilityTimer - (float)delta
        );

        UpdateAttack(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        MoveHandler();
    }

    #endregion
}