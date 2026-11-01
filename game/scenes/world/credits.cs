using Godot;

public partial class credits : ScrollContainer
{
	[Export] private float _scrollSpeed = 50f;
	private float _scrollPosition;

	public override void _Process(double delta)
	{
		_scrollPosition += _scrollSpeed * (float)delta;
		ScrollVertical = (int)_scrollPosition;
	}
}
