using System.Collections;
using System.Data;

namespace Aizen.Core.Data.Abstraction;

internal class AizenDataParameterCollection : IDataParameterCollection
{
    private readonly IDataParameterCollection _dataParameterCollection;

    public AizenDataParameterCollection(IDataParameterCollection dataParameterCollection)
    {
        this._dataParameterCollection = dataParameterCollection;
    }

    public IEnumerator GetEnumerator()
    {
        return this._dataParameterCollection.GetEnumerator();
    }

    public void CopyTo(Array array, int index)
    {
        this._dataParameterCollection.CopyTo(array, index);
    }

    public int Count => this._dataParameterCollection.Count;

    public bool IsSynchronized => this._dataParameterCollection.IsSynchronized;

    public object SyncRoot => this._dataParameterCollection.SyncRoot;

    public int Add(object value)
    {
        return this._dataParameterCollection.Add(value);
    }

    public void Clear()
    {
        this._dataParameterCollection.Clear();
    }

    public bool Contains(object value)
    {
        return this._dataParameterCollection.Contains(value);
    }

    public int IndexOf(object value)
    {
        return this._dataParameterCollection.IndexOf(value);
    }

    public void Insert(int index, object value)
    {
        this._dataParameterCollection.Insert(index, value);
    }

    public void Remove(object value)
    {
        this._dataParameterCollection.Remove(value);
    }

    public void RemoveAt(int index)
    {
        this._dataParameterCollection.RemoveAt(index);
    }

    public bool IsFixedSize => this._dataParameterCollection.IsFixedSize;

    public bool IsReadOnly => this._dataParameterCollection.IsReadOnly;

    public object this[int index]
    {
        get => this._dataParameterCollection[index];
        set => this._dataParameterCollection[index] = value;
    }

    public bool Contains(string parameterName)
    {
        return this._dataParameterCollection.Contains(parameterName);
    }

    public int IndexOf(string parameterName)
    {
        return this._dataParameterCollection.IndexOf(parameterName);
    }

    public void RemoveAt(string parameterName)
    {
        this._dataParameterCollection.RemoveAt(parameterName);
    }

    public object this[string parameterName]
    {
        get => this._dataParameterCollection[parameterName];
        set => this._dataParameterCollection[parameterName] = value;
    }
}