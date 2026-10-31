using System;
using Godot;
using game.scenes.enemies;

namespace game.scenes.world;

public partial class Spawner : Node2D
{
	[ExportGroup("References")]
	[Export] private Node2D _player;
	[Export] private Node2D _objects;

	[ExportGroup("Enemies")]
	[Export] private PackedScene[] _enemyScenes;

	[ExportGroup("Spawn Settings")]
	[Export] private int _maxEnemies = 50;
	[Export] private int _spawnCount = 1;

	[Export] private Vector2 _worldMin = new(-450, -300);
	[Export] private Vector2 _worldMax = new(450, 300);

	private readonly Random _random = new();

	private int _minute;
	private int _second;

	private Label _minuteLabel;
	private Label _secondLabel;

	public override void _Ready()
	{
		_minuteLabel = GetNode<Label>("%Minute");
		_secondLabel = GetNode<Label>("%Second");

		UpdateTimerLabels();

		// Сразу создаём первого врага.
		SpawnAmount(1);

		GD.Print("Spawner initialized.");
	}

	public override void _Process(double delta)
	{
	}

	private void UpdateTimerLabels()
	{
		if (GodotObject.IsInstanceValid(_minuteLabel))
			_minuteLabel.Text = _minute.ToString();

		if (GodotObject.IsInstanceValid(_secondLabel))
			_secondLabel.Text = _second.ToString("00");
	}

	private void UpdateGameTime()
	{
		_second++;

		if (_second >= 10)
		{
			_second = 0;
			_minute++;
		}

		UpdateTimerLabels();
	}

	private int GetEnemyCount()
	{
		return GetTree().GetNodesInGroup("enemies").Count;
	}

	private Vector2 GetRandomPosition()
	{
		return new Vector2(
			(float)(_random.NextDouble() * (_worldMax.X - _worldMin.X) + _worldMin.X),
			(float)(_random.NextDouble() * (_worldMax.Y - _worldMin.Y) + _worldMin.Y)
		);
	}

	private void Spawn()
	{
		if (_enemyScenes == null || _enemyScenes.Length == 0)
		{
			GD.PushWarning("Spawner: No enemy scenes assigned!");
			return;
		}

		int unlockedCount = Math.Min(
			_minute + 1,
			_enemyScenes.Length
		);

		int index = _random.Next(unlockedCount);

		PackedScene scene = _enemyScenes[index];

		if (scene == null)
			return;

		Enemy enemy = scene.Instantiate<Enemy>();

		_objects.AddChild(enemy);

		enemy.GlobalPosition = ToGlobal(GetRandomPosition());
		enemy.PlayerReference = _player;

		GD.Print($"Spawned: {enemy.Name} at {enemy.GlobalPosition}");
	}

	private void SpawnAmount(int amount)
	{
		int remaining = Math.Max(
			0,
			_maxEnemies - GetEnemyCount()
		);

		int count = Math.Min(amount, remaining);

		for (int i = 0; i < count; i++)
			Spawn();
	}

	private void OnTimerTimeout()
	{
		UpdateGameTime();

		SpawnAmount(_spawnCount);
	}
}
