using Godot;

namespace game.scripts;

public partial class Enemy : CharacterBody2D
{
    [Export] public CharacterBody2D PlayerReference;

    private float _speed = 75f;
    private Vector2 _direction;
    
    
    public override void _PhysicsProcess(double delta)
    {
        if ((PlayerReference.Position - Position).Length() > 5)
        {
            Velocity = (PlayerReference.Position - Position).Normalized() * _speed;
        }
        else
        {
            Velocity = Vector2.Zero;
        }
        MoveAndSlide();
    }
}