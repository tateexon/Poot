

using System.Collections.Generic;

public class ThreadSafeList<T>
{
	private readonly List<T> _List = new List<T>();
	private readonly object _Lock = new object();

	public void SafeAdd(T value)
	{
		lock (_Lock)
		{
			_List.Add(value);
		}
	}

	public T SafeGet(int index)
	{
		lock (_Lock)
		{
			return _List[index];
		}
	}

	public void SafeRemove(int index)
	{
		lock (_Lock)
		{
			_List.RemoveAt(index);
		}
	}

	public void SafeRemove(T item)
	{
		lock (_Lock)
		{
			_List.Remove(item);
		}
	}

	public int SafeCount()
	{
		lock (_Lock)
		{
			return _List.Count;
		}
	}

	public bool SafeContains(T item)
	{
		lock (_Lock)
		{
			return _List.Contains(item);
		}
	}

	public List<T> SafeShallowClone()
	{
		lock (_Lock)
		{
			return new List<T>(_List);
		}
	}
}
