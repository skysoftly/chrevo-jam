using Godot;
using System;

public partial class GameUi : CanvasLayer
{
	[Export] private string _pathMenu;
	
	public void ToMenu()
	{
		GetTree().Paused = false;
		Error error = GetTree().ChangeSceneToFile(_pathMenu);

		if (error != Error.Ok)
			GD.PushError($"Не удалось открыть меню: {error}, path: {_pathMenu}");
	}
	
	
	public void Reset()
	{
		GetTree().Paused = false;
		GetTree().ReloadCurrentScene();
	}
}
