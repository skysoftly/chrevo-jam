using System.Collections.Generic;
using Godot;
using game.scenes.bosses;
using game.scenes.enemies;

namespace game.scenes.world;

public partial class Spawner : Node2D
{
	[ExportGroup("References")]
	[Export] private Node2D _player;
	[Export] private Node2D _objects;

	[ExportGroup("UI")]
	[Export] private Label _enemiesLeftLabel;
	[Export] private Label _waveLabel;
	[Export] private Label _minuteLabel;
	[Export] private Label _secondLabel;
	[Export] private Control _bossVictoryMenu;
	[ExportGroup("Animations")]
	[Export] private AnimationPlayer _waveAnimationPlayer;

	[ExportGroup("Waves")]
	[Export] private Godot.Collections.Array<WaveSettings> _waves = new();

	[ExportGroup("Endless")]
	[Export] private PackedScene[] _endlessEnemyScenes = [];
	[Export] private float _endlessSpawnInterval = 0.7f;
	[Export] private int _endlessMaxAlive = 50;

	[ExportGroup("Spawn Area")]
	[Export] private Vector2 _worldMin = new(-450, -300);
	[Export] private Vector2 _worldMax = new(450, 300);

	public Node2D Player => _player;
	public Vector4 Arena =>
		new(_worldMin.X, _worldMin.Y, _worldMax.X, _worldMax.Y);

	private readonly HashSet<Enemy> _waveEnemies = new();
	private readonly HashSet<Enemy> _countedEnemies = new();

	private Boss _currentBoss;

	private int _currentWaveIndex;
	private int _spawnedEnemies;
	private int _killedEnemies;

	private float _spawnTimer;
	private float _gameTime;
	private float _secondTimer;

	private int _minute;
	private int _second;

	private bool _bossSpawned;
	private bool _waveFinished;
	private bool _endlessMode;

	public bool IsCutscenePlaying { get; set; }
	
	public override void _Ready()
	{
		if (_waves.Count > 0)
			StartWave(0);
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		UpdateGameTime(dt);

		if (_endlessMode)
		{
			UpdateEndless(dt);
			return;
		}

		UpdateWave(dt);
	}

	// --------------------------------------------------
	// WAVES
	// --------------------------------------------------

	private void StartWave(int index)
	{
		if (index >= _waves.Count)
		{
			StartEndlessMode();
			return;
		}

		_currentWaveIndex = index;

		_spawnedEnemies = 0;
		_killedEnemies = 0;

		_spawnTimer = 0f;

		_bossSpawned = false;
		_waveFinished = false;

		_currentBoss = null;

		_waveEnemies.Clear();
		_countedEnemies.Clear();

		UpdateWaveUI();
		
		if (GodotObject.IsInstanceValid(_waveAnimationPlayer) &&
			_waveAnimationPlayer.HasAnimation("WaveStart"))
		{
			_waveAnimationPlayer.Play("WaveStart");
		}

		GD.Print($"Wave {index + 1} started");
	}

	private void UpdateWave(float delta)
	{
		if (_waveFinished || _currentWaveIndex >= _waves.Count)
			return;

		WaveSettings wave = _waves[_currentWaveIndex];

		_spawnTimer += delta;

		if (wave.IsBossWave)
			UpdateBossWave(wave);
		else
			UpdateNormalWave(wave);
	}

	private void UpdateNormalWave(WaveSettings wave)
	{
		if (_spawnedEnemies < wave.TotalEnemies)
		{
			if (_spawnTimer >= wave.SpawnInterval &&
				GetAliveEnemyCount(_waveEnemies) < wave.MaxAlive)
			{
				_spawnTimer = 0f;
				SpawnEnemy(wave.EnemyScenes, true);
			}
		}

		if (_spawnedEnemies >= wave.TotalEnemies &&
			GetAliveEnemyCount(_waveEnemies) == 0)
		{
			FinishWave();
		}
	}

	private void UpdateBossWave(WaveSettings wave)
	{
		// Сначала обычные враги.
		if (!_bossSpawned)
		{
			if (_spawnedEnemies < wave.TotalEnemies)
			{
				if (_spawnTimer >= wave.SpawnInterval &&
					GetAliveEnemyCount(_waveEnemies) < wave.MaxAlive)
				{
					_spawnTimer = 0f;
					SpawnEnemy(wave.EnemyScenes, true);
				}
			}

			// Когда обычные враги закончились — появляется босс.
			if (_spawnedEnemies >= wave.TotalEnemies &&
				GetAliveEnemyCount(_waveEnemies) == 0)
			{
				SpawnBoss(wave);
			}

			return;
		}

		// Во время битвы поддерживаем количество миньонов.
		if (!GodotObject.IsInstanceValid(_currentBoss))
			return;

		if (_currentBoss.IsDead)
			return;

		if (GetAliveEnemyCount(_waveEnemies) < wave.MinEnemies &&
			_spawnTimer >= wave.SpawnInterval)
		{
			_spawnTimer = 0f;
			SpawnEnemy(wave.BossEnemyScenes, false);
		}
	}

	// --------------------------------------------------
	// ENEMIES
	// --------------------------------------------------

	private void SpawnEnemy(PackedScene[] scenes, bool countForWave)
	{
		PackedScene scene = GetRandomScene(scenes);

		if (scene == null)
		{
			GD.PushWarning("Spawner: No enemy scenes assigned!");
			return;
		}

		if (scene.Instantiate() is not Enemy enemy)
		{
			GD.PushWarning("Spawner: Scene does not inherit from Enemy!");
			return;
		}

		_objects.AddChild(enemy);

		enemy.GlobalPosition = GetRandomPosition();
		enemy.PlayerReference = _player;

		enemy.Died += OnEnemyDied;

		_waveEnemies.Add(enemy);

		if (countForWave)
		{
			_spawnedEnemies++;
			_countedEnemies.Add(enemy);
		}

		UpdateWaveUI();
	}

	private void OnEnemyDied(Enemy enemy)
	{
		enemy.Died -= OnEnemyDied;

		_waveEnemies.Remove(enemy);

		if (_countedEnemies.Remove(enemy))
			_killedEnemies++;

		UpdateWaveUI();
	}

	// --------------------------------------------------
	// BOSS
	// --------------------------------------------------

	private void SpawnBoss(WaveSettings wave)
	{
		if (_bossSpawned || wave.BossScene == null)
			return;

		if (wave.BossScene.Instantiate() is not Boss boss)
		{
			GD.PushWarning("Spawner: Boss scene does not inherit from Boss!");
			return;
		}

		// Передаём спавнер до добавления в дерево.
		boss.SetSpawner(this);

		_objects.AddChild(boss);

		boss.GlobalPosition = GetWorldCenter();
		boss.Died += OnBossDied;

		_currentBoss = boss;
		_bossSpawned = true;

		_spawnTimer = 0f;

		GD.Print("Boss spawned!");
	}

	private void OnBossDied(Boss boss)
	{
		boss.Died -= OnBossDied;

		if (boss != _currentBoss)
			return;

		_currentBoss = null;

		foreach (Enemy enemy in _waveEnemies)
		{
			if (GodotObject.IsInstanceValid(enemy) && !enemy.IsDead)
				enemy.TakeDamage(enemy.MaxHealth);
		}

		AudioManager.Instance.PlaySound("Yes");
		GetTree().Paused = true;
		_bossVictoryMenu.Show();
	}

	// --------------------------------------------------
	// BOSS SPAWNING API
	// --------------------------------------------------

	public void SpawnBossMinion(PackedScene scene, Vector2 position)
	{
		if (scene == null)
			return;

		if (scene.Instantiate() is not Enemy enemy)
		{
			GD.PushWarning("Spawner: Minion scene does not inherit from Enemy!");
			return;
		}

		_objects.AddChild(enemy);

		enemy.GlobalPosition = position;
		enemy.PlayerReference = _player;

		enemy.Died += OnEnemyDied;

		_waveEnemies.Add(enemy);
	}

	public void AddBossObject(Node2D node)
	{
		if (!GodotObject.IsInstanceValid(node))
			return;

		_objects.AddChild(node);
	}

	// --------------------------------------------------
	// FINISH WAVE
	// --------------------------------------------------

	private void FinishWave()
	{
		if (_waveFinished)
			return;

		_waveFinished = true;

		GD.Print($"Wave {_currentWaveIndex + 1} finished!");

		StartWave(_currentWaveIndex + 1);
	}

	// --------------------------------------------------
	// ENDLESS
	// --------------------------------------------------

	public void StartEndlessMode()
	{
		_endlessMode = true;
		_spawnTimer = 0f;

		_bossVictoryMenu.Hide();
		GetTree().Paused = true;

		if (GodotObject.IsInstanceValid(_waveLabel))
			_waveLabel.Text = "Бесконечность";

		GD.Print("Endless mode started!");
	}

	private void UpdateEndless(float delta)
	{
		_spawnTimer += delta;

		if (_spawnTimer < _endlessSpawnInterval)
			return;

		if (GetEnemyCount() >= _endlessMaxAlive)
			return;

		_spawnTimer = 0f;

		SpawnEnemy(_endlessEnemyScenes, false);
	}

	// --------------------------------------------------
	// HELPERS
	// --------------------------------------------------

	private PackedScene GetRandomScene(PackedScene[] scenes)
	{
		if (scenes == null || scenes.Length == 0)
			return null;

		List<PackedScene> validScenes = new();

		foreach (PackedScene scene in scenes)
		{
			if (scene != null)
				validScenes.Add(scene);
		}

		if (validScenes.Count == 0)
			return null;

		return validScenes[GD.RandRange(0, validScenes.Count - 1)];
	}

	private Vector2 GetRandomPosition()
	{
		Vector2 localPosition = new(
			(float)GD.RandRange(_worldMin.X, _worldMax.X),
			(float)GD.RandRange(_worldMin.Y, _worldMax.Y)
		);

		return _objects.ToGlobal(localPosition);
	}

	private Vector2 GetWorldCenter()
	{
		return _objects.ToGlobal((_worldMin + _worldMax) / 2f);
	}

	private int GetAliveEnemyCount(HashSet<Enemy> enemies)
	{
		int count = 0;

		foreach (Enemy enemy in enemies)
		{
			if (!GodotObject.IsInstanceValid(enemy))
				continue;

			if (!enemy.IsDead && !enemy.IsQueuedForDeletion())
				count++;
		}

		return count;
	}

	private int GetEnemyCount()
	{
		int count = 0;

		foreach (Node node in GetTree().GetNodesInGroup("enemies"))
		{
			if (node is Enemy enemy &&
				GodotObject.IsInstanceValid(enemy) &&
				!enemy.IsDead &&
				!enemy.IsQueuedForDeletion())
			{
				count++;
			}
		}

		return count;
	}

	// --------------------------------------------------
	// TIME
	// --------------------------------------------------

	private void UpdateGameTime(float delta)
	{
		_gameTime += delta;
		_secondTimer += delta;

		if (_secondTimer < 1f)
			return;

		_secondTimer -= 1f;
		_second++;

		if (_second >= 60)
		{
			_second = 0;
			_minute++;
		}

		if (GodotObject.IsInstanceValid(_minuteLabel))
			_minuteLabel.Text = _minute.ToString("00");

		if (GodotObject.IsInstanceValid(_secondLabel))
			_secondLabel.Text = _second.ToString("00");
	}

	// --------------------------------------------------
	// UI
	// --------------------------------------------------

	private void UpdateWaveUI()
	{
		if (_currentWaveIndex >= _waves.Count)
			return;

		WaveSettings wave = _waves[_currentWaveIndex];

		if (GodotObject.IsInstanceValid(_waveLabel))
			_waveLabel.Text = wave.WaveName;

		if (GodotObject.IsInstanceValid(_enemiesLeftLabel))
		{
			int remaining = Mathf.Max(
				0,
				wave.TotalEnemies - _killedEnemies
			);

			_enemiesLeftLabel.Text = remaining.ToString();
		}
	}
}
