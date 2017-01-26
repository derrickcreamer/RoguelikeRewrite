using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UtilityCollections { // Updated 2017-01-21
	/// <summary>
	/// A hashset with an indexer for convenience.
	/// </summary>
	public class EasyHashSet<T> : HashSet<T> {
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
		public EasyHashSet() { }
		public EasyHashSet(IEqualityComparer<T> comparer) : base(comparer) { }
		public EasyHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer = null) : base(collection, comparer) { }
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

	public class Grouping<TKey, TValue> : IGrouping<TKey, TValue> {
		public TKey Key { get; protected set; }
		private readonly IEnumerable<TValue> sequence;
		public Grouping(TKey key, IEnumerable<TValue> sequence) {
			Key = key;
			this.sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));
		}
		public IEnumerator<TValue> GetEnumerator() => sequence.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	}

	public class MultiValueDictionary<TKey, TValue> : IEnumerable<IGrouping<TKey, TValue>> {
		private Dictionary<TKey, ICollection<TValue>> d;
		private readonly Func<ICollection<TValue>> createCollection;
		public MultiValueDictionary() : this(null, null) { }
		public MultiValueDictionary(IEqualityComparer<TKey> comparer) : this(null, comparer) { }
		private MultiValueDictionary(Func<ICollection<TValue>> createCollection, IEqualityComparer<TKey> comparer = null) {
			d = new Dictionary<TKey, ICollection<TValue>>(comparer);
			if(createCollection == null) createCollection = () => new List<TValue>();
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
		public IEqualityComparer<TKey> Comparer => d.Comparer;
		//todo: xml note that empty collections can be returned?
		public IEnumerator<IGrouping<TKey, TValue>> GetEnumerator() {
			foreach(var pair in d) yield return new Grouping<TKey, TValue>(pair.Key, pair.Value);
		}
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerable<KeyValuePair<TKey, TValue>> GetAllKeyValuePairs() {
			foreach(var pair in d) {
				foreach(var v in pair.Value) {
					yield return new KeyValuePair<TKey, TValue>(pair.Key, v);
				}
			}
		}
		public ICollection<TKey> GetAllKeys() => d.Keys;
		public IEnumerable<TValue> GetAllValues() {
			foreach(var collection in d.Values) {
				foreach(var v in collection) {
					yield return v;
				}
			}
		}
		//todo: xml, note 'out' value is not null
		public bool TryGetValues(TKey key, out IEnumerable<TValue> values) {
			if(d.TryGetValue(key, out ICollection<TValue> collection)) {
				values = collection;
				return collection.Count > 0;
			}
			else {
				values = Enumerable.Empty<TValue>();
				return false;
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
				if(value == null) throw new ArgumentNullException("value"); //todo: should xml explain that null throws, so use Clear instead?
				ICollection<TValue> coll = createCollection();
				foreach(TValue v in value) {
					coll.Add(v);
				}
				d[key] = coll;
			}
		}
		//todo: possible xml note: "if you want duplicate values to be ignored, consider creating this collection with HashSet"
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
		public void Clear() => d.Clear();
		public void Clear(TKey key) => d.Remove(key);
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
		//todo: possible xml note: suggest HashSet here too, "if you're using AddUnique exclusively".
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
			return d.TryGetValue(key, out values) && values.Count > 0;
		}
	}
	public class BimapOneToOne<T1, T2> : IEnumerable<(T1 one, T2 two)> {
		protected Dictionary<T1, T2> d1;
		protected Dictionary<T2, T1> d2;
		public BimapOneToOne() : this(null, null) { }
		public BimapOneToOne(IEqualityComparer<T1> comparer1, IEqualityComparer<T2> comparer2) {
			if(comparer1 == null) comparer1 = EqualityComparer<T1>.Default;
			if(comparer2 == null) comparer2 = EqualityComparer<T2>.Default;
			d1 = new Dictionary<T1, T2>(comparer1);
			d2 = new Dictionary<T2, T1>(comparer2);
		}
		public T1 this[T2 key] {
			get => d2[key];
			set {
				if(key == null) throw new ArgumentNullException(nameof(key));
				if(d1.TryGetValue(value, out T2 result2)) { // If 'value' currently has any association...
					if(d2.Comparer.Equals(result2, key)) return; // Already linked; just return.
					else d2.Remove(result2); // 'value' has a new association, so break the previous one.
				}
				if(d2.TryGetValue(key, out T1 result1)) { // Same check, for the other half.
					d1.Remove(result1);
				}
				d1[value] = key;
				d2[key] = value;
			}
		}
		public T2 this[T1 key] {
			get => d1[key];
			set {
				if(key == null) throw new ArgumentNullException(nameof(key));
				if(d2.TryGetValue(value, out T1 result1)) {
					if(d1.Comparer.Equals(result1, key)) return;
					else d1.Remove(result1);
				}
				if(d1.TryGetValue(key, out T2 result2)) {
					d2.Remove(result2);
				}
				d2[value] = key;
				d1[key] = value;
			}
		}
		public bool TryGetValue(T2 key, out T1 value) => d2.TryGetValue(key, out value);
		public bool TryGetValue(T1 key, out T2 value) => d1.TryGetValue(key, out value);
		public bool Contains(T1 key) => d1.ContainsKey(key);
		public bool Contains(T2 key) => d2.ContainsKey(key);
		public bool Contains(T1 key1, T2 key2) {
			if(key2 == null) throw new ArgumentNullException(nameof(key2));
			if(!d1.TryGetValue(key1, out T2 result)) return false;
			return d2.Comparer.Equals(result, key2);
		}
		public void Add(T1 key1, T2 key2) {
			if(key2 == null) throw new ArgumentNullException(nameof(key2)); // Check first, to prevent inconsistent state if the
			if(d2.ContainsKey(key2)) throw new ArgumentException("Key '" + nameof(key2) + "' already present."); // 2nd Add() would throw.
			d1.Add(key1, key2);
			d2.Add(key2, key1);
		}
		public bool Remove(T1 key) {
			if(!d1.TryGetValue(key, out T2 result)) return false;
			d2.Remove(result);
			d1.Remove(key);
			return true;
		}
		public bool Remove(T2 key) {
			if(!d2.TryGetValue(key, out T1 result)) return false;
			d1.Remove(result);
			d2.Remove(key);
			return true;
		}
		public bool Remove(T1 key1, T2 key2) {
			if(!Contains(key1, key2)) return false;
			d1.Remove(key1);
			d2.Remove(key2);
			return true;
		}
		public IEnumerator<(T1 one, T2 two)> GetEnumerator() {
			foreach(var pair in d1) {
				yield return (pair.Key, pair.Value);
			}
		}
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEqualityComparer<T1> ComparerT1 => d1.Comparer;
		public IEqualityComparer<T2> ComparerT2 => d2.Comparer;
		public ICollection<T1> KeysT1 => d1.Keys;
		public ICollection<T2> KeysT2 => d2.Keys;
		public void Clear() {
			d1.Clear();
			d2.Clear();
		}
	}
	public class BimapOneToMany<T1, TMany> : IEnumerable<(T1 one, TMany two)> {
		protected MultiValueDictionary<T1, TMany> d1;
		protected Dictionary<TMany, T1> d2;
		public BimapOneToMany() : this(null, null) { }
		public BimapOneToMany(IEqualityComparer<T1> comparer1, IEqualityComparer<TMany> comparer2) {
			if(comparer1 == null) comparer1 = EqualityComparer<T1>.Default;
			if(comparer2 == null) comparer2 = EqualityComparer<TMany>.Default;
			d1 = MultiValueDictionary<T1, TMany>.Create<HashSet<TMany>>(comparer1); // Using a hashset to automatically ignore duplicates
			d2 = new Dictionary<TMany, T1>(comparer2);
		}
		public T1 this[TMany key] {
			get => d2[key];
			set {
				if(value == null) throw new ArgumentNullException("value");
				if(d2.TryGetValue(key, out T1 result1)) { // If 'obj' was already linked to a T1, remove its link with 'obj'.
					if(d1.Comparer.Equals(result1, value)) return; // (If it's the same link, just return.)
					else d1.Remove(result1, key);
				}
				d1.Add(value, key);
				d2[key] = value;
			}
		}
		public IEnumerable<TMany> this[T1 key] {
			get => d1[key];
			set {
				if(value == null) throw new ArgumentNullException("value");
				foreach(var tmany in d1[key]) {
					d2.Remove(tmany); // Break any links between 'obj' and the replaced collection.
				}
				foreach(var tmany in value) {
					d1.Remove(d2[tmany], tmany); // Break any links the new collection may already have.
				}
				d1[key] = value;
				foreach(var tmany in value) {
					d2[tmany] = key;
				}
			}
		}
		public bool TryGetValue(TMany key, out T1 value) => d2.TryGetValue(key, out value);
		public bool TryGetValues(T1 key, out IEnumerable<TMany> values) => d1.TryGetValues(key, out values);
		public bool Contains(T1 key) => d1.AnyValues(key);
		public bool Contains(TMany key) => d2.ContainsKey(key);
		public bool Contains(T1 key1, TMany key2) {
			if(key1 == null) throw new ArgumentNullException(nameof(key1));
			if(!d2.TryGetValue(key2, out T1 result)) return false;
			return d1.Comparer.Equals(result, key1);
		}
		public void Add(T1 key1, TMany key2) {
			if(key2 == null) throw new ArgumentNullException(nameof(key2)); // Check first, to prevent inconsistent state if the
			if(d2.ContainsKey(key2)) throw new ArgumentException("Key '" + nameof(key2) + "' already present."); // 2nd Add() would throw.
			d1.Add(key1, key2);
			d2.Add(key2, key1);
		}
		public bool Remove(T1 key) {
			if(!d1.AnyValues(key)) return false;
			foreach(var tmany in d1[key]) {
				d2.Remove(tmany);
			}
			d1.Clear(key);
			return true;
		}
		public bool Remove(TMany key) {
			if(!d2.TryGetValue(key, out T1 result)) return false;
			d1.Remove(result, key);
			d2.Remove(key);
			return true;
		}
		public bool Remove(T1 key1, TMany key2) {
			if(!Contains(key1, key2)) return false;
			return Remove(key2);
		}
		//todo: needs testing:
		public IEnumerator<(T1 one, TMany two)> GetEnumerator() {
			foreach(var pair in d1.GetAllKeyValuePairs()) yield return (pair.Key, pair.Value);
		}
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerable<IGrouping<T1, TMany>> GroupsByT1 => d1;
		public IEqualityComparer<T1> ComparerT1 => d1.Comparer;
		public IEqualityComparer<TMany> ComparerTMany => d2.Comparer;
		public ICollection<T1> KeysT1 => d1.GetAllKeys();
		public ICollection<TMany> KeysTMany => d2.Keys;
		public void Clear() {
			d1.Clear();
			d2.Clear();
		}
	}
	public class BimapManyToMany<T1, T2> : IEnumerable<(T1 one, T2 two)> {
		protected MultiValueDictionary<T1, T2> d1;
		protected MultiValueDictionary<T2, T1> d2;
		public BimapManyToMany() : this(null, null) { }
		public BimapManyToMany(IEqualityComparer<T1> comparer1, IEqualityComparer<T2> comparer2) {
			if(comparer1 == null) comparer1 = EqualityComparer<T1>.Default;
			if(comparer2 == null) comparer2 = EqualityComparer<T2>.Default;
			d1 = MultiValueDictionary<T1, T2>.Create<HashSet<T2>>(comparer1); // Using a hashset to automatically ignore duplicates
			d2 = MultiValueDictionary<T2, T1>.Create<HashSet<T1>>(comparer2);
		}
		public IEnumerable<T1> this[T2 key] {
			get => d2[key];
			set {
				if(value == null) throw new ArgumentNullException("value");
				foreach(var t1 in d2[key]) {
					d1.Remove(t1, key); // Break any links between 'obj' and the replaced collection.
				}
				d2[key] = value;
				foreach(var t1 in value) {
					d1.Add(t1, key);
				}
			}
		}
		public IEnumerable<T2> this[T1 key] {
			get => d1[key];
			set {
				if(value == null) throw new ArgumentNullException("value");
				foreach(var t2 in d1[key]) {
					d2.Remove(t2, key);
				}
				d1[key] = value;
				foreach(var t2 in value) {
					d2.Add(t2, key);
				}
			}
		}
		public bool TryGetValues(T2 key, out IEnumerable<T1> values) => d2.TryGetValues(key, out values);
		public bool TryGetValues(T1 key, out IEnumerable<T2> values) => d1.TryGetValues(key, out values);
		public bool Contains(T1 key) => d1.AnyValues(key);
		public bool Contains(T2 key) => d2.AnyValues(key);
		public bool Contains(T1 key1, T2 key2) {
			if(key2 == null) throw new ArgumentNullException(nameof(key2));
			return d1.Contains(key1, key2); // (This could be optimized, esp. if I knew the counts.)
		}
		public void Add(T1 key1, T2 key2) {
			if(key2 == null) throw new ArgumentNullException(nameof(key2)); // Check first, to prevent inconsistent state if the 2nd Add() would throw.
			d1.Add(key1, key2);
			d2.Add(key2, key1);
		}
		public bool Remove(T1 key) {
			if(!d1.AnyValues(key)) return false;
			foreach(var t2 in d1[key]) {
				d2.Remove(t2, key);
			}
			d1.Clear(key);
			return true;
		}
		public bool Remove(T2 key) {
			if(!d2.AnyValues(key)) return false;
			foreach(var t1 in d2[key]) {
				d1.Remove(t1, key);
			}
			d2.Clear(key);
			return true;
		}
		public bool Remove(T1 key1, T2 key2) => d1.Remove(key1, key2) | d2.Remove(key2, key1); // Note the non-short-circuiting operator!
		//todo: needs testing:
		public IEnumerator<(T1 one, T2 two)> GetEnumerator() {
			foreach(var pair in d1.GetAllKeyValuePairs()) {
				yield return (pair.Key, pair.Value);
			}
		}
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEqualityComparer<T1> ComparerT1 => d1.Comparer;
		public IEqualityComparer<T2> ComparerT2 => d2.Comparer;
		public ICollection<T1> KeysT1 => d1.GetAllKeys();
		public ICollection<T2> KeysT2 => d2.GetAllKeys();
		public IEnumerable<IGrouping<T1, T2>> GroupsByT1 => d1;
		public IEnumerable<IGrouping<T2, T1>> GroupsByT2 => d2;
		public void Clear() {
			d1.Clear();
			d2.Clear();
		}
	}
	//todo, xml note about how this is a stable sort (right?)
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

namespace UtilityCollectionsExtensions {
	using UtilityCollections;
	public static class Extensions {
		public static T1 GetT1<T1, T2>(this BimapOneToOne<T1, T2> bimap, T2 obj) => bimap[obj];
		public static T2 GetT2<T1, T2>(this BimapOneToOne<T1, T2> bimap, T1 obj) => bimap[obj];
		public static bool ContainsT1<T1, T2>(this BimapOneToOne<T1, T2> bimap, T1 obj) => bimap.Contains(obj);
		public static bool ContainsT2<T1, T2>(this BimapOneToOne<T1, T2> bimap, T2 obj) => bimap.Contains(obj);
		public static bool RemoveT1<T1, T2>(this BimapOneToOne<T1, T2> bimap, T1 obj) => bimap.Remove(obj);
		public static bool RemoveT2<T1, T2>(this BimapOneToOne<T1, T2> bimap, T2 obj) => bimap.Remove(obj);
		public static bool TryGetValueT1<T1, T2>(this BimapOneToOne<T1, T2> bimap, T1 key, out T2 value) => bimap.TryGetValue(key, out value);
		public static bool TryGetValueT2<T1, T2>(this BimapOneToOne<T1, T2> bimap, T2 key, out T1 value) => bimap.TryGetValue(key, out value);

		public static T1 GetT1<T1, TMany>(this BimapOneToMany<T1, TMany> bimap, TMany obj) => bimap[obj];
		public static IEnumerable<TMany> GetTMany<T1, TMany>(this BimapOneToMany<T1, TMany> bimap, T1 obj) => bimap[obj];
		public static bool ContainsT1<T1, TMany>(this BimapOneToMany<T1, TMany> bimap, T1 obj) => bimap.Contains(obj);
		public static bool ContainsTMany<T1, TMany>(this BimapOneToMany<T1, TMany> bimap, TMany obj) => bimap.Contains(obj);
		public static bool RemoveT1<T1, TMany>(this BimapOneToMany<T1, TMany> bimap, T1 obj) => bimap.Remove(obj);
		public static bool RemoveTMany<T1, TMany>(this BimapOneToMany<T1, TMany> bimap, TMany obj) => bimap.Remove(obj);

		public static IEnumerable<T1> GetT1<T1, T2>(this BimapManyToMany<T1, T2> bimap, T2 obj) => bimap[obj];
		public static IEnumerable<T2> GetT2<T1, T2>(this BimapManyToMany<T1, T2> bimap, T1 obj) => bimap[obj];
		public static bool ContainsT1<T1, T2>(this BimapManyToMany<T1, T2> bimap, T1 obj) => bimap.Contains(obj);
		public static bool ContainsT2<T1, T2>(this BimapManyToMany<T1, T2> bimap, T2 obj) => bimap.Contains(obj);
		public static bool RemoveT1<T1, T2>(this BimapManyToMany<T1, T2> bimap, T1 obj) => bimap.Remove(obj);
		public static bool RemoveT2<T1, T2>(this BimapManyToMany<T1, T2> bimap, T2 obj) => bimap.Remove(obj);
		public static bool TryGetValuesT1<T1, T2>(this BimapManyToMany<T1, T2> bimap, T1 key, out IEnumerable<T2> values) => bimap.TryGetValues(key, out values);
		public static bool TryGetValuesT2<T1, T2>(this BimapManyToMany<T1, T2> bimap, T2 key, out IEnumerable<T1> values) => bimap.TryGetValues(key, out values);
	}
}
