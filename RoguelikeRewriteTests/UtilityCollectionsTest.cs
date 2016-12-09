using System;
using System.Collections.Generic;
using System.Linq;
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
}
