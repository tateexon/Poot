using Godot;

public class MeshWorker : WorkerQueue<Vector3I>
{
	public PackedScene ChunkScene;

	public MeshWorker(int numThreads) : base(numThreads)
	{
	}

	public override void ProcessItem(Vector3I chunk)
	{
		if (!Chunks.ChunksData.SafeContainsKey(chunk)) { }
		ChunkData item = Chunks.ChunksData.SafeGet(chunk);
		if (item.Blocks == null) { return; }
		if (Chunks.ChunksMesh.SafeContainsKey(item.Location)) { return; }

		Chunk chunkInstance = (Chunk)ChunkScene.Instantiate();
		chunkInstance.ChunkLocation = item.Location;
		chunkInstance.Transform = new Transform3D(Basis.Identity, new Vector3(item.Location.X * ChunkData.Size, item.Location.Y * ChunkData.Size, item.Location.Z * ChunkData.Size));
		bool success = chunkInstance.GenerateChunkMesh(item, Chunks.ChunksData);
		if (!success) {
			Chunks._terrainWorkerQueue.EnqueueItem(chunk);
			//if (Chunks.ChunksReadyForMesh.SafeContains(chunk))
			//{
			//	Chunks.ChunksReadyForMesh.SafeAdd(chunk);
			//}
			return;
		}

		if (Chunks.ChunksMesh.SafeContainsKey(item.Location)) { return; }
		Chunks.ChunksMesh.SafeAdd(item.Location, chunkInstance);

		// notify the mesh is ready to be added to the tree
		Chunks.ChunksReadyToShow.SafeAdd(item.Location);
		//GD.Print("Generated Chunk Mesh");
	}

	public override void InvokeCounterEvent(int count)
	{
		//EventManager.MeshWorker_CountEvent?.Invoke(count);
	}
}
