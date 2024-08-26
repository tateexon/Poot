using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

public class MeshWorker
{
	public static List<ChunkData> queue = new List<ChunkData>();
	private static object lockObject = new object();
	private static AutoResetEvent itemAddedEvent = new AutoResetEvent(false);
	private static bool isRunning = true;
	public static PackedScene ChunkScene;

	// example start
	//Thread workerThread = new Thread(Worker);
	//workerThread.Start();

	// example stop
	// StopWorker();
	// workerThread.Join();

	public static void Worker()
	{
		while (isRunning)
		{
			// Wait for an item to be added or for a reorder to complete
			itemAddedEvent.WaitOne();
			
			while (true)
			{
				ChunkData? item = null;
				//Stopwatch sw = new Stopwatch();
				//sw.Start();

				// Get the next item to process
				lock (lockObject)
				{
					if (queue.Count > 0)
					{
						item = queue[0];
						queue.RemoveAt(0);
						EventManager.MeshWorker_CountEvent?.Invoke(queue.Count);
					}
				}
				if (item == null)
				{
					break; // Queue was empty, go back to waiting for new items
				}
				ProcessItem(item.Value);
			//	sw.Stop();
			//	TimeSpan ts = sw.Elapsed;
			//	string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
			//ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
			//	GD.Print("RunTime " + elapsedTime);
			}
		}
	}

	public static void EnqueueItem(ChunkData item)
	{
		lock (lockObject)
		{
			queue.Add(item);
		}
		itemAddedEvent.Set(); // Signal the worker thread that an item is added
	}

	public static void AddList(List<ChunkData> list)
	{
		foreach (var item in list)
		{
			EnqueueItem(item);
		}
	}

	public static void UpdateList(List<ChunkData> newQueue)
	{
		lock (lockObject)
		{
			queue = newQueue;
		}

		itemAddedEvent.Set(); // Signal the worker thread to start processing the updated list
	}

	public static void StopWorker()
	{
		isRunning = false;
		itemAddedEvent.Set(); // Ensure the worker thread exits the wait state
	}

	static void ProcessItem(ChunkData item)
	{
		if (Chunks.ChunksMesh.SafeContainsKey(item.Location)) { return; }

		Chunk chunkInstance = (Chunk)ChunkScene.Instantiate();
		chunkInstance.ChunkLocation = item.Location;
		chunkInstance.Transform = new Transform3D(Basis.Identity, new Vector3(item.Location.X * ChunkData.Size, item.Location.Y * ChunkData.Size, item.Location.Z * ChunkData.Size));
		chunkInstance.IsGenerated = true;
		chunkInstance.GenerateChunkMesh(item);

		if (Chunks.ChunksMesh.SafeContainsKey(item.Location)) { return; }
		Chunks.ChunksMesh.SafeAdd(item.Location, chunkInstance);

		// notify the mesh is ready to be added to the tree
		Chunks.ChunksReadyToShow.Enqueue(item.Location);
		//GD.Print("Added item to queue");
	}
}
