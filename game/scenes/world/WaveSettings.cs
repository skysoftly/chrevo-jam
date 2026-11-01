using Godot;

namespace game.scenes.world;

[GlobalClass]
public partial class WaveSettings : Resource
{
    [ExportGroup("Wave")]
    [Export] public string WaveName { get; set; } = "Wave";
    [Export] public int TotalEnemies { get; set; } = 10;
    [Export] public float SpawnInterval { get; set; } = 1f;
    [Export] public int MaxAlive { get; set; } = 5;

    [ExportGroup("Enemies")]
    [Export] public PackedScene[] EnemyScenes { get; set; } = [];

    [ExportGroup("Difficulty")]
    [Export] public Curve SpawnIntervalCurve { get; set; }
    [Export] public Curve MaxAliveCurve { get; set; }

    [ExportGroup("Boss")]
    [Export] public bool IsBossWave { get; set; }
    [Export] public PackedScene BossScene { get; set; }
    [Export] public PackedScene[] BossEnemyScenes { get; set; } = [];
    [Export] public int MinEnemies { get; set; } = 3;
}