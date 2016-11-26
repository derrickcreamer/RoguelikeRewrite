using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UtilityCollections { // Updated 2016-10-25
	/// <summary>
	/// A hashset with an indexer for convenience.
	/// </summary>
	public class DefaultHashSet<T> : HashSet<T> {
		/// <summary>
		/// GET: Returns true if the given element is present.
		/// SET: If 'true', add the given element. If 'false', remove the given element.
		/// </summary>
		public bool this[T t] {
			get { return Contains(t); }
			set {
				if(value) Add(t);
				else Remove(t);
			}
		}
		public DefaultHashSet() { }
		public DefaultHashSet(IEqualityComparer<T> comparer) : base(comparer) { }
		public DefaultHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer = null) : base(collection, comparer) { }
	}
	/// <summary>
	/// A dictionary that returns a default value if the given key isn't present.
	/// </summary>
	public class DefaultValueDictionary<TKey, TValue> : Dictionary<TKey, TValue> {
		new public TValue this[TKey key] {
			get {
				TValue v;
				if(TryGetValue(key, out v) || getDefaultValue == null) { // TryGetValue sets 'v' to default(TValue) if not found.
					return v;
				}
				else return getDefaultValue();
			}
			set {
				base[key] = value;
			}
		}
		private Func<TValue> getDefaultValue = null;
		/// <summary>
		/// If defined, the result of this method will be used instead of default(TValue).
		/// </summary>
		public Func<TValue> GetDefaultValue {
			get {
				if(getDefaultValue == null) return () => default(TValue);
				else return getDefaultValue;
			}
			set { getDefaultValue = value; }
		}
		public DefaultValueDictionary() { }
		public DefaultValueDictionary(IEqualityComparer<TKey> comparer) : base(comparer) { }
		public DefaultValueDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer = null)
			: base(dictionary, comparer) { }
	}

	public class MultiValueDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, IEnumerable<TValue>>> {
		private Dictionary<TKey, ICollection<TValue>> d;
		private readonly Func<ICollection<TValue>> createCollection;
		public MultiValueDictionary() {
			d = new Dictionary<TKey, ICollection<TValue>>();
			createCollection = () => new List<TValue>();
		}
		public MultiValueDictionary(IEqualityComparer<TKey> comparer) {
			d = new Dictionary<TKey, ICollection<TValue>>(comparer);
			createCollection = () => new List<TValue>();
		}
		private MultiValueDictionary(Func<ICollection<TValue>> createCollection, IEqualityComparer<TKey> comparer = null) {
			d = new Dictionary<TKey, ICollection<TValue>>(comparer);
			this.createCollection = createCollection;
		}
		/// <summary>
		/// Create a MultiValueDictionary that uses a specific type of ICollection, instead of the default List.
		/// </summary>
		/// <typeparam name="TCollection">The type of ICollection to use instead of List</typeparam>
		public static MultiValueDictionary<TKey, TValue> Create<TCollection>() where TCollection : ICollection<TValue>, new() {
			return new MultiValueDictionary<TKey, TValue>(() => new TCollection());
		}
		/// <summary>
		/// Create a MultiValueDictionary that uses a specific type of ICollection, instead of the default List.
		/// </summary>
		/// <typeparam name="TCollection">The type of ICollection to use instead of List</typeparam>
		public static MultiValueDictionary<TKey, TValue> Create<TCollection>(IEqualityComparer<TKey> comparer)
			where TCollection : ICollection<TValue>, new()
		{
			return new MultiValueDictionary<TKey, TValue>(() => new TCollection(), comparer);
		}
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		//todo: xml note that empty collections can be returned?
		public IEnumerator<KeyValuePair<TKey, IEnumerable<TValue>>> GetEnumerator() {
			foreach(var pair in d) yield return new KeyValuePair<TKey, IEnumerable<TValue>>(pair.Key, pair.Value);
		}
		public IEnumerable<KeyValuePair<TKey, TValue>> GetAllKeyValuePairs() {
			foreach(var pair in d) {
				foreach(var v in pair.Value) {
					yield return new KeyValuePair<TKey, TValue>(pair.Key, v);
				}
			}
		}
		public IEnumerable<TKey> GetAllKeys() => d.Keys;
		public IEnumerable<TValue> GetAllValues() {
			foreach(var collection in d.Values) {
				foreach(var v in collection) {
					yield return v;
				}
			}
		}
		public IEnumerable<TValue> this[TKey key] {
			get {
				ICollection<TValue> values;
				if(d.TryGetValue(key, out values)) return values;
				else return Enumerable.Empty<TValue>();
			}
			//todo: xml: This one replaces the entire contents of this key.
			set {
				if(value == null) {
					d.Remove(key);
				}
				else {
					ICollection<TValue> coll = createCollection();
					foreach(TValue v in value) {
						coll.Add(v);
					}
					d[key] = coll;
				}
			}
		}
		public void Add(TKey key, TValue value) {
			ICollection<TValue> values;
			if(!d.TryGetValue(key, out values)) {
				values = createCollection();
				d.Add(key, values);
			}
			values.Add(value);
		}
		public bool Remove(TKey key, TValue value) {
			ICollection<TValue> values;
			if(d.TryGetValue(key, out values)) return values.Remove(value);
			else return false;
		}
		public void Clear() { d.Clear(); }
		public void Clear(TKey key) { d.Remove(key); }
		public bool Contains(TKey key, TValue value) {
			ICollection<TValue> values;
			return d.TryGetValue(key, out values) && values.Contains(value);
		}
		public bool Contains(TValue value) {
			foreach(var collection in d.Values) {
				if(collection.Contains(value)) return true;
			}
			return false;
		}
		public bool AddUnique(TKey key, TValue value) {
			ICollection<TValue> values;
			if(d.TryGetValue(key, out values)) {
				if(values.Contains(value)) return false;
			}
			else {
				values = createCollection();
				d.Add(key, values);
			}
			values.Add(value);
			return true;
		}
		public bool AnyValues(TKey key) {
			ICollection<TValue> values;
			return d.TryGetValue(key, out values) && values.Any();
		}
	}
	public class PriorityQueue<T, TSortKey> : IEnumerable<T>, IReadOnlyCollection<T> {
		public PriorityQueue(Func<T, TSortKey> keySelector, bool descending = false)
			: this(keySelector, Comparer<TSortKey>.Default.Compare, descending) { }
		public PriorityQueue(Func<T, TSortKey> keySelector, Func<TSortKey, TSortKey, int> compare, bool descending = false) {
			if(compare == null) compare = Comparer<TSortKey>.Default.Compare;
			set = new SortedSet<pqElement>(new PriorityQueueComparer(descending, keySelector, compare));
			DescendingOrder = descending;
		}
		public PriorityQueue(Func<T, TSortKey> keySelector, IEnumerable<T> collection, Func<TSortKey, TSortKey, int> compare = null, bool descending = false)
			: this(keySelector, compare, descending) {
			foreach(T item in collection) Enqueue(item);
		}
		public PriorityQueue(Func<T, TSortKey> keySelector, IComparer<TSortKey> comparer, bool descending = false)
			: this(keySelector, comparer.Compare, descending) { }
		public PriorityQueue(Func<T, TSortKey> keySelector, IEnumerable<T> collection, IComparer<TSortKey> comparer, bool descending = false)
			: this(keySelector, collection, comparer.Compare, descending) { }
		private SortedSet<pqElement> set;
		private struct pqElement {
			public readonly int idx;
			public readonly T item;
			public pqElement(int idx, T item) {
				this.idx = idx;
				this.item = item;
			}
		}
		private class PriorityQueueComparer : Comparer<pqElement> {
			private readonly bool descending;
			private readonly Func<T, TSortKey> getSortKey;
			private readonly Func<TSortKey, TSortKey, int> compare;
			public PriorityQueueComparer(bool descending, Func<T, TSortKey> keySelector, Func<TSortKey, TSortKey, int> compare) {
				this.descending = descending;
				getSortKey = keySelector;
				this.compare = compare;
			}
			public override int Compare(pqElement x, pqElement y) {
				int primarySort;
				if(descending) primarySort = compare(getSortKey(y.item), getSortKey(x.item)); // Flip x & y for descending order.
				else primarySort = compare(getSortKey(x.item), getSortKey(y.item));
				if(primarySort != 0) return primarySort;
				else return Comparer<int>.Default.Compare(x.idx, y.idx); // Use insertion order as the final tiebreaker.
			}
		}
		public readonly bool DescendingOrder;
		private static int nextIdx = 0;
		public void Enqueue(T item) => set.Add(new pqElement(nextIdx++, item));
		public T Dequeue() {
			if(set.Count == 0) throw new InvalidOperationException("The PriorityQueue is empty.");
			pqElement next = set.Min;
			set.Remove(next);
			return next.item;
		}
		public int Count => set.Count;
		public void Clear() => set.Clear();
		public bool Contains(T item) => set.Any(x => x.item.Equals(item));
		public T Peek() {
			if(set.Count == 0) throw new InvalidOperationException("The PriorityQueue is empty.");
			return set.Min.item;
		}
		public bool ChangePriority(T item, Action<T> change) => ChangePriority(item, () => change(item));
		//todo: xml, be sure to note that this preserves insertion order
		public bool ChangePriority(T item, Action change) {
			pqElement? found = null;
			foreach(var element in set) { // Linear search is the best we can do, given our constraints.
				if(element.item.Equals(item)) {
					found = element;
					break;
				}
			}
			if(found == null) return false;
			set.Remove(found.Value); // Remove the element before changing - otherwise, the set can't find it.
			change();
			set.Add(found.Value); // Add it again with its new priority.
			return true;
		}
		public IEnumerator<T> GetEnumerator() {
			foreach(var x in set) yield return x.item;
		}
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
