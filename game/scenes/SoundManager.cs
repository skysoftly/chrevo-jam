using Godot;

namespace game.scenes;

public partial class SoundManager : Node
{
	public static SoundManager Instance { get; private set; }

	[Export] private AudioStreamPlayer2D _pumpkinJumpSound;
	[Export] private AudioStreamPlayer2D _batAttackSound;

	public override void _Ready()
	{
		Instance = this;
	}

	public void PlayPumpkinJump()
	{
		_pumpkinJumpSound.Play();
	}


	public void PlayBatAttack()
	{
		_batAttackSound.Play();
	}
}
