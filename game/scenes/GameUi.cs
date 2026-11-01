using Godot;
using System;

public partial class GameUi : CanvasLayer
{
	[Export] private string _pathMenu;
	[Export] private string _pathCredits;
	
	public void ToMenu()
	{
		GetTree().Paused = false;
		Error error = GetTree().ChangeSceneToFile(_pathMenu);

		if (error != Error.Ok)
			GD.PushError($"Не удалось открыть меню: {error}, path: {_pathMenu}");
	}
	
	
	public void ToCredits()
	{
		GetTree().Paused = false;
		Error error = GetTree().ChangeSceneToFile(_pathCredits);

		if (error != Error.Ok)
			GD.PushError($"Не удалось открыть меню: {error}, path: {_pathCredits}");
	}
	
	
	
	public void Reset()
	{
		GetTree().Paused = false;
		GetTree().ReloadCurrentScene();
	}
}
