using Godot;
using System;

public partial class EventManager
{
	public static Action<Vector3I> MeshReadyToAddEvent;
	public static Action<int> MeshWorker_CountEvent;
	public static Action<int> TerrainWorker_CountEvent;
}
