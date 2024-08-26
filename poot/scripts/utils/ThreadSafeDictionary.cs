using System.Collections.Generic;

public class ThreadSafeDictionary<TKey, TValue>
{
	private readonly Dictionary<TKey, TValue> _Dictionary = new Dictionary<TKey, TValue>();
	private readonly object _Lock = new object();

	public void SafeAdd(TKey key, TValue value)
	{
		lock (_Lock)
		{
			_Dictionary.Add(key, value);
		}
	}

	public bool SafeTryAdd(TKey key, TValue value)
	{
		lock (_Lock)
		{
			if (!_Dictionary.ContainsKey(key))
			{
				_Dictionary.Add(key, value);
				return true;
			}
			return false;
		}
	}

	public TValue SafeGet(TKey key)
	{
		lock (_Lock)
		{
			return _Dictionary[key];
		}
	}

	public bool SafeRemove(TKey key)
	{
		lock (_Lock)
		{
			return _Dictionary.Remove(key);
		}
	}

	public bool SafeContainsKey(TKey key)
	{
		lock (_Lock)
		{
			return _Dictionary.ContainsKey(key);
		}
	}

	public List<TKey> SafeGetKeys()
	{
		lock (_Lock)
		{
			return new List<TKey>(_Dictionary.Keys);
		}
	}

	public List<TValue> SafeGetValues()
	{
		lock (_Lock)
		{
			return new List<TValue>(_Dictionary.Values);
		}
	}

	public void SafeClear()
	{
		lock (_Lock)
		{
			_Dictionary.Clear();
		}
	}

	public int SafeCount
	{
		get
		{
			lock (_Lock)
			{
				return _Dictionary.Count;
			}
		}
	}
}
