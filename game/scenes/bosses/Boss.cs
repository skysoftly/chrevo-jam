
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
    [Export] protected Sprite2D Sprite { get; set; }
    [Export] private AnimationPlayer _introAnimationPlayer;
    [Export] private AudioStreamPlayer2D _introSound;
    [Export] private AudioStreamPlayer _music;

    [ExportGroup("Intro")]
    [Export] private string _introAnimation = "Intro";

    [ExportGroup("Outline")]
    [Export] private ShaderMaterial _outlineMaterial;

    private ShaderMaterial _materialInstance;
    private bool _introFinished;

    protected override void OnEntityReady()
    {
        base.OnEntityReady();

        AddToGroup("bosses");

        ShowHealthBar();

        
        SetupOutline();
        StartIntro();
    }

    public void SetSpawner(Spawner spawner)
    {
        Spawner = spawner;
    }

    // INTRO

    private void StartIntro()
    {
        if (GodotObject.IsInstanceValid(_introSound))
            _introSound.Play();

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

        _introFinished = true;

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

    // OUTLINE

    private void SetupOutline()
    {
        if (_outlineMaterial == null || Sprite == null)
        {
            GD.PushWarning($"{Name}: Outline Sprite or Material is missing!");
            return;
        }

        _materialInstance = (ShaderMaterial)_outlineMaterial.Duplicate();
        Sprite.Material = _materialInstance;

        SetOutline(false);
    }

    public void SetOutline(bool enabled, bool attack = false)
    {
        if (_materialInstance == null || IsDead)
            return;

        _materialInstance.SetShaderParameter("enabled", enabled);
        _materialInstance.SetShaderParameter("attack", attack);
    }

    // DEATH

    protected override void Die()
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

        Died?.Invoke(this);
        base.Die();
    }
}
