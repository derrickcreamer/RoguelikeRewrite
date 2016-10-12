using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Points;

namespace Grids { //todo: xml comments, unit testing? same question for point & rectangle.
	public static class ExtGet {
		// These 2 extension methods are intended to make it easier to enumerate over some value for each T in an IEnum<T>.
		// In short, they allow the caller to not care about whether to call Select or SelectMany.
		// The following 2 examples both result in an IEnumerable<int> :
		//   manyObjects.Get(o => o.intValue)
		//   manyObjects.Get(o => o.manyIntValues)
		public static IEnumerable<U> Get<T, U>(this IEnumerable<T> source, Func<T, IEnumerable<U>> selector) {
			return source.SelectMany(selector);
		}
		public static IEnumerable<U> Get<T, U>(this IEnumerable<T> source, Func<T, U> selector) { //todo, move this somewhere cool
			return source.Select(selector);
		}
	}
	public interface ReadonlyGrid<T> : IEnumerable<T>, Rectangular where T : class {
		int Rows { get; }
		int Cols { get; }
		T this[point p] { get; }
		T this[Positioned positioned] { get; }
		IEnumerable<T> this[IEnumerable<point> positions] { get; }
		IEnumerable<T> this[IEnumerable<Positioned> positionedElements] { get; }
	}
	public class Grid<T> : ReadonlyGrid<T>, IEnumerable<T>, Rectangular where T : class {
		protected T[,] objs;
		protected HashSet<T> denseObjs;
		public int Rows { get; protected set; }
		public int Cols { get; protected set; }
		public rectangle rect => new rectangle(point.Zero, new point(Rows, Cols));
		public T this[point p] {
			get {
				if(rect.Contains(p)) return objs[p.x,p.y];
				else return null;
			}
			protected set {
				objs[p.x,p.y] = value; //todo: should this do nothing, or throw an exception, when OOB?
			}
		}
		public T this[Positioned positioned] => this[positioned.p];
		public IEnumerable<T> this[IEnumerable<point> positions] {
			get {
				HashSet<point> returned = new HashSet<point>();
				foreach(point p in positions) {
					T t = this[p];
					if(t != null && returned.Add(p)) {
						yield return t;
					}
				}
			}
		}
		public IEnumerable<T> this[IEnumerable<Positioned> positionedElements] {
			get {
				HashSet<point> returned = new HashSet<point>();
				foreach(Positioned psd in positionedElements) {
					T t = this[psd.p];
					if(t != null && returned.Add(psd.p)) {
						yield return t;
					}
				}
			}
		}
		public bool Move(point source, point destination) {
			// Can only move an existing item, and only to a point in-bounds and empty.
			if(this[source] != null && rect.Contains(destination) && this[destination] == null) {
				this[destination] = this[source];
				this[source] = null;
				return true;
			}
			return false;
		}
		public bool Swap(point source1, point source2) {
			// Can only swap two existing items.
			if(this[source1] != null && this[source2] != null) {
				T t = this[source1];
				this[source1] = this[source2];
				this[source2] = t;
				return true;
			}
			return false;
		}
		public bool Add(T element, point destination) {
			// Can only add a non-null item that isn't already on the grid, and only to a point in-bounds and empty.
			if(element != null && rect.Contains(destination) && this[destination] == null && denseObjs.Add(element)) {
				this[destination] = element;
				return true;
			}
			return false;
		}
		public bool Remove(point source) {
			// Can only remove an existing item.
			if(this[source] != null && denseObjs.Remove(this[source])) {
				this[source] = null;
				return true;
			}
			return false;
		}
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<T> GetEnumerator() { return denseObjs.GetEnumerator(); }
		public Grid(int rows, int cols) {
			Rows = rows;
			Cols = cols;
			objs = new T[rows, cols];
			denseObjs = new HashSet<T>();
		}
	}
	public interface ReadOnlyMultiGrid<T> : IEnumerable<T>, Rectangular where T : class {
		int Rows { get; }
		int Cols { get; }
		IEnumerable<T> this[point p] { get; }
		IEnumerable<T> this[Positioned positioned] { get; }
		IEnumerable<T> this[IEnumerable<point> positions] { get; }
		IEnumerable<T> this[IEnumerable<Positioned> positionedElements] { get; }
	}
	public class MultiGrid<T> : ReadOnlyMultiGrid<T>, IEnumerable<T>, Rectangular where T : class {
		protected HashSet<T>[,] objs;
		protected HashSet<T> denseObjs;
		public int Rows { get; protected set; }
		public int Cols { get; protected set; }
		public rectangle rect => new rectangle(point.Zero, new point(Rows, Cols));
		public IEnumerable<T> this[point p] {
			get {
				if(rect.Contains(p) && objs[p.x,p.y] != null) return objs[p.x, p.y];
				else return Enumerable.Empty<T>();
			}
		}
		public IEnumerable<T> this[Positioned positioned] => this[positioned.p];
		public IEnumerable<T> this[IEnumerable<point> positions] {
			get {
				HashSet<point> returned = new HashSet<point>();
				foreach(point p in positions) {
					if(rect.Contains(p) && objs[p.x, p.y] != null && returned.Add(p)) {
						foreach(T t in objs[p.x,p.y]) {
							yield return t;
						}
					}
				}
			}
		}
		public IEnumerable<T> this[IEnumerable<Positioned> positionedElements] {
			get {
				HashSet<point> returned = new HashSet<point>();
				foreach(Positioned psd in positionedElements) {
					if(rect.Contains(psd.p) && objs[psd.p.x, psd.p.y] != null && returned.Add(psd.p)) {
						foreach(T t in objs[psd.p.x, psd.p.y]) {
							yield return t;
						}
					}
				}
			}
		}
		public bool Move(T element, point source, point destination) {
			// Can only move an existing item, and only to a point in-bounds.
			if(element != null && rect.Contains(source) && rect.Contains(destination)) {
				if(objs[source.x,source.y] != null && objs[source.x,source.y].Remove(element)) {
					if(objs[destination.x,destination.y] == null) objs[destination.x,destination.y] = new HashSet<T>();
					objs[destination.x,destination.y].Add(element);
					return true;
				}
			}
			return false;
		}
		public bool Add(T element, point destination) {
			// Can only add a non-null item that isn't already on the grid, and only to a point in-bounds.
			if(element != null && rect.Contains(destination) && denseObjs.Add(element)) {
				if(objs[destination.x, destination.y] == null) objs[destination.x, destination.y] = new HashSet<T>();
				objs[destination.x, destination.y].Add(element);
				return true;
			}
			return false;
		}
		public bool Remove(T element, point source) {
			// Can only remove an existing item.
			if(element != null && rect.Contains(source)) {
				if(objs[source.x,source.y] != null && objs[source.x,source.y].Remove(element) && denseObjs.Remove(element)) {
					return true;
				}
			}
			return false;
		}
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<T> GetEnumerator() { return denseObjs.GetEnumerator(); }
		public MultiGrid(int rows, int cols) {
			Rows = rows;
			Cols = cols;
			objs = new HashSet<T>[rows, cols];
			denseObjs = new HashSet<T>();
		}
	}
}
