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
	[Export] private Curve _spawnCountCurve;
	[Export] private Curve _maxEnemiesCurve;

	[ExportGroup("Difficulty")]
	[Export] private float _difficultyDuration = 600f;

	[ExportGroup("Spawn Area")]
	[Export] private Vector2 _worldMin = new(-450, -300);
	[Export] private Vector2 _worldMax = new(450, 300);

	[ExportGroup("Boss")]
	[Export] private PackedScene _bossScene;
	[Export] private float _bossSpawnTime = 300f;

	private readonly Random _random = new();

	private float _gameTime;
	private float _spawnTimer;
	private float _secondTimer;

	private int _minute;
	private int _second;

	private bool _bossSpawned;

	private Label _minuteLabel;
	private Label _secondLabel;

	public override void _Ready()
	{
		_minuteLabel = GetNode<Label>("%Minute");
		_secondLabel = GetNode<Label>("%Second");

		UpdateTimerLabels();

		// Первый моб сразу при запуске.
		SpawnAmount(1);

		GD.Print("Spawner initialized.");
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		_gameTime += dt;
		_spawnTimer += dt;
		_secondTimer += dt;

		UpdateGameTimer();
		UpdateBossSpawn();

		if (_spawnTimer >= _spawnInterval)
		{
			_spawnTimer -= _spawnInterval;
			SpawnAmount(GetSpawnCount());
		}
	}

	// TIME

	private void UpdateGameTimer()
	{
		while (_secondTimer >= 1f)
		{
			_secondTimer -= 1f;
			_second++;

			if (_second >= 60)
			{
				_second = 0;
				_minute++;
			}

			UpdateTimerLabels();
		}
	}

	private void UpdateTimerLabels()
	{
		if (GodotObject.IsInstanceValid(_minuteLabel))
			_minuteLabel.Text = _minute.ToString();

		if (GodotObject.IsInstanceValid(_secondLabel))
			_secondLabel.Text = _second.ToString("00");
	}

	// DIFFICULTY

	private float GetDifficultyProgress()
	{
		if (_difficultyDuration <= 0f)
			return 1f;

		return Mathf.Clamp(
			_gameTime / _difficultyDuration,
			0f,
			1f
		);
	}

	private int GetSpawnCount()
	{
		if (_spawnCountCurve == null)
			return 1;

		return Mathf.Max(
			1,
			Mathf.RoundToInt(
				_spawnCountCurve.Sample(GetDifficultyProgress())
			)
		);
	}

	private int GetMaxEnemies()
	{
		if (_maxEnemiesCurve == null)
			return _maxEnemies;

		float multiplier = _maxEnemiesCurve.Sample(
			GetDifficultyProgress()
		);

		return Mathf.Clamp(
			Mathf.RoundToInt(_maxEnemies * multiplier),
			1,
			_maxEnemies
		);
	}

	// SPAWNING

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
			return;

		PackedScene scene = _enemyScenes[
			_random.Next(_enemyScenes.Length)
		];

		if (scene == null)
			return;

		Enemy enemy = scene.Instantiate<Enemy>();

		_objects.AddChild(enemy);

		enemy.GlobalPosition = ToGlobal(GetRandomPosition());
		enemy.PlayerReference = _player;
	}

	private void SpawnAmount(int amount)
	{
		int remaining = GetMaxEnemies() - GetEnemyCount();
		int count = Math.Min(amount, Math.Max(0, remaining));

		for (int i = 0; i < count; i++)
			Spawn();
	}

	// BOSS

	private void UpdateBossSpawn()
	{
		if (_bossSpawned || _bossScene == null)
			return;

		if (_gameTime < _bossSpawnTime)
			return;

		_bossSpawned = true;
		SpawnBoss();
	}

	private void SpawnBoss()
	{
		Enemy boss = _bossScene.Instantiate<Enemy>();

		_objects.AddChild(boss);

		boss.GlobalPosition = ToGlobal(GetRandomPosition());
		boss.PlayerReference = _player;

		GD.Print("Boss spawned!");
	}
}
