using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Grids;

namespace GridTests {
	public class Thing { }
	public class Location {
		public static Location Here = new Location();
		public static Location There = new Location();
		public static Location Elsewhere = new Location();
		public static Location FarAway = new Location();
		public static Location FarFarAway = new Location();
	}
	[TestFixture] public class GridTest {

		Grid<Thing, int> grid;

		[SetUp] public void Init() {
			grid = new Grid<Thing, int>(i => i>=0 && i < 1000);
		}
		[TestCase] public void GridAdd() {
			var thing1 = new Thing();
			grid.Add(thing1, 4);
			Assert.AreEqual(4, grid.GetPositionOf(thing1));
			Assert.IsTrue(grid.HasContents(4));
			Assert.IsTrue(grid.Contains(thing1));
			Assert.AreEqual(thing1, grid[4]);
			Assert.AreEqual(1, grid.Count());
			Assert.AreEqual(thing1, grid.First());
			var things = grid[new List<int>{4}];
			Assert.AreEqual(1, things.Count());
			Assert.AreEqual(thing1, things.First());

			Assert.IsFalse(grid.Add(thing1, 999)); // Fails: already present
			Assert.IsTrue(grid.InBounds(999));
			Assert.IsFalse(grid.InBounds(1000));
			Assert.IsFalse(grid.Add(new Thing(), 1000)); // Fails: out of bounds
			Assert.IsTrue(grid.Add(thing1, 4)); // Succeeds: already present, but at correct location
			grid.Add(new Thing(), 3);
			Assert.IsFalse(grid.Add(new Thing(), 3)); // Fails: position occupied
		}
		[TestCase] public void GridRemove() {
			var thing1 = new Thing();
			grid.Add(thing1, 5);
			grid.Remove(thing1);
			Assert.IsFalse(grid.Contains(thing1));
			Assert.IsFalse(grid.HasContents(5));
			grid.Add(thing1, 8);
			grid.RemoveContents(8);
			Assert.IsFalse(grid.Contains(thing1));
			Assert.IsFalse(grid.HasContents(8));
		}
		[TestCase] public void GridNullAndEmpty() {
			Assert.IsFalse(grid.Contains(new Thing()));
			Assert.IsFalse(grid.HasContents(44));
			Assert.IsNull(grid[-3]); // OOB returns null
			Assert.IsNull(grid[0]); // In bounds & not present returns null
			Assert.AreEqual(0, grid[new List<int>{ 2, 4, -1 }].Count()); // OOB and not present return empty collection

			Assert.Throws<KeyNotFoundException>(() => grid.GetPositionOf(new Thing())); // Throws for missing element

			Thing nullThing = null;
			Assert.Throws<ArgumentNullException>(() => grid.GetPositionOf(nullThing)); // Throws for null elements:
			Assert.Throws<ArgumentNullException>(() => grid.Contains(nullThing));
			Assert.Throws<ArgumentNullException>(() => grid.Add(nullThing, 1));
			Assert.Throws<ArgumentNullException>(() => grid.Move(nullThing, 1));
			Assert.Throws<ArgumentNullException>(() => grid.Swap(new Thing(), nullThing));
			Assert.Throws<ArgumentNullException>(() => grid.SwapContents(1, nullThing));
			Assert.Throws<ArgumentNullException>(() => grid.Replace(new Thing(), nullThing));
			Assert.Throws<ArgumentNullException>(() => grid.ReplaceContents(1, nullThing));
			Assert.Throws<ArgumentNullException>(() => grid.Remove(nullThing));
			IEnumerable<int> nullPositions = null;
			Assert.Throws<ArgumentNullException>(() => grid[nullPositions].Count()); // Throws for null IEnumerable

			var locationGrid = new Grid<Thing, Location>(); // Test with a reference type for TPosition:
			Location nullLocation = null;
			Assert.Throws<ArgumentNullException>(() => locationGrid[nullLocation].ToString());
			Assert.Throws<ArgumentNullException>(() => locationGrid[new List<Location> { Location.Elsewhere, nullLocation }].Count());
			Assert.Throws<ArgumentNullException>(() => locationGrid.HasContents(nullLocation));
			Assert.Throws<ArgumentNullException>(() => locationGrid.Add(new Thing(), nullLocation));
			Assert.Throws<ArgumentNullException>(() => locationGrid.Move(new Thing(), nullLocation));
			Assert.Throws<ArgumentNullException>(() => locationGrid.MoveContents(new Location(), nullLocation));
			Assert.Throws<ArgumentNullException>(() => locationGrid.MoveContents(nullLocation, new Location()));
			Assert.Throws<ArgumentNullException>(() => locationGrid.SwapContents(new Location(), nullLocation));
			Assert.Throws<ArgumentNullException>(() => locationGrid.SwapContents(nullLocation, new Thing()));
			Assert.Throws<ArgumentNullException>(() => locationGrid.ReplaceContents(nullLocation, new Thing()));
			Assert.Throws<ArgumentNullException>(() => locationGrid.RemoveContents(nullLocation));
		}
		[TestCase] public void GridMove() {
			var thing1 = new Thing();
			grid.Add(thing1, 7);
			Assert.IsFalse(grid.Move(thing1, -1)); // Fails: OOB
			Assert.IsFalse(grid.Move(new Thing(), 3)); // Fails: element not on grid
			Assert.IsTrue(grid.Move(thing1, 2)); // Succeeds: actually moved
			Assert.AreSame(thing1, grid[2]);
			Assert.AreEqual(2, grid.GetPositionOf(thing1));
			grid.Add(new Thing(), 9);
			Assert.IsFalse(grid.Move(thing1, 9)); // Fails: position occupied
			Assert.IsTrue(grid.Move(thing1, 2)); // Succeeds: already at correct location

			Assert.IsTrue(grid.MoveContents(44, 55)); // Succeeds: nothing left behind in '44'
			Assert.IsTrue(grid.MoveContents(44, 9)); // Succeeds: nothing left behind in '44'
			Assert.IsTrue(grid.MoveContents(44, 44)); // Succeeds: same destination
			Assert.IsTrue(grid.MoveContents(9, 9)); // Succeeds: same destination
			Assert.IsFalse(grid.MoveContents(2, -2)); // Fails: OOB
			Assert.IsFalse(grid.MoveContents(2, 9)); // Fails: position occupied
			Assert.IsTrue(grid.MoveContents(2, 15)); // Succeeds: actually moved
			Assert.AreSame(thing1, grid[15]);
			Assert.AreEqual(15, grid.GetPositionOf(thing1));
		}
		[TestCase] public void GridSwap() {
			var thing1 = new Thing();
			grid.Add(thing1, 11);
			var thing2 = new Thing();
			grid.Add(thing2, 22);
			Assert.IsTrue(grid.Swap(thing1, thing2)); // Succeeds
			Assert.AreSame(thing1, grid[22]);
			Assert.AreEqual(22, grid.GetPositionOf(thing1));
			Assert.AreSame(thing2, grid[11]);
			Assert.AreEqual(11, grid.GetPositionOf(thing2));

			Assert.IsFalse(grid.Swap(thing1, new Thing())); // Fails: 2nd argument not on grid

			Assert.IsTrue(grid.SwapContents(44, 55)); // Succeeds: nothing to swap
			Assert.IsTrue(grid.SwapContents(-3, -2)); // Succeeds: nothing to swap, despite OOB
			Assert.IsFalse(grid.SwapContents(22, -2)); // Fails: can't swap contents of '22' to an OOB position
			Assert.IsTrue(grid.SwapContents(44, 22)); // Succeeds: empty 44 swapped with occupied 22 (thing1)
			Assert.IsTrue(grid.SwapContents(44, 11)); // Succeeds: occupied 44 (thing1) swapped with occupied 11 (thing2)
			Assert.AreSame(thing2, grid[44]);
			Assert.AreSame(thing1, grid[11]);
			Assert.IsNull(grid[22]);

			Assert.IsFalse(grid.SwapContents(3, new Thing())); // Fails: element not on grid
			Assert.IsFalse(grid.SwapContents(-5, thing1)); // Fails: can't swap thing1 to an OOB position
			Assert.IsTrue(grid.SwapContents(66, thing1)); // Succeeds: thing1 now at (previously unoccupied) 66
			Assert.AreSame(thing1, grid[66]);
			Assert.IsTrue(grid.SwapContents(44, thing1)); // Succeeds: thing1 now at (previously occupied by thing2) 44
			Assert.AreSame(thing1, grid[44]);
			Assert.AreSame(thing2, grid[66]);
		}
		[TestCase] public void GridReplace() {
			var thing1 = new Thing();
			grid.Add(thing1, 10);
			var thing2 = new Thing();
			grid.Add(thing2, 12);
			var thing3 = new Thing();
			Assert.IsFalse(grid.Replace(thing3, new Thing())); // Fails: thing3 not on grid
			Assert.IsFalse(grid.Replace(thing2, thing1)); // Fails: thing1 already on grid
			Assert.IsTrue(grid.Replace(thing2, thing3)); // Succeeds: thing3 replaces thing2 at position 12
			Assert.AreSame(thing3, grid[12]);
			Assert.IsFalse(grid.Contains(thing2));

			Assert.IsFalse(grid.ReplaceContents(12, thing1)); // Fails: thing1 already on grid
			Assert.IsFalse(grid.ReplaceContents(-3, new Thing())); // Fails: OOB
			Assert.IsTrue(grid.ReplaceContents(12, new Thing())); // Succeeds: new element replaces thing3 at position 12
			Assert.IsFalse(grid.Contains(thing3));
			Assert.IsTrue(grid.ReplaceContents(77, new Thing())); // Succeeds: new element replaces nothing at position 77
			Assert.IsTrue(grid.HasContents(77));
		}
		[TestCase] public void GridReferencePositions() {
			var locationGrid = new Grid<Thing, Location>(loc => loc != Location.FarFarAway); // All other locations are in bounds.
			var thing1 = new Thing();
			Assert.IsFalse(locationGrid.Add(new Thing(), Location.FarFarAway)); // Fails: OOB
			locationGrid.Add(thing1, Location.Here);
			Assert.IsTrue(locationGrid.HasContents(Location.Here));
			Assert.AreSame(thing1, locationGrid[Location.Here]);
			Assert.AreSame(Location.Here, locationGrid.GetPositionOf(thing1));
			var thing2 = new Thing();
			locationGrid.Add(thing2, Location.There);
			Assert.IsFalse(locationGrid.Add(new Thing(), Location.There)); // Fails: already occupied
			Assert.IsTrue(locationGrid.Move(thing2, Location.Elsewhere)); // Succeeds: moved
			Assert.IsTrue(locationGrid.MoveContents(Location.Elsewhere, Location.There)); // Succeeds: moved
			Assert.IsTrue(locationGrid.SwapContents(Location.FarAway, Location.FarFarAway)); // Succeeds: nothing swapped
			Assert.IsTrue(locationGrid.ReplaceContents(Location.FarAway, new Thing())); // Succeeds: new element replaced nothing
			Assert.IsFalse(locationGrid.ReplaceContents(Location.FarFarAway, new Thing())); // Fails: new element can't be placed OOB
		}
	}
	[TestFixture] public class MultiGridTest {

		MultiGrid<Thing, int> grid;

		[SetUp] public void Init() {
			grid = new MultiGrid<Thing, int>(i => i>=0 && i < 1000);
		}
		[TestCase] public void MultiGridAdd() {
			var thing1 = new Thing();
			grid.Add(thing1, 4);
			Assert.AreEqual(4, grid.GetPositionOf(thing1));
			Assert.IsTrue(grid.HasContents(4));
			Assert.IsTrue(grid.Contains(thing1));
			Assert.AreEqual(thing1, grid[4].First());
			Assert.AreEqual(1, grid.Count());
			Assert.AreEqual(thing1, grid.First());
			var things = grid[new List<int>{4}];
			Assert.AreEqual(1, things.Count());
			Assert.AreEqual(thing1, things.First());

			Assert.IsFalse(grid.Add(thing1, 999)); // Fails: already present
			Assert.IsTrue(grid.InBounds(999));
			Assert.IsFalse(grid.InBounds(1000));
			Assert.IsFalse(grid.Add(new Thing(), 1000)); // Fails: out of bounds
			Assert.IsTrue(grid.Add(thing1, 4)); // Succeeds: already present, but at correct location
			grid.Add(new Thing(), 3);
			Assert.IsTrue(grid.Add(new Thing(), 3)); // Succeeds: occupied position OK for multigrid
		}
		[TestCase] public void MultiGridRemove() {
			var thing1 = new Thing();
			grid.Add(thing1, 5);
			grid.Remove(thing1);
			Assert.IsFalse(grid.Contains(thing1));
			Assert.IsFalse(grid.HasContents(5));
			grid.Add(thing1, 8);
			grid.Add(new Thing(), 8);
			grid.RemoveContents(8);
			Assert.IsFalse(grid.Contains(thing1));
			Assert.IsFalse(grid.HasContents(8));
		}
		[TestCase] public void MultiGridNullAndEmpty() {
			Assert.IsFalse(grid.Contains(new Thing()));
			Assert.IsFalse(grid.HasContents(44));
			Assert.AreEqual(0, grid[-3].Count()); // OOB returns empty IEnumerable
			Assert.AreEqual(0, grid[0].Count()); // In bounds & not present returns empty IEnumerable
			Assert.AreEqual(0, grid[new List<int>{ 2, 4, -1 }].Count()); // OOB and not present return empty collection

			Assert.Throws<KeyNotFoundException>(() => grid.GetPositionOf(new Thing())); // Throws for missing element

			Thing nullThing = null;
			Assert.Throws<ArgumentNullException>(() => grid.GetPositionOf(nullThing)); // Throws for null elements:
			Assert.Throws<ArgumentNullException>(() => grid.Contains(nullThing));
			Assert.Throws<ArgumentNullException>(() => grid.Add(nullThing, 1));
			Assert.Throws<ArgumentNullException>(() => grid.Move(nullThing, 1));
			Assert.Throws<ArgumentNullException>(() => grid.Swap(new Thing(), nullThing));
			Assert.Throws<ArgumentNullException>(() => grid.Replace(new Thing(), nullThing));
			Assert.Throws<ArgumentNullException>(() => grid.Remove(nullThing));
			IEnumerable<int> nullPositions = null;
			Assert.Throws<ArgumentNullException>(() => grid[nullPositions].Count()); // Throws for null IEnumerable

			var locationGrid = new MultiGrid<Thing, Location>(); // Test with a reference type for TPosition:
			Location nullLocation = null;
			Assert.Throws<ArgumentNullException>(() => locationGrid[nullLocation].ToString());
			Assert.Throws<ArgumentNullException>(() => locationGrid[new List<Location> { Location.Elsewhere, nullLocation }].Count());
			Assert.Throws<ArgumentNullException>(() => locationGrid.HasContents(nullLocation));
			Assert.Throws<ArgumentNullException>(() => locationGrid.Add(new Thing(), nullLocation));
			Assert.Throws<ArgumentNullException>(() => locationGrid.Move(new Thing(), nullLocation));
			Assert.Throws<ArgumentNullException>(() => locationGrid.MoveContents(new Location(), nullLocation));
			Assert.Throws<ArgumentNullException>(() => locationGrid.MoveContents(nullLocation, new Location()));
			Assert.Throws<ArgumentNullException>(() => locationGrid.SwapContents(new Location(), nullLocation));
			Assert.Throws<ArgumentNullException>(() => locationGrid.RemoveContents(nullLocation));
		}
		[TestCase] public void MultiGridMove() {
			var thing1 = new Thing();
			grid.Add(thing1, 7);
			Assert.IsFalse(grid.Move(thing1, -1)); // Fails: OOB
			Assert.IsFalse(grid.Move(new Thing(), 3)); // Fails: element not on grid
			Assert.IsTrue(grid.Move(thing1, 2)); // Succeeds: actually moved
			Assert.IsTrue(grid[2].Contains(thing1));
			Assert.AreEqual(2, grid.GetPositionOf(thing1));
			Assert.IsTrue(grid.Move(thing1, 2)); // Succeeds: already at correct location
			grid.Add(new Thing(), 9);
			Assert.IsTrue(grid.Move(thing1, 9)); // Succeeds: occupied position OK for multigrid
			grid.Move(thing1, 2);
			Assert.IsTrue(grid.MoveContents(44, 55)); // Succeeds: nothing left behind in '44'
			Assert.IsTrue(grid.MoveContents(44, 9)); // Succeeds: nothing left behind in '44'
			Assert.IsTrue(grid.MoveContents(44, 44)); // Succeeds: same destination
			Assert.IsTrue(grid.MoveContents(9, 9)); // Succeeds: same destination
			Assert.IsFalse(grid.MoveContents(2, -2)); // Fails: OOB
			Assert.IsTrue(grid.MoveContents(2, 9)); // Succeeds: occupied position OK for multigrid
			Assert.IsTrue(grid[9].Contains(thing1));
			Assert.IsTrue(grid.MoveContents(9, 15)); // Succeeds: actually moved
			Assert.IsTrue(grid[15].Contains(thing1));
			Assert.AreEqual(15, grid.GetPositionOf(thing1));
		}
		[TestCase] public void MultiGridSwap() {
			var thing1 = new Thing();
			grid.Add(thing1, 11);
			var thing2 = new Thing();
			grid.Add(thing2, 22);
			Assert.IsTrue(grid.Swap(thing1, thing2)); // Succeeds
			Assert.IsTrue(grid[22].Contains(thing1));
			Assert.AreEqual(22, grid.GetPositionOf(thing1));
			Assert.IsTrue(grid[11].Contains(thing2));
			Assert.AreEqual(11, grid.GetPositionOf(thing2));

			Assert.IsFalse(grid.Swap(thing1, new Thing())); // Fails: 2nd argument not on grid

			Assert.IsTrue(grid.SwapContents(44, 55)); // Succeeds: nothing to swap
			Assert.IsTrue(grid.SwapContents(-3, -2)); // Succeeds: nothing to swap, despite OOB
			Assert.IsFalse(grid.SwapContents(22, -2)); // Fails: can't swap contents of '22' to an OOB position
			Assert.IsTrue(grid.SwapContents(44, 22)); // Succeeds: empty 44 swapped with occupied 22 (thing1)
			Assert.IsTrue(grid.SwapContents(44, 11)); // Succeeds: occupied 44 (thing1) swapped with occupied 11 (thing2)
			Assert.IsTrue(grid[44].Contains(thing2));
			Assert.IsTrue(grid[11].Contains(thing1));
			Assert.AreEqual(0, grid[22].Count());
		}
		[TestCase] public void MultiGridReplace() {
			var thing1 = new Thing();
			grid.Add(thing1, 10);
			var thing2 = new Thing();
			grid.Add(thing2, 12);
			var thing3 = new Thing();
			Assert.IsFalse(grid.Replace(thing3, new Thing())); // Fails: thing3 not on grid
			Assert.IsFalse(grid.Replace(thing2, thing1)); // Fails: thing1 already on grid
			Assert.IsTrue(grid.Replace(thing2, thing3)); // Succeeds: thing3 replaces thing2 at position 12
			Assert.IsTrue(grid[12].Contains(thing3));
			Assert.IsFalse(grid.Contains(thing2));
		}
	}
}
