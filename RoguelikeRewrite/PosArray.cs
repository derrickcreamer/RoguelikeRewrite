using System;
using System.Collections;
using System.Collections.Generic;
namespace PosArrays {
	public interface Positioned {
		int Row { get; }
		int Col { get; }
	}
	public struct pos : Positioned {
		public int row;
		public int col;
		public int Row => row;
		public int Col => col;
		public pos(int r, int c) {
			row = r;
			col = c;
		}
	}
	public class PosArray<T> { //a 2D array with a position indexer, 1D indexer, and IEnum yielding indexer in addition to the usual 2D indexer.
		public T[,] objs;
		public T this[int row, int col] {
			get {
				return objs[row, col];
			}
			set {
				objs[row, col] = value;
			}
		}
		public T this[pos p] {
			get {
				return objs[p.row, p.col];
			}
			set {
				objs[p.row, p.col] = value;
			}
		}
		public T this[int idx] {
			get {
				return objs[idx / objs.GetLength(1), idx % objs.GetLength(1)];
			}
			set {
				objs[idx / objs.GetLength(1), idx % objs.GetLength(1)] = value;
			}
		}
		public IEnumerable<T> this[IEnumerable<pos> positions] {
			get {
				foreach(pos p in positions) yield return objs[p.row,p.col];
			}
		}
		public IEnumerator GetEnumerator() {
			return objs.GetEnumerator();
		}
		//todo: IEnum<T>? interface?
		public PosArray(int rows, int cols) {
			objs = new T[rows, cols];
		}
	}
}
