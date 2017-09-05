using System;
using System.Linq;
using NUnit.Framework;
using GameComponents;

namespace PointTests {
	[TestFixture] public class PointTest {
		[TestCase] public void PointEquality() {
			Point zero = new Point(0, 0);
			Point one = new Point(2, 1);
			Point two = new Point(2, 1);
			Assert.IsTrue(one.Equals(two));
			Assert.IsTrue(one == two);
			Assert.IsTrue(zero.Equals(Point.Zero));
			Assert.IsTrue(zero == Point.Zero);
			Assert.IsFalse(one.Equals(zero));
			Assert.IsFalse(one == zero);
			Assert.IsTrue(one != zero);
		}
		[TestCase] public void PointOperators() {
			Point one = new Point(1, 2);
			Point two = new Point(2, 7);
			Assert.AreEqual(new Point(2, 4), one + one);
			Assert.AreEqual(new Point(1, 5), two - one);
			Assert.AreEqual(new Point(4, 5), one + 3);
			Assert.AreEqual(new Point(-3, -2), one - 4);
			Assert.AreEqual(new Point(-2, -7), -two);
		}
	}
	[TestFixture] public class RectangleTest {
		[TestCase] public void RectangleCreationAndEquality() {
			CellRectangle one = new CellRectangle(new Point(1, 2), new Point(10, 5));
			CellRectangle two = CellRectangle.CreateFromEdges(1, 10, 2, 6);
			CellRectangle three = CellRectangle.CreateFromPoints(new Point(1, 2), new Point(10, 6));
			CellRectangle four = CellRectangle.CreateFromPoints(new Point(1, 6), new Point(10, 2));
			CellRectangle five = CellRectangle.CreateFromSize(1, 2, 10, 5);
			Assert.IsTrue(one.Equals(two));
			Assert.IsTrue(one == two);
			Assert.IsTrue(one == three);
			Assert.IsTrue(one == four);
			Assert.IsTrue(one == five);
		}
		[TestCase] public void RectanglePoints() {
			CellRectangle one = CellRectangle.CreateFromSize(-3, 5, 1, 1);
			var oneList = one.Points.ToList();
			Assert.AreEqual(1, oneList.Count);
			Assert.AreEqual(new Point(-3, 5), oneList[0]);

			CellRectangle two = CellRectangle.CreateFromSize(0, 0, 0, 1);
			var twoList = two.Points.ToList();
			Assert.AreEqual(0, twoList.Count);

			CellRectangle three = CellRectangle.CreateFromSize(1, 2, -3, -1);
			var threeList = three.Points.ToList();
			Assert.AreEqual(0, threeList.Count);
		}
		[TestCase] public void ChangeSizeAndPosition() {
			CellRectangle one = CellRectangle.CreateFromSize(0, 0, 1, 4);
			CellRectangle two = one.Grow(2);
			Assert.IsTrue(two == new CellRectangle(new Point(-2, -2), new Point(5, 8)));
			CellRectangle three = one.Shrink(2);
			Assert.IsTrue(three == new CellRectangle(new Point(2, 2), new Point(-3, 0)));
			Assert.IsTrue(one == three.Grow(2));
			CellRectangle four = three.Translate(new Point(3, -8));
			Assert.IsTrue(four == new CellRectangle(new Point(5, -6), new Point(-3, 0)));
		}
		[TestCase] public void RectangleContains() {
			var one = CellRectangle.CreateFromSize(2, 2, 5, 5);
			Assert.IsTrue(one.Contains(new Point(2, 2)));
			Assert.IsTrue(one.Contains(new Point(6, 6)));
			Assert.IsFalse(one.Contains(new Point(7, 7)));
			Assert.IsTrue(one.Contains(CellRectangle.CreateFromSize(4, 4, 3, 3)));
			Assert.IsFalse(one.Contains(CellRectangle.CreateFromSize(1, 2, 5, 5)));
			var two = CellRectangle.CreateFromSize(0, 0, 0, 0);
			var three = CellRectangle.CreateFromSize(0, 0, 1, 1);
			Assert.IsFalse(two.Contains(three));
		}
		[TestCase] public void RectangleIntersects() {
			var one = CellRectangle.CreateFromSize(1, 1, 3, 3);
			Assert.IsTrue(one.Intersects(one));
			Assert.IsTrue(one.GetIntersection(one) == one);
			var two = CellRectangle.CreateFromSize(3, 2, 7, 7);
			Assert.IsTrue(one.Intersects(two));
			var three = one.GetIntersection(two);
			var threeList = three.Points.ToList();
			Assert.AreEqual(2, threeList.Count);
			Assert.IsTrue(three.Contains(new Point(3, 2)));
			Assert.IsTrue(three.Contains(new Point(3, 3)));
		}
	}
}
