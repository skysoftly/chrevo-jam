using System;
using Godot;

namespace game.scripts;

public partial class Player : CharacterBody2D
{
    private float _speed = 150f;
    
    public override void _PhysicsProcess(double delta)
    {
        var direction = Input.GetVector("left", "right", "up", "down");
        
        Velocity = direction * _speed;
        MoveAndSlide();
    }
}