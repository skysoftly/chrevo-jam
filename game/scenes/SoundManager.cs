using Godot;

namespace game.scenes;

public partial class SoundManager : Node
{
	public static SoundManager Instance { get; private set; }

	[Export] private AudioStreamPlayer2D _pumpkinJumpSound;

	public override void _Ready()
	{
		Instance = this;
	}

	public void PlayPumpkinJump()
	{
		if (!_pumpkinJumpSound.Playing)
			_pumpkinJumpSound.Play();
	}
}
