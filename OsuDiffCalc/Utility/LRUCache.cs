using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsuDiffCalc.Utility;

/// <summary>
/// Fixed size dictionary that will evict the least-recently-used items when <c>Count > Capacity</c>. <br/>
/// If you enumerate it, the elements are in most-recently-used order.
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
internal class LRUCache<TKey, TValue> : IDictionary<TKey, TValue> {
	private readonly record struct CacheEntry(TValue Value, int Priority);
	private readonly Dictionary<TKey, CacheEntry> _cache;
	private readonly SortedList<int, TKey> _priorityList;
	private int _capacity;

	/// <summary>
	/// Current highest priority of any item in the priority list. <br/>
	/// lower = higher priority, counts down with every get; or add;
	/// </summary>
	private int _currentHighestPriority = int.MaxValue;

	/// <summary>
	/// Create a new LRU cache
	/// </summary>
	/// <param name="capacity"> The maximum number of items that the cache will hold </param>
	/// <param name="autoDispose"> If <see langword="true"/>, will dispose IDisposable items as they are evicted / removed </param>
	public LRUCache(int capacity, bool autoDispose = true) {
		_capacity = capacity;
		_cache = new(capacity);
		_priorityList = new(capacity);
		AutoDispose = autoDispose;
	}

	/// <summary>
	/// The maximum number of recently used items to keep in the cache. <br/>
	/// -1 = infinite size cache.
	/// </summary>
	public int Capacity {
		get => _capacity;
		set {
			_capacity = value;
			EvictAsNeeded(AutoDispose);
		}
	}

	/// <summary>
	/// If <see langword="true"/>, will dispose IDisposable items as they are evicted / removed
	/// </summary>
	public bool AutoDispose { get; set; }

	public int Count => _cache.Count;

	public bool IsReadOnly => false;

	public ICollection<TKey> Keys => _priorityList.Select(pair => pair.Value).ToArray();

	public ICollection<TValue> Values => _priorityList.Select(pair => _cache[pair.Value].Value).ToArray();

	public TValue this[TKey key] {
		get {
			if (TryGetValue(key, out var value))
				return value;
			else
				throw new KeyNotFoundException($"Key '{key}' was not found");
		}
		set => Add(key, value);
	}

	/// <summary>
	/// Adds <paramref name="item"/> to the cache with the highest priority. <br/>
	/// If the <paramref name="item"/> is already in the cache, this marks it as the most recently used item.
	/// </summary>
	/// <param name="autoDispose"><inheritdoc cref="Clear(bool?)"/></param>
	public void Add(KeyValuePair<TKey, TValue> item, bool? autoDispose) {
		if (TryGetCacheEntry(item.Key, out var cacheEntry)) {
			if (!EqualityComparer<TValue>.Default.Equals(cacheEntry.Value, item.Value))
				Remove(item.Key, autoDispose);
			else
				return;
		}

		if (_priorityList.Count == 0)
			_currentHighestPriority = int.MaxValue;
		else
			--_currentHighestPriority;
		_priorityList.Add(_currentHighestPriority, item.Key);
		_cache[item.Key] = new(item.Value, _currentHighestPriority);
		EvictAsNeeded(autoDispose);
	}

	/// <inheritdoc cref="Add(KeyValuePair{TKey, TValue}, bool?)"/>
	public void Add(KeyValuePair<TKey, TValue> item) => Add(item, null);

	/// <inheritdoc cref="Add(TKey, TValue)"/>
	/// <param name="autoDispose"><inheritdoc cref="Clear(bool?)"/></param>
	public void Add(TKey key, TValue value, bool? autoDispose) => Add(new(key, value), autoDispose);

	/// <inheritdoc cref="Dictionary{TKey, TValue}.Add(TKey, TValue)"/>
	public void Add(TKey key, TValue value) => Add(key, value, null);

	/// <inheritdoc cref="Dictionary{TKey, TValue}.ContainsKey(TKey)"/>
	public bool ContainsKey(TKey key) => _cache.ContainsKey(key);

	/// <inheritdoc cref="Dictionary{TKey, TValue}.ContainsValue(TValue)"/>
	public bool ContainsValue(TValue value) => _cache.Any(pair => pair.Value.Value?.Equals(value) ?? value is null);

	public bool Contains(KeyValuePair<TKey, TValue> item) {
		return _cache.Any(pair =>
			(pair.Key?.Equals(item.Key) ?? item.Key is null)
			&& (pair.Value.Value?.Equals(item.Value) ?? item.Value is null));
	}

	public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
		int i = arrayIndex;
		foreach (var (priority, key) in _priorityList) {
			var value = _cache[key].Value;
			array[i] = new KeyValuePair<TKey, TValue>(key, value);
			++i;
		}
	}

	/// <summary>
	/// Empties the cache, and follows the behavior of AutoDispose
	/// </summary>
	public void Clear() => Clear(null);

	/// <summary>
	/// Empties the cache
	/// </summary>
	/// <param name="autoDispose">
	/// If <see langword="true"/>, will dispose any removed items. <br/>
	/// If <see langword="false"/>, will not. <br/>
	/// If <see langword="null"/>, inherits the value from AutoDispose.
	/// </param>
	/// <param name="exceptions"> Any values which should remain in the cache after clearing </param>
	public void Clear(bool? autoDispose, IList<TValue> exceptions = null) {
		bool shouldDispose = autoDispose ?? AutoDispose;
		exceptions = exceptions?.Where(x => x is not null).ToList();
		if (exceptions is null || exceptions.Count == 0) {
			if (shouldDispose) {
				foreach (var (_, (value, _)) in _cache) {
					if (value is IDisposable disposable)
						disposable.Dispose();
				}
			}
			_cache.Clear();
			_priorityList.Clear();
			_currentHighestPriority = int.MaxValue;
		}
		else {
			int n = exceptions.Count;
			var exceptionPairs = new List<(TKey, TValue)>(n);
			foreach (var (key, (value, _)) in _cache) {
				if (exceptions.Contains(value))
					exceptionPairs.Add((key, value));
				else if (shouldDispose && value is IDisposable disposable)
					disposable.Dispose();
			}
			n = Math.Min(n, exceptionPairs.Count);

			_cache.Clear();
			_priorityList.Clear();
			_currentHighestPriority = int.MaxValue;

			for (int i = 0; i < n; ++i) {
				var (key, value) = exceptionPairs[i];
				_cache[key] = new(value, _currentHighestPriority);
				_priorityList.Add(_currentHighestPriority, key);
				--_currentHighestPriority;
			}
			if (Capacity >= 0 && n > Capacity)
				EvictAsNeeded(autoDispose);
		}
	}

	public bool Remove(TKey key) => Remove(key, null);

	/// <inheritdoc cref="Remove(TKey)"/>
	/// <param name="autoDispose"><inheritdoc cref="Clear(bool?)"/></param>
	public bool Remove(TKey key, bool? autoDispose) {
		if (_cache.TryGetValue(key, out var pair)) {
			_cache.Remove(key);
			int idx = _priorityList.IndexOfKey(pair.Priority);
			if (idx != -1)
				_priorityList.RemoveAt(idx);
			if (Count == 0)
				_currentHighestPriority = int.MaxValue;

			if ((autoDispose ?? AutoDispose) && pair.Value is IDisposable disposable)
				disposable.Dispose();
			return true;
		}
		else
			return false;
	}

	/// <summary>
	/// Try to remove the <paramref name="item"/>.Key from the cache. Ignores the <paramref name="item"/>.Value property.
	/// </summary>
	/// <returns><see langword="true"/> if the item was removed, <see langword="false"/> if it was not found in the cache.</returns>
	/// <param name="autoDispose"><inheritdoc cref="Clear(bool?)"/></param>
	public bool Remove(KeyValuePair<TKey, TValue> item, bool? autoDispose) => Remove(item.Key, autoDispose);

	/// <inheritdoc cref="Remove(KeyValuePair{TKey, TValue}, bool?)"/>
	public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key, null);

	/// <summary>
	/// Gets the value associated with the given key, and updates its priority
	/// </summary>
	/// <param name="key"></param>
	/// <param name="value"></param>
	/// <returns></returns>
	public bool TryGetValue(TKey key, out TValue value) {
		if (TryGetCacheEntry(key, out var entry)) {
			value = entry.Value;
			return true;
		}
		else {
			value = default;
			return false;
		}
	}

	/// <summary>
	/// Try to get the cache entry associated with the given key and update its priority
	/// </summary>
	private bool TryGetCacheEntry(TKey key, out CacheEntry entry) {
		if (_cache.TryGetValue(key, out entry)) {
			int idx = _priorityList.IndexOfKey(entry.Priority);
			if (idx != -1)
				_priorityList.RemoveAt(idx);

			if (_priorityList.Count == 0)
				_currentHighestPriority = int.MaxValue;
			else
				--_currentHighestPriority;
			_priorityList[_currentHighestPriority] = key; // causes a binary search
			return true;
		}
		else {
			entry = default;
			return false;
		}
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
		return _priorityList.Select(entry => 
			new KeyValuePair<TKey, TValue>(entry.Value, _cache[entry.Value].Value)
		).GetEnumerator();
	}

	/// <summary>
	/// Evict as many entries as needed to satisfy the current Capacity
	/// </summary>
	/// <param name="autoDispose"><inheritdoc cref="Clear(bool?)"/></param>
	private void EvictAsNeeded(bool? autoDispose = null) {
		if (Capacity < 0)
			return;
		int last = _priorityList.Count - 1;
		int stop = Capacity - 1;
		if (last > stop) {
			for (int i = last; i > stop; --i) {
				var key = _priorityList.ElementAt(i).Value;
				_priorityList.RemoveAt(i);

				if (_cache.TryGetValue(key, out var pair)) {
					TValue value = pair.Value;
					_cache.Remove(key);
					if ((autoDispose ?? AutoDispose) && value is IDisposable disposable)
						disposable.Dispose();
				}
			}
		}
		if (Count == 0)
			_currentHighestPriority = int.MaxValue;
	}
}
