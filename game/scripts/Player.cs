using System;
using Godot;

namespace game.scripts;

public partial class Player : Entity
{
    public override void _PhysicsProcess(double delta)
    {
        var direction = Input.GetVector("left", "right", "up", "down");
        
        Velocity = direction * Speed;
        MoveAndSlide();
    }
}