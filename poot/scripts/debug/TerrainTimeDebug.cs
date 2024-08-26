using Godot;
using System;

public partial class TerrainTimeDebug : Label
{
	public override void _Process(double delta)
	{
		EventManager.TerrainWorker_TimeEvent += UpdateLabel;
	}

	public override void _ExitTree()
	{
		EventManager.TerrainWorker_TimeEvent -= UpdateLabel;
	}

	private void UpdateLabel(string count)
	{
		CallDeferred(nameof(SetLabelText), count);
	}

	private void SetLabelText(string count)
	{
		Text = $"Terrain Chunk Time: {count}";
	}
}
