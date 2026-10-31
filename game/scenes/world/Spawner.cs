
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
	[Export] private float _spawnInterval = 5f;
	[Export] private float _difficultyDuration = 600f;

	[Export] private Curve _spawnCurve;
	[Export] private Curve _maxEnemiesCurve;

	[ExportGroup("Spawn Area")]
	[Export] private Vector2 _worldMin = new(-450, -300);
	[Export] private Vector2 _worldMax = new(450, 300);

	private readonly Random _random = new();

	private float _gameTime;
	private float _spawnTimer;

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
		float dt = (float)delta;

		_gameTime += dt;
		_spawnTimer += dt;

		if (_spawnTimer < _spawnInterval)
			return;

		_spawnTimer -= _spawnInterval;

		UpdateGameTime();

		int spawnCount = GetSpawnCount();

		SpawnAmount(spawnCount);
	}

	#region Game Time

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

		if (_second >= 60)
		{
			_second = 0;
			_minute++;
		}

		UpdateTimerLabels();
	}

	#endregion

	#region Difficulty

	private float GetDifficultyProgress()
	{
		return Mathf.Clamp(
			_gameTime / _difficultyDuration,
			0f,
			1f
		);
	}

	private int GetSpawnCount()
	{
		if (_spawnCurve == null)
			return 1;

		float progress = GetDifficultyProgress();

		return Mathf.Max(
			1,
			Mathf.RoundToInt(_spawnCurve.Sample(progress))
		);
	}

	private int GetMaxEnemies()
	{
		if (_maxEnemiesCurve == null)
			return _maxEnemies;

		float progress = GetDifficultyProgress();

		float multiplier = _maxEnemiesCurve.Sample(progress);

		return Mathf.Clamp(
			Mathf.RoundToInt(multiplier * _maxEnemies),
			1,
			_maxEnemies
		);
	}

	private int GetUnlockedEnemyCount()
	{
		if (_enemyScenes == null || _enemyScenes.Length == 0)
			return 0;

		float progress = GetDifficultyProgress();

		return Mathf.Clamp(
			Mathf.FloorToInt(progress * _enemyScenes.Length) + 1,
			1,
			_enemyScenes.Length
		);
	}

	#endregion

	#region Spawning

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

		int unlockedCount = GetUnlockedEnemyCount();

		if (unlockedCount <= 0)
			return;

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
			GetMaxEnemies() - GetEnemyCount()
		);

		int count = Math.Min(amount, remaining);

		for (int i = 0; i < count; i++)
			Spawn();
	}

	#endregion
}
