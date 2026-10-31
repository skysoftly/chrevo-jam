using Godot;

namespace game.scripts;

public partial class Enemy : CharacterBody2D
{
    [Export] public CharacterBody2D PlayerReference;

    private float _speed = 150f;
    private Vector2 _direction;
    
    
    public override void _PhysicsProcess(double delta)
    {
        Velocity = (PlayerReference.Position - Position) * _speed;
        MoveAndSlide();
    }
}