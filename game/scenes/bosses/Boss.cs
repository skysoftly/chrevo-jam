
using System;
using Godot;
using game.scenes.entities;
using game.scenes.world;

namespace game.scenes.bosses;

public abstract partial class Boss : Entity
{
    public event Action<Boss> Died;

    protected Spawner Spawner { get; private set; }

    protected Node2D Player => Spawner?.Player;

    [ExportGroup("References")]
    [Export] private AnimationPlayer _introAnimationPlayer;
    [Export] private AudioStreamPlayer _music;

    [ExportGroup("Intro")]
    [Export] private string _introAnimation = "Intro";

    private bool _introFinished;

    protected override void OnEntityReady()
    {
        base.OnEntityReady();
        ProcessMode = Node.ProcessModeEnum.Always;
        Spawner.IsCutscenePlaying = true;
        AddToGroup("bosses");

        ShowHealthBar();
        
        StartIntro();
    }

    public void SetSpawner(Spawner spawner)
    {
        Spawner = spawner;
    }

    // INTRO

    private void StartIntro()
    {
        GetTree().Paused = true;

        // if (GodotObject.IsInstanceValid(_introSound))
        //     _introSound.Play();

        if (GodotObject.IsInstanceValid(_introAnimationPlayer) &&
            _introAnimationPlayer.HasAnimation(_introAnimation))
        {
            _introAnimationPlayer.AnimationFinished += OnIntroAnimationFinished;
            _introAnimationPlayer.Play(_introAnimation);
        }
        else
        {
            FinishIntro();
        }
    }

    private void OnIntroAnimationFinished(StringName animationName)
    {
        if (animationName != _introAnimation)
            return;

        FinishIntro();
    }

    private void FinishIntro()
    {
        if (_introFinished || IsDead)
            return;

        ProcessMode = Node.ProcessModeEnum.Inherit;
        _introFinished = true;
        Spawner.IsCutscenePlaying = false;
        GetTree().Paused = false;

        if (GodotObject.IsInstanceValid(_music))
            _music.Play();

        OnIntroFinished();
    }

    // Вызывается после интро. Каждый босс определяет своё поведение.
    protected virtual void OnIntroFinished()
    {
    }

    // ARENA

    protected Vector2 GetRandomArenaPosition()
    {
        Vector4 bounds = Spawner.Arena;

        Vector2 localPosition = new(
            (float)GD.RandRange(bounds.X, bounds.Z),
            (float)GD.RandRange(bounds.Y, bounds.W)
        );

        return Spawner.ToGlobal(localPosition);
    }
    

    // DEATH

    protected override async void Die()
    {
        if (IsDead)
            return;

        GetTree().Paused = false;

        RemoveFromGroup("bosses");

        SetOutline(false);

        if (GodotObject.IsInstanceValid(_introAnimationPlayer))
            _introAnimationPlayer.AnimationFinished -= OnIntroAnimationFinished;

        if (GodotObject.IsInstanceValid(_music))
            _music.Stop();

        ProcessMode = ProcessModeEnum.Always;

        Died?.Invoke(this);
        
        await ToSignal(GetTree().CreateTimer( 10.0), SceneTreeTimer.SignalName.Timeout);
        
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0f, 1.5f);

        await ToSignal(tween, Tween.SignalName.Finished);
        
        base.Die();
    }
}
