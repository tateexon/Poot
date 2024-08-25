using Godot;
using System;

public partial class FPSLabel : Label
{
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		// Calculate FPS
		int fps = (int)(1.0 / delta);

		// Update the label text
		Text = $"FPS: {fps}";
	}
}
