using Godot;
using System;

public partial class TerrainDebug : Label
{
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		EventManager.TerrainWorker_CountEvent += UpdateLabel;
	}

	public override void _ExitTree()
	{
		EventManager.TerrainWorker_CountEvent -= UpdateLabel;
	}

	private void UpdateLabel(int count)
	{
		//Text = $"Terrain Queue: {count}";
		CallDeferred(nameof(SetLabelText), count);
	}

	private void SetLabelText(int count)
	{
		Text = $"Terrain Queue: {count}";
	}
}
