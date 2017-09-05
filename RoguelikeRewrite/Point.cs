using System;
using System.Collections.Generic;

namespace GameComponents {
	public struct Point : IEquatable<Point> {
		public readonly int x, y;

		public Point(int x, int y) { this.x = x;  this.y = y; }
		public static Point operator +(Point left, Point right) => new Point(left.x + right.x, left.y + right.y);
		public static Point operator -(Point left, Point right) => new Point(left.x - right.x, left.y - right.y);
		public static Point operator -(Point right) => new Point(-right.x, -right.y);
		public static Point operator +(Point left, int i) => new Point(left.x + i, left.y + i);
		public static Point operator -(Point left, int i) => new Point(left.x - i, left.y - i);
		public static bool operator ==(Point left, Point right) => left.Equals(right);
		public static bool operator !=(Point left, Point right) => !left.Equals(right);
		public override int GetHashCode() { unchecked { return x * 7757 + y; } }
		public override bool Equals(object other) {
			if(other is Point) return Equals((Point)other);
			else return false;
		}
		public bool Equals(Point other) => x == other.x && y == other.y;
		public static readonly Point Zero = new Point(0, 0);
	}

	public struct CellRectangle : IEquatable<CellRectangle> {
		public readonly Point Position, Size;
		public int x => Position.x;
		public int y => Position.y;
		public int Width => Size.x;
		public int Height => Size.y;
		public int Left => Position.x;
		public int Right => Position.x + Size.x - 1;
		public int Top => Position.y;
		public int Bottom => Position.y + Size.y - 1;
		public Point TopLeft => new Point(Left, Top);
		public Point BottomLeft => new Point(Left, Bottom);
		public Point TopRight => new Point(Right, Top);
		public Point BottomRight => new Point(Right, Bottom);
		public bool IsEmpty => Size.x <= 0 || Size.y <= 0;

		public CellRectangle(Point position, Point size) { this.Position = position;  this.Size = size; }
		public static CellRectangle CreateFromSize(int x, int y, int width, int height) => new CellRectangle(new Point(x, y), new Point(width, height));
		public static CellRectangle CreateFromEdges(int left, int right, int top, int bottom) {
			return new CellRectangle(new Point(left, top), new Point(right-left + 1, bottom-top + 1));
		}
		public static CellRectangle CreateFromPoints(Point p1, Point p2) {
			int resultLeft = Math.Min(p1.x, p2.x);
			int resultRight = Math.Max(p1.x, p2.x);
			int resultTop = Math.Min(p1.y, p2.y);
			int resultBottom = Math.Max(p1.y, p2.y);
			return CreateFromEdges(resultLeft, resultRight, resultTop, resultBottom);
		}
		public IEnumerable<Point> Points {
			get {
				for(int i=Position.x; i<Position.x+Size.x; ++i) {
					for(int j=Position.y; j<Position.y+Size.y; ++j) {
						yield return new Point(i, j);
					}
				}
			}
		}
		public CellRectangle Shrink(int i) => new CellRectangle(Position + i, Size - i*2); // todo: Is there any need for shrink/grow taking 2 ints apiece?
		public CellRectangle Grow(int i) => new CellRectangle(Position - i, Size + i*2); // todo: or what about shrink/grow toward corner?
		public CellRectangle Translate(Point p) => new CellRectangle(Position + p, Size); // todo: what about 90(+) degree rotation around a point?
		public bool Contains(Point p) => p.x >= Position.x && p.x < Position.x+Size.x && p.y >= Position.y && p.y < Position.y+Size.y;
		public bool Contains(CellRectangle other) {
			return Position.x <= other.Position.x && Position.x + Size.x >= other.Position.x + other.Size.x
				&& Position.y <= other.Position.y && Position.y + Size.y >= other.Position.y + other.Size.y;
		}
		public bool Intersects(CellRectangle other) {
			if(this.Left > other.Right || other.Left > this.Right) return false;
			if(this.Top > other.Bottom || other.Top > this.Bottom) return false;
			return true;
		}
		public CellRectangle GetIntersection(CellRectangle other) {
			int resultLeft = Math.Max(this.Left, other.Left);
			int resultTop = Math.Max(this.Top, other.Top);
			int resultRight = Math.Min(this.Right, other.Right);
			int resultBottom = Math.Min(this.Bottom, other.Bottom);
			return CreateFromEdges(resultLeft, resultRight, resultTop, resultBottom);
		}
		public override int GetHashCode() { unchecked { return Position.GetHashCode() + Size.GetHashCode() * 5003; } }
		public override bool Equals(object other) {
			if(other is CellRectangle) return Equals((CellRectangle)other);
			else return false;
		}
		public bool Equals(CellRectangle other) => Position.Equals(other.Position) && Size.Equals(other.Size);
		public static bool operator ==(CellRectangle left, CellRectangle right) => left.Equals(right);
		public static bool operator !=(CellRectangle left, CellRectangle right) => !left.Equals(right);
	}
}
