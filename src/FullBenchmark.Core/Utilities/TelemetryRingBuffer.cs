using System.Collections;

namespace FullBenchmark.Core.Utilities;

/// <summary>
/// Thread-safe fixed-capacity ring buffer for telemetry history.
/// When full, oldest entries are overwritten.
/// Used by ViewModels to populate charts without querying the database.
/// </summary>
public sealed class TelemetryRingBuffer<T> : IEnumerable<T>
{
    private readonly T[] _buffer;
    private int          _head;    // index of the next write position
    private int          _count;
    private readonly object _lock = new();

    public int Capacity { get; }
    public int Count    { get { lock (_lock) return _count; } }

    public TelemetryRingBuffer(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        Capacity = capacity;
        _buffer  = new T[capacity];
    }

    /// <summary>Adds an item. Overwrites the oldest item when at capacity.</summary>
    public void Add(T item)
    {
        lock (_lock)
        {
            _buffer[_head] = item;
            _head          = (_head + 1) % Capacity;
            if (_count < Capacity) _count++;
        }
    }

    /// <summary>Returns a snapshot ordered oldest → newest.</summary>
    public T[] ToArray()
    {
        lock (_lock)
        {
            var result = new T[_count];
            if (_count == 0) return result;

            // oldest item is at (_head - _count + Capacity) % Capacity
            var start = (_head - _count + Capacity) % Capacity;
            for (var i = 0; i < _count; i++)
                result[i] = _buffer[(start + i) % Capacity];

            return result;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            _head  = 0;
            _count = 0;
        }
    }

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)ToArray()).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
