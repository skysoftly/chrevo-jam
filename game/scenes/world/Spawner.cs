using System;
using game.scenes.enemies;
using Godot;
using Enemy = game.scenes.enemies.Enemy;

namespace game.scenes.world;


public partial class Spawner : Node2D
{
	[Export] private CharacterBody2D _player;
	[Export] private PackedScene _enemy;

	[Export] private EnemyType[] _enemyTypes;
	
	[Export] private Node2D _objects;
	
	private float _distance = 400;

	private readonly Random _random = new Random();
	
	
	private int _minute;
	private int Minute
	{
		get => _minute;
		set
		{
			_minute = value;
			GetNode<Label>("%Minute").Text = _minute.ToString();
		}
	}
	
	private int _second;
	private int Second
	{
		get => _second;
		set
		{
			_second = value;
			if (_second >= 10)
			{
				_second -= 10;
				Minute += 1;
			}
			GetNode<Label>("%Second").Text = _second.ToString().PadLeft(2, '0');;
		}
	}

	private void Spawn(Vector2 pos)
	{
		var enemyInstance = _enemy.Instantiate<Enemy>();

		enemyInstance.Type = _enemyTypes[_random.Next( Math.Min(_minute, _enemyTypes.Length - 1) +  1)];
		enemyInstance.Position = pos;
		enemyInstance.PlayerReference = _player;
		
		_objects.AddChild(enemyInstance);
	}

	private Vector2 GetRandomPosition()
	{
		Vector2 pos;

		do
		{
			pos = new Vector2(
				_random.Next(-450, 451),
				_random.Next(-300, 301)
			);
		}
		while (pos.DistanceSquaredTo(_player.Position) < _distance * _distance);

		return pos;
	}

	private void Amount(int number = 1)
	{
		for (int i = 0; i < number; i++)
		{
			Spawn(GetRandomPosition());
		}
	}

	private void On_timer_timeout()
	{
		Second += 1;
		int count = Second % 2;

		int remaining = 50 - GetTree().GetNodesInGroup("enemies").Count;
		
		Amount(Math.Min(count, remaining));
	}
}
