using Godot;
using System.Collections.Generic;
using System.Threading;

public abstract class WorkerQueue<T>
{
	List<T> _queue = new List<T>();
	private readonly object _lock = new object();
	private bool _isPaused = false;
	private bool _stop = false;
	private List<Thread> _threads = new List<Thread>();
	private int _numThreads;

	protected WorkerQueue(int numThreads)
	{
		_numThreads = numThreads;
	}

	public void StartWorkerThreads()
	{
		for (int i = 0; i < _numThreads; i++)
		{
			var workerThread = new Thread(Worker);
			workerThread.IsBackground = true;
			_threads.Add(workerThread);
			workerThread.Start();
		}
	}

	private void Worker()
	{
		while (true)
		{
			T item = default(T);
			lock (_lock)
			{
				while ((_queue.Count == 0 || _isPaused) && !_stop)
				{
					Monitor.Wait(_lock);
				}

				if (_stop)
				{
					return;
				}

				if (_queue.Count > 0 && !_isPaused)
				{
					item = _queue[0];
					_queue.RemoveAt(0);
					InvokeCounterEvent(_queue.Count);
				}
			}

			if (item != null)
			{
				ProcessItem(item);
			}
		}
	}

	public void EnqueueItem(T item)
	{
		lock (_lock)
		{
			_queue.Add(item);
			Monitor.PulseAll(_lock); // Wake up any waiting worker thread
		}
	}

	public void AddList(List<T> list)
	{
		foreach (T item in list)
		{
			EnqueueItem(item);
		}
	}

	public virtual void UpdateList(List<T> newQueue)
	{
		lock (_lock)
		{
			_queue = newQueue;
			Monitor.PulseAll(_lock); // Wake up any waiting worker thread
		}
	}

	public void Pause()
	{
		lock (_lock)
		{
			_isPaused = true;
		}
	}

	public void Resume()
	{
		lock (_lock)
		{
			_isPaused = false;
			Monitor.PulseAll(_lock);
		}
	}

	public void Stop()
	{
		lock (_lock)
		{
			_stop = true;
			Monitor.PulseAll(_lock); // Wake up all threads to let them exit
		}
		// Wait for all worker threads to finish
		foreach (var thread in _threads)
		{
			thread.Join(); // Blocks until the thread terminates
		}
	}

	public virtual void InvokeCounterEvent(int count)
	{
	}

	public virtual void ProcessItem(T item)
	{
		GD.PrintErr("Implement Me");
	}
}
