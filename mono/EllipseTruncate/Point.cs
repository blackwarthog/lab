using System;

namespace EllipseTruncate {
	public struct Point {
		public double x;
		public double y;

		public Point(double x, double y) {
			this.x = x;
			this.y = y;
		}

		public static Point operator+ (Point a, Point b)
			{ return new Point(a.x + b.x, a.y + b.y); }
		public static Point operator- (Point a, Point b)
			{ return new Point(a.x - b.x, a.y - b.y); }
		public static double operator* (Point b, Point a)
			{ return a.x*b.x + a.y*b.y; }
		public static Point operator* (Point a, double b)
			{ return new Point(a.x*b, a.y*b); }
		public static Point operator* (double b, Point a)
			{ return a*b; }
		public static Point operator/ (Point a, double b)
			{ return new Point(a.x/b, a.y/b); }

		public bool isEqual(Point other)
			{ return (this - other).lenSqr() <= Geometry.precisionSqr; }
		public double lenSqr()
			{ return x*x + y*y; }
		public double len()
			{ return Math.Sqrt(lenSqr()); }

		public Point normalize() {
			double l = len();
			return l > Geometry.precision ? this/l : this;
		}

		public Point rotate90()
			{ return new Point(-y, x); }

		public Point rotate180()
			{ return new Point(-x, -y); }

		public Point rotate270()
			{ return new Point(y, -x); }

		public Point rotate(double angle) {
			double s = Math.Sin(angle);
			double c = Math.Cos(angle);
			return new Point(c*x - s*y, s*x + c*y);
		}
		
		public double atan()
			{ return Math.Atan2(y, x); }
	}
}

