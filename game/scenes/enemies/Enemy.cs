using game.scenes.player;
using Godot;

namespace game.scenes.enemies;

public partial class Enemy : CharacterBody2D
{
    [Export] public CharacterBody2D PlayerReference;
    [Export] private Sprite2D _sprite;

    public float Speed;
    private float _damage;

    private float _knockbackForce;

    private Vector2 _direction;
    private Player _target;

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
        }
    }

    public override void _Ready()
    {
        _sprite.Material = _sprite.Material.Duplicate() as ShaderMaterial;
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


            Velocity = direction.Normalized() * Speed;
            MoveAndSlide();
            
            

            if (_target != null)
            {
                _target.Knockback(GlobalPosition, _knockbackForce);
                _target.TakeDamage(_damage, this);
            }
        }
    }


    private void OnAttackAreaBodyEntered(Node2D body)
    {
        if (body is Player player)
            _target = player;
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
}