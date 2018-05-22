using System;

namespace EllipseTruncate {
	public struct Rectangle {
		public double x0, y0, x1, y1;

		public Point p0 {
			get { return new Point(x0, y0); }
			set { x0 = value.x; y0 = value.y; }
		}

		public Point p1 {
			get { return new Point(x1, y1); }
			set { x1 = value.x; y1 = value.y; }
		}

		public double width {
			get { return x1 - x0; }
			set { x1 = x0 + value; }
		}

		public double height {
			get { return y1 - y0; }
			set { y1 = y0 + value; }
		}

		public bool empty {
			get { return width <= Geometry.precision || height <= Geometry.precision; }
		}

		public Rectangle(double x0, double y0, double x1, double y1) {
			this.x0 = x0;
			this.y0 = y0;
			this.x1 = x1;
			this.y1 = y1;
		}
		public Rectangle(double x, double y):
			this(x, y, x, y) { }
		public Rectangle(Point p):
			this(p.x, p.y) { }
		public Rectangle(Point p0, Point p1):
			this(p0.x, p0.y, p1.x, p1.y) { }

		public Rectangle expand(Point p, double radius = 0.0) {
			return new Rectangle(
				Math.Min(x0, p.x),
				Math.Min(y0, p.y),
				Math.Max(x1, p.x),
				Math.Max(y1, p.y) );
		}

		public Rectangle inflate(double x, double y) {
			return new Rectangle(
			    x0 - x,
			    y0 - y,
			    x1 + x,
			    y1 + y );
		}

		public Rectangle inflate(double size) {
			return inflate(size, size);
		}

		public static Rectangle operator| (Rectangle a, Rectangle b) {
			Rectangle rect = a.empty ? b
				           : b.empty ? a
					       : new Rectangle(	Math.Min(a.x0, b.x0),
								        Math.Min(a.y0, b.y0),
								        Math.Max(a.x1, b.x1),
					                    Math.Max(a.y1, b.y1) );
			return rect.empty ? new Rectangle() : rect;
		}

		public static Rectangle operator& (Rectangle a, Rectangle b) {
			Rectangle rect = a.empty ? b
				           : b.empty ? a
					       : new Rectangle( Math.Max(a.x0, b.x0),
					                    Math.Max(a.y0, b.y0),
					                    Math.Min(a.x1, b.x1),
					                    Math.Min(a.y1, b.y1) );
			return rect.empty ? new Rectangle() : rect;
		}
	}
}

