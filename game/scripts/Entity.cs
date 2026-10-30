using System;
using Godot;

namespace game.scripts;

public abstract partial class Entity : CharacterBody2D
{
    [Export] public string EntityName { get; set; } = "Entity";
    [Export] public int MaxHealth { get; set; } = 100;
    [Export] public float Speed { get; set; } = 150f;
    
    public int Health { get; private set; }

    public override void _Ready()
    {
        Health = MaxHealth;
    }

    public void TakeDamage(int damage)
    {
        Health = Math.Max(Health - damage, 0);

        if (Health == 0)
            Die();
    }


    public override void _PhysicsProcess(double delta)
    {
    }
    

    protected virtual void Die()
    {
        QueueFree();
    }
}