using Godot;
using System.Collections.Generic;

namespace game;

public partial class AudioManager : Node
{
	public static AudioManager Instance { get; private set; }

	private readonly Dictionary<string, List<AudioStreamPlayer>> _sounds = new();

	public override void _Ready()
	{
		Instance = this;

		foreach (Node child in GetChildren())
		{
			if (child is not AudioStreamPlayer source)
				continue;

			string soundName = source.Name;

			if (!_sounds.TryGetValue(soundName, out var players))
			{
				players = new List<AudioStreamPlayer>();
				_sounds[soundName] = players;
			}

			players.Add(source);
		}
	}

	public void PlaySound(string name)
	{
		if (!_sounds.TryGetValue(name, out var players))
			return;

		foreach (var player in players)
		{
			if (player.Playing)
				continue;

			player.Play();
			return;
		}
	}
}
