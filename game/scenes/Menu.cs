using Godot;

namespace game.scenes;

public partial class Menu : Node2D
{
	[Export]
	public Button PlayBtn { get; set; }
	[Export]
	public Button QuitBtn { get; set; }
	
	[Export]
	public PackedScene Scene { get; set; }
	
	public override void _Ready()
	{
		PlayBtn.Pressed += PlayBtnOnPressed;
		QuitBtn.Pressed += QuitBtnOnPressed;
	}

	private void PlayBtnOnPressed()
	{
		GetTree().ChangeSceneToPacked(Scene);
	}
	private void QuitBtnOnPressed()
	{
		GetTree().Quit();
	}


	public override void _Process(double delta)
	{
	}
}
