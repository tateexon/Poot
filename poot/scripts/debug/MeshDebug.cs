using Godot;
using System;

public partial class MeshDebug : Label
{
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		EventManager.MeshWorker_CountEvent += UpdateLabel;
	}

	public override void _ExitTree()
	{
		EventManager.MeshWorker_CountEvent -= UpdateLabel;
	}

	private void UpdateLabel(int count)
	{
		//Text = $"Mesh Queue: {count}";
		CallDeferred(nameof(SetLabelText), count);
	}

	private void SetLabelText(int count)
	{
		Text = $"Mesh Queue: {count}";
	}
}
