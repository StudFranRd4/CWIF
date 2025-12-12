using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CollectionAdapter<T> : IList<T>
{
    private readonly ICollection<T> _collection;
    public CollectionAdapter(ICollection<T> collection) => _collection = collection ?? throw new ArgumentNullException(nameof(collection));

    public int Count => _collection.Count;
    public bool IsReadOnly => _collection.IsReadOnly;

    public T this[int index]
    {
        get => _collection.ElementAt(index);
        set
        {
            var list = _collection.ToList();
            list[index] = value;
            _collection.Clear();
            foreach (var item in list) _collection.Add(item);
        }
    }

    public void Add(T item) => _collection.Add(item);
    public void Clear() => _collection.Clear();
    public bool Contains(T item) => _collection.Contains(item);
    public void CopyTo(T[] array, int arrayIndex) => _collection.CopyTo(array, arrayIndex);
    public IEnumerator<T> GetEnumerator() => _collection.GetEnumerator();
    public int IndexOf(T item) => _collection.ToList().IndexOf(item);
    public void Insert(int index, T item)
    {
        var list = _collection.ToList();
        list.Insert(index, item);
        _collection.Clear();
        foreach (var i in list) _collection.Add(i);
    }
    public bool Remove(T item) => _collection.Remove(item);
    public void RemoveAt(int index)
    {
        var list = _collection.ToList();
        list.RemoveAt(index);
        _collection.Clear();
        foreach (var i in list) _collection.Add(i);
    }

    // Corrección aquí: IEnumerable.GetEnumerator() devuelve IEnumerator
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_collection).GetEnumerator();
}
