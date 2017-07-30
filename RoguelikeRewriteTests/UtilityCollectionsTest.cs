using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UtilityCollections;

namespace UtilityCollectionsTests {
	public class StringLengthEqualityComparer : EqualityComparer<string> {
		public override bool Equals(string x, string y) => x.Length == y.Length;
		public override int GetHashCode(string obj) => 0;
	}

	[TestFixture] public class BimapOneToOneTest {

		BimapOneToOne<string, int> bimap;

		[SetUp] public void Init() {
			bimap = new BimapOneToOne<string, int>();
		}
		[TestCase] public void Bimap11BasicAdd() {
			bimap["one"] = 1;
			Assert.AreEqual(1, bimap["one"]); // Added...
			Assert.AreEqual("one", bimap[1]); // ...on both sides.

			bimap.Add("two", 2);
			Assert.AreEqual(2, bimap["two"]); // Added...
			Assert.AreEqual("two", bimap[2]); // ...on both sides.

			Assert.Throws<KeyNotFoundException>(() => bimap["missing"].ToString()); // Throw on missing keys.
			Assert.Throws<KeyNotFoundException>(() => bimap[55].ToString());
		}
		[TestCase] public void Bimap11Remove() {
			bimap["one"] = 1;
			bimap.Remove("one");
			Assert.IsFalse(bimap.Contains("one")); // Removed...
			Assert.IsFalse(bimap.Contains(1)); // ...on both sides.

			bimap["two"] = 2;
			bimap.Remove(2);
			Assert.IsFalse(bimap.Contains("two")); // Removing either one will work.
			Assert.IsFalse(bimap.Contains(2));

			bimap["three"] = 3;
			bimap.Remove("computer, please remove threes", 3);
			Assert.AreEqual("three", bimap[3]); // "three" and 3 were unaffected - only that exact pair would be removed.
			Assert.AreEqual(3, bimap["three"]);
			bimap.Remove("three", 3);
			Assert.IsFalse(bimap.Contains("three")); // The pair was removed this time.
			Assert.IsFalse(bimap.Contains(3));
		}
		[TestCase] public void Bimap11AddExisting() {
			bimap["one"] = 1;
			bimap["one"] = 1;
			Assert.AreEqual(1, bimap.KeysT1.Count); // Still just 1 entry on each side.
			Assert.AreEqual(1, bimap.KeysT2.Count);
			Assert.Throws<ArgumentException>(() => bimap.Add("another one", 1)); // If either exists, throw.
			Assert.Throws<ArgumentException>(() => bimap.Add("one", 2));
			Assert.Throws<ArgumentException>(() => bimap.Add("one", 1));

			bimap["two"] = 2;
			bimap[2] = "another two";
			Assert.IsFalse(bimap.Contains("two")); // "two" was removed when '2' was reassigned.
			Assert.AreEqual(2, bimap["another two"]); // The new assignment was added normally.
			Assert.AreEqual("another two", bimap[2]);

			bimap["three"] = 3;
			bimap[4] = "four";
			bimap["three"] = 4;
			Assert.IsFalse(bimap.Contains("four")); // "four" and '3' were dropped.
			Assert.IsFalse(bimap.Contains(3));
			Assert.AreEqual(4, bimap["three"]); // The new assignment was added normally.
			Assert.AreEqual("three", bimap[4]);
		}
		[TestCase] public void Bimap11Null() {
			Assert.Throws<ArgumentNullException>(() => bimap.Add(null, 2)); // Null keys always throw.
			Assert.Throws<ArgumentNullException>(() => bimap[null] = 2);
			Assert.Throws<ArgumentNullException>(() => bimap[2] = null);
			Assert.Throws<ArgumentNullException>(() => bimap.Contains(null));
			Assert.Throws<ArgumentNullException>(() => bimap.Remove(null));
		}
		[TestCase] public void Bimap11Comparer() {
			BimapOneToOne<string, int> bimapCustomComparer = new BimapOneToOne<string, int>(new StringLengthEqualityComparer(), null);
			bimapCustomComparer[2] = "two";
			Assert.AreEqual(2, bimapCustomComparer["???"]); // Any string of length 3 will do.
			Assert.IsTrue(bimapCustomComparer.Contains("one"));
			bimapCustomComparer[4] = "ten";
			Assert.Throws<ArgumentException>(() => bimapCustomComparer.Add("err", 55)); // Length 3 already exists, so throw.
			Assert.IsFalse(bimapCustomComparer.Contains(2)); // '2' was removed when the 3-length string was reassigned.
			Assert.IsTrue(bimapCustomComparer.Remove("   ")); // Removal succeeds.
			Assert.AreEqual(0, bimapCustomComparer.KeysT1.Count); // Nothing left.
			Assert.AreEqual(0, bimapCustomComparer.KeysT2.Count);
		}
	}

	[TestFixture] public class BimapOneToManyTest {

		BimapOneToMany<string, int> bimap;

		[SetUp] public void Init() {
			bimap = new BimapOneToMany<string, int>();
		}
		[TestCase] public void Bimap1ManyBasicAdd() {
			bimap.Add("one", 1);
			Assert.AreEqual(1, bimap["one"].First()); // Added...
			Assert.AreEqual("one", bimap[1]); // ...on both sides.

			bimap[2] = "one";
			Assert.AreEqual(2, bimap["one"].Count()); // "one" now has 2 links.
			Assert.AreEqual("one", bimap[2]); // '2' is now linked to "one".

			Assert.Throws<KeyNotFoundException>(() => bimap[55].ToString()); // Throw on missing TMany key.
			Assert.AreEqual(0, bimap["missing"].Count()); // A missing T1 key returns an empty IEnumerable instead of throwing.
		}
		[TestCase] public void Bimap1ManyRemove() {
			bimap.Add("one", 1);
			bimap.Remove("one");
			Assert.IsFalse(bimap.Contains("one")); // Removed...
			Assert.IsFalse(bimap.Contains(1)); // ...on both sides.

			bimap.Add("two", 2);
			bimap.Remove(2);
			Assert.IsFalse(bimap.Contains("two")); // Removing either one will work.
			Assert.IsFalse(bimap.Contains(2));

			bimap.Add("three", 3);
			bimap.Remove("computer, please remove threes", 3);
			Assert.AreEqual("three", bimap[3]); // "three" and 3 were unaffected - only that exact pair would be removed.
			Assert.AreEqual(3, bimap["three"].First());
			bimap.Remove("three", 3);
			Assert.IsFalse(bimap.Contains("three")); // The pair was removed this time.
			Assert.IsFalse(bimap.Contains(3));
		}
		[TestCase] public void Bimap1ManyAddExisting() {
			bimap[1] = "one";
			bimap[1] = "one";
			Assert.AreEqual(1, bimap.KeysT1.Count); // Still just 1 entry on each side.
			Assert.AreEqual(1, bimap.KeysTMany.Count);
			Assert.Throws<ArgumentException>(() => bimap.Add("another one", 1)); // If the TMany key exists, throw.
			bimap.Add("one", 111); // The T1 key can be duplicated.

			bimap.Add("two", 2);
			bimap[2] = "another two";
			Assert.IsFalse(bimap.Contains("two")); // "two" was removed when '2' was reassigned.
			Assert.AreEqual(2, bimap["another two"].First()); // The new assignment was added normally.
			Assert.AreEqual("another two", bimap[2]);

			bimap.Add("three", 3);
			bimap[4] = "four";
			bimap["three"] = new List<int>{ 4 };
			Assert.IsFalse(bimap.Contains("four")); // "four" and '3' were dropped.
			Assert.IsFalse(bimap.Contains(3));
			Assert.AreEqual(4, bimap["three"].First()); // The new assignment was added normally.
			Assert.AreEqual("three", bimap[4]);
		}
		[TestCase] public void Bimap1ManyNull() {
			Assert.Throws<ArgumentNullException>(() => bimap.Add(null, 2));
			Assert.Throws<ArgumentNullException>(() => bimap[null] = new List<int>());
			Assert.Throws<ArgumentNullException>(() => bimap[2] = null);
			Assert.Throws<ArgumentNullException>(() => bimap["two"] = null);
			Assert.Throws<ArgumentNullException>(() => bimap.Contains(null));
			Assert.Throws<ArgumentNullException>(() => bimap.Remove(null));
		}
	}

	[TestFixture] public class BimapManyToManyTest {

		BimapManyToMany<string, int> bimap;

		[SetUp] public void Init() {
			bimap = new BimapManyToMany<string, int>();
		}
		[TestCase] public void BimapManyManyBasicAdd() {
			bimap.Add("one", 1);
			Assert.AreEqual(1, bimap["one"].First()); // Added...
			Assert.AreEqual("one", bimap[1].First()); // ...on both sides.

			bimap.Add("one", 2);
			Assert.AreEqual(2, bimap["one"].Count()); // "one" now has 2 links.
			Assert.AreEqual("one", bimap[2].First()); // '2' is now linked to "one".

			Assert.AreEqual(0, bimap[99].Count()); // A missing key returns an empty IEnumerable instead of throwing.
			Assert.AreEqual(0, bimap["missing"].Count());
		}
		[TestCase] public void BimapManyManyRemove() {
			bimap.Add("one", 1);
			bimap.Remove("one");
			Assert.IsFalse(bimap.Contains("one")); // Removed...
			Assert.IsFalse(bimap.Contains(1)); // ...on both sides.

			bimap.Add("two", 2);
			bimap.Remove(2);
			Assert.IsFalse(bimap.Contains("two")); // Removing either one will work.
			Assert.IsFalse(bimap.Contains(2));

			bimap.Add("three", 3);
			bimap.Remove("computer, please remove threes", 3);
			Assert.AreEqual("three", bimap[3].First()); // "three" and 3 were unaffected - only that exact pair would be removed.
			Assert.AreEqual(3, bimap["three"].First());
			bimap.Remove("three", 3);
			Assert.IsFalse(bimap.Contains("three")); // The pair was removed this time.
			Assert.IsFalse(bimap.Contains(3));
		}
		[TestCase] public void BimapManyManyAddExisting() {
			bimap[1] = new List<string> { "one" };
			bimap[1] = new List<string> { "one" };
			Assert.AreEqual(1, bimap.KeysT1.Count); // Still just 1 entry on each side.
			Assert.AreEqual(1, bimap.KeysT2.Count);
			bimap.Add("another one", 1); // The keys can be duplicated.
			bimap.Add("one", 111);

			bimap.Add("two", 2);
			bimap[2] = new List<string> { "another two" };
			Assert.IsFalse(bimap.Contains("two")); // "two" was removed when '2' was reassigned.
			Assert.AreEqual(2, bimap["another two"].First()); // The new assignment was added normally.
			Assert.AreEqual("another two", bimap[2].First());

			bimap.Add("three", 3);
			bimap.Add("four", 4);
			bimap["three"] = new List<int>{ 4 };
			Assert.IsFalse(bimap.Contains(3)); // '3' was dropped.
			Assert.IsTrue(bimap.Contains("four")); // "four" was not dropped.
			Assert.AreEqual(4, bimap["three"].First()); // The new assignment was added normally.
			Assert.IsTrue(bimap[4].Contains("three"));
		}
		[TestCase] public void BimapManyManyNull() {
			Assert.Throws<ArgumentNullException>(() => bimap.Add(null, 2));
			Assert.Throws<ArgumentNullException>(() => bimap[null] = new List<int>());
			Assert.Throws<ArgumentNullException>(() => bimap[2] = null);
			Assert.Throws<ArgumentNullException>(() => bimap["two"] = null);
			Assert.Throws<ArgumentNullException>(() => bimap.Contains(null));
			Assert.Throws<ArgumentNullException>(() => bimap.Remove(null));
		}
	}

	[TestFixture] public class PriorityQueueTest {
		[TestCase] public void PQBasicOperations() {
			var pq = new PriorityQueue<Exception, int>(ex => ex.Message.Length);
			Assert.AreEqual(0, pq.Count);
			pq.Enqueue(new Exception("short message"));
			Assert.AreEqual(1, pq.Count);
			pq.Enqueue(new Exception("a longer message"));
			Assert.AreEqual(2, pq.Count);
			Exception ex1 = pq.Peek();
			Assert.AreEqual(2, pq.Count); // Not removed
			Assert.AreSame(ex1, pq.Dequeue()); // Remove and verify it's the same one returned by Peek().
			Assert.AreEqual(1, pq.Count);
			pq.Clear();
			Assert.AreEqual(0, pq.Count);
		}
		[TestCase] public void PQAdvancedOperations() {
			var coll = new[]{ "4444", "1", "333", "22" };
			var pq1 = new PriorityQueue<string, int>(s => s.Length, coll, false);
			var pq2 = new PriorityQueue<string, int>(s => s.Length, coll, true);
			Assert.AreEqual("1", pq1.Peek()); // Ascending
			Assert.AreEqual("4444", pq2.Peek()); // Descending
			var pq1out = pq1.ToArray();
			Assert.IsTrue(pq1out.SequenceEqual(new[]{ "1", "22", "333", "4444" }));
			Assert.IsTrue(pq1out.SequenceEqual(pq2.ToArray().Reverse()));
		}

		private class ObjWithInt { public int i; }

		[TestCase] public void PQChangePriority() {
			var pq = new PriorityQueue<ObjWithInt, int>(x => x.i);
			var obj1 = new ObjWithInt{ i = 5 };
			var obj2 = new ObjWithInt { i = 5 };
			var obj3 = new ObjWithInt { i = 5 };
			pq.Enqueue(obj1);
			pq.Enqueue(obj2);
			pq.Enqueue(obj3);
			Assert.AreSame(obj1, pq.Peek()); // Insertion order
			pq.ChangePriority(obj1, () => obj1.i = 9);
			Assert.AreSame(obj2, pq.Peek()); // obj1 has moved to the back
			pq.ChangePriority(obj3, () => obj3.i = 9);
			Assert.AreSame(obj2, pq.Peek()); // obj3 has moved to the back, obj2 is still in front
			pq.ChangePriority(obj2, () => obj2.i = 9);
			Assert.AreSame(obj1, pq.Dequeue()); // All 3 now have the same value, and therefore insertion order is used again
			Assert.AreSame(obj2, pq.Dequeue());
			Assert.AreSame(obj3, pq.Dequeue());
		}
		[TestCase] public void PQNulls() {
			Assert.Throws<ArgumentNullException>(() => new PriorityQueue<string, int>(null)); // Null key selector
			var pq1 = new PriorityQueue<string, int>(s => s.Length, (IComparer<int>)null); // No exception, default comparer.
			var pq2 = new PriorityQueue<string, int>(s => s.Length, null, (IComparer<int>)null, true); // Null collection, no exception.
			Assert.Throws<ArgumentNullException>(() => pq1.Enqueue(null));
			Assert.Throws<ArgumentNullException>(() => pq1.Contains(null));
			Assert.Throws<ArgumentNullException>(() => pq1.Remove(null));
			Assert.Throws<ArgumentNullException>(() => pq1.RemoveAll(null));
			Assert.Throws<ArgumentNullException>(() => pq1.ChangePriority(null, s => s.Contains("")));
			Assert.Throws<ArgumentNullException>(() => pq1.ChangePriority(null, () => { }));
			Assert.Throws<ArgumentNullException>(() => pq1.ChangePriority("", (Action)null));
			Assert.Throws<ArgumentNullException>(() => pq1.ChangePriority("",(Action<string>)null));
		}
	}

	[TestFixture] public class OrderingCollectionTest {
		OrderingCollection<object> oc;
		[SetUp] public void Init() {
			oc = new OrderingCollection<object>();
		}
		[TestCase] public void OrderingBasicOperations() {
			object o1 = new object(), o2 = new object();
			Assert.AreEqual(0, oc.Count);
			oc.InsertAtEnd(o1);
			oc.InsertAtStart(o2);
			Assert.AreEqual(2, oc.Count);
			Assert.IsTrue(oc.Contains(o1) && oc.Contains(o2));
			Assert.AreEqual(1, oc.Compare(o1, o2));
			Assert.AreEqual(-1, oc.Compare(o2, o1));
			Assert.AreEqual(0, oc.Compare(o1, o1));
			Assert.IsTrue(oc.Remove(o1) && oc.Remove(o2));
			Assert.IsFalse(oc.Contains(o1) || oc.Contains(o2));
			Assert.AreEqual(0, oc.Count);
		}
		[TestCase] public void OrderingNullAndMissing() {
			Assert.IsFalse(oc.Contains(new object()));
			Assert.IsFalse(oc.Remove(new object()));
			Assert.Throws<ArgumentNullException>(() => oc.Contains(null));
			Assert.Throws<ArgumentNullException>(() => oc.Remove(null));
			Assert.Throws<ArgumentNullException>(() => oc.Compare(null, null));
			Assert.Throws<KeyNotFoundException>(() => oc.Compare(new object(), new object()));
			Assert.Throws<ArgumentNullException>(() => oc.InsertAtStart(null));
			Assert.Throws<ArgumentNullException>(() => oc.InsertAtEnd(null));
			Assert.Throws<ArgumentNullException>(() => oc.InsertAfter(null, null));
			Assert.Throws<ArgumentNullException>(() => oc.InsertBefore(null, null));
			Assert.Throws<KeyNotFoundException>(() => oc.InsertAfter(new object(), new object()));
			Assert.Throws<KeyNotFoundException>(() => oc.InsertBefore(new object(), new object()));
		}
		[TestCase] public void OrderingInsert() {
			object o1 = new object(), o2 = new object(), o3 = new object(), o4 = new object(), o5 = new object();
			oc.InsertAtEnd(o1); // Current order: o1
			Assert.Throws<InvalidOperationException>(() => oc.InsertAtStart(o1));
			oc.InsertBefore(null, o2); // Current order: o1 o2
			Assert.AreEqual(-1, oc.Compare(o1, o2));
			oc.InsertAfter(null, o3); // Current order: o3 o1 o2
			Assert.AreEqual(-1, oc.Compare(o3, o1));
			Assert.AreEqual(1, oc.Compare(o2, o3));
			oc.InsertBefore(o2, o4); // Current order: o3 o1 o4 o2
			Assert.AreEqual(1, oc.Compare(o4, o1));
			Assert.AreEqual(-1, oc.Compare(o4, o2));
			oc.InsertAfter(o2, o5); // Current order: o3 o1 o4 o2 o5
			Assert.AreEqual(1, oc.Compare(o5, o2));
			Assert.AreEqual(1, oc.Compare(o5, o1));
		}
		[TestCase] public void OrderingCustomComparer() {
			var oc2 = new OrderingCollection<string>(new StringLengthEqualityComparer());
			oc2.InsertAtEnd("aaa");
			Assert.Throws<InvalidOperationException>(() => oc2.InsertAtStart("bbb")); // Length 3 string already exists
			Assert.IsTrue(oc2.Contains("ccc"));
			Assert.AreEqual(0, oc2.Compare("ddd", "eee"));
			Assert.AreEqual(1, oc2.Count);
			Assert.IsTrue(oc2.Remove("fff"));
			Assert.AreEqual(0, oc2.Count);
			oc2.InsertAtStart("AA");
			oc2.InsertBefore("BB", "----");
			Assert.AreEqual(-1, oc2.Compare("!!!!", "??"));
		}
	}
}
