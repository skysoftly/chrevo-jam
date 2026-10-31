using System;
using game.scenes.player;
using Godot;

namespace game.scenes.enemies;

public partial class Enemy : CharacterBody2D, IHealth
{
    [Export] public CharacterBody2D PlayerReference;
    [Export] private Sprite2D _sprite;

    public float Speed;
    private float _damage;

    private float _knockbackForce;

    private Vector2 _direction;
    private IHealth _target;

    public bool IsPossessed { get; set; }


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
            0, _invulnerabilityTimer - (float)delta
        );
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsPossessed)
        {
            Vector2 direction = PlayerReference.GlobalPosition - GlobalPosition;

            if (direction.Length() > 10)
                direction = direction.Normalized();
            else
                direction = Vector2.Zero;

            Velocity = direction * Speed;
            MoveAndSlide();


            if (_target != null)
            {
                // _target.Knockback(GlobalPosition, _knockbackForce);
                _target.TakeDamage(_damage);
            }
        }
    }


    private void OnAttackAreaBodyEntered(Node2D body)
    {
        if (body == this)
            return;
        GD.Print("Entered: ", body.Name);

        if (body is Player player)
            _target = player;
        if (body is Enemy { IsPossessed: true } enemy)
        {
            _target = enemy;
        }
    }

    private void OnAttackAreaBodyExited(Node2D body)
    {
        if (body == _target)
            _target = null;
    }

    public void SetOutline(bool enabled)
    {
        if (_sprite.Material is ShaderMaterial material)
        {
            material.SetShaderParameter("enabled", enabled);
        }
    }
    
    public void SetOutlineAttack(bool enabled)
    {
        if (_sprite.Material is ShaderMaterial material)
        {
            material.SetShaderParameter("attack", enabled);
        }
    }


    public void SetPossessed(bool possessed)
    {
        IsPossessed = possessed;
    }
    
    
    
    #region Health

    [Export] private ProgressBar _healthBar;
    [Export] public float MaxHealth { get; private set; }

    public float Health { get; private set; }

    private float _invulnerability = 0.2f;
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
        GD.Print("Die ");
    }

    #endregion
}