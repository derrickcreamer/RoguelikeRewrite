using System;
using System.Collections;
using System.Collections.Generic;

namespace Points {
	public interface Positioned {
		point p { get; }
	}

	public struct point : IEquatable<point>{
		public readonly int x, y;

		public point(int x, int y) { this.x = x;  this.y = y; }
		public static point operator +(point left,point right) => new point(left.x + right.x, left.y + right.y);
		public static point operator -(point left, point right) => new point(left.x - right.x, left.y - right.y);
		public static point operator -(point right) => new point(-right.x, -right.y);
		public static point operator +(point left, int i) => new point(left.x + i, left.y + i);
		public static point operator -(point left, int i) => new point(left.x - i, left.y - i);
		public static bool operator ==(point left, point right) => left.Equals(right);
		public static bool operator !=(point left, point right) => !left.Equals(right);
		public override int GetHashCode() { unchecked { return x * 7757 + y; } }
		public override bool Equals(object other) {
			if(other is point) return Equals((point)other);
			else return false;
		}
		public bool Equals(point other) => x == other.x && y == other.y;
		public static readonly point Zero = new point(0, 0);
	}

	public interface Rectangular {
		rectangle rect { get; }
	}

	public struct rectangle : IEquatable<rectangle>, IEnumerable<point>{
		public readonly point position, size;
		//todo: x, y, width, height? properties?
		//
		//todo: also check why i need this struct instead of using system's rectangle.

		public rectangle(point position, point size) { this.position = position;  this.size = size; }
		public rectangle(int x, int y, int width, int height) { position = new point(x, y); size = new point(width, height); }
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<point> GetEnumerator() {
			for(int i=position.x;i<position.x+size.x;++i) {
				for(int j= position.y;j<position.y+size.y;++j) {
					yield return new point(i,j);
				}
			}
		}
		public bool Contains(point p) => p.x >= position.x && p.x < position.x+size.x && p.y >= position.y && p.y < position.y+size.y;
		public bool Contains(rectangle other) {
			return position.x <= other.position.x && position.x + size.x >= other.position.x + other.size.x
				&& position.y <= other.position.y && position.y + size.y >= other.position.y + other.size.y;
		}
		public rectangle Shrink(int i) => new rectangle(position + i, size - i);
		public rectangle Grow(int i) => new rectangle(position - i, size + i);
		public override int GetHashCode() { unchecked { return position.GetHashCode() + size.GetHashCode() * 5003; } }
		public override bool Equals(object other) {
			if(other is rectangle) return Equals((rectangle)other);
			else return false;
		}
		public bool Equals(rectangle other) => position.Equals(other.position) && size.Equals(other.size);
		public static bool operator ==(rectangle left, rectangle right) => left.Equals(right);
		public static bool operator !=(rectangle left, rectangle right) => !left.Equals(right);
		//if needed, add Contains(rectangle), and Shrink(pos) & Grow(pos) which can shrink X and Y by different amounts.
		// also Move(point)
		//what about overlaps? returns bool or rectangle?
		// width/height properties? or fields?
	}
}
