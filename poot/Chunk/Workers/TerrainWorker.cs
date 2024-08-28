using Godot;

public class TerrainWorker : WorkerQueue<Vector3I>
{
	public TerrainWorker(int numThreads) : base(numThreads)
	{
	}

	public override void ProcessItem(Vector3I item)
	{
		if (Chunks.ChunksData.SafeContainsKey(item)) { return; }
		ChunkData data = new ChunkData(item);
		if (!Chunks.heightMaps.SafeContainsKey(item))
		{
			Chunks.heightMaps.SafeAdd(item, data.GenerateHeightMap());
		}
		int[,] heightMap = Chunks.heightMaps.SafeGet(item);
		data.GenerateTerrain(ref heightMap);
		if (Chunks.ChunksData.SafeContainsKey(item)) { return; }
		if (Chunks.ChunksReadyForMesh.SafeContains(item)) { return; }
		if (data.Blocks == null) { return; }
		Chunks.ChunksData.SafeAdd(item, data);
		if (Chunks.ChunksReadyForMesh.SafeContains(item) ) { return; }
		Chunks.ChunksReadyForMesh.SafeAdd(item);
		//GD.Print($"Generated chunk {item}");
	}

	public override void InvokeCounterEvent(int count)
	{
		//EventManager.TerrainWorker_CountEvent?.Invoke(count);
	}
}
