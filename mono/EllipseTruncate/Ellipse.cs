using System;

namespace EllipseTruncate {
	public struct AngleRange {
		public double angle, size;
		
		public AngleRange(double angle, double size)
			{ this.angle = angle; this.size = size; }
		public bool isEmpty()
			{ return size <= Geometry.precision; }
		public bool isFull()
			{ return size >= 2.0*Math.PI - Geometry.precision; }
		public double end()
			{ return wrap(angle + size); }
		public bool contains(double a)
			{ return diff(a, angle) < size; }
		public bool intersects(AngleRange r)
			{ return contains(r.angle) || r.contains(angle); }
		public AngleRange union(AngleRange r) {
			double da = diff(r.angle, angle);
			if (da <= size + Geometry.precision)
				return new AngleRange(angle, Math.Max(size, da + r.size));
			da = 2.0*Math.PI - da;
			if (da <= r.size + Geometry.precision)
				return new AngleRange(r.angle, Math.Max(r.size, da + size));
			da = Math.Min(angle, r.angle);
			return new AngleRange(da, Math.Max(angle + size, r.angle + r.size) - da);
		}

		public static AngleRange byAngles(double a0, double a1)
			{ return new AngleRange(a0, diff(a1, a0)); }
		public static double diff(double a1, double a0)
			{ return a0 < a1 ? a1 - a0 : a1 - a0 + 2.0*Math.PI; }
		public static double wrap(double angle) {
			return angle >  Math.PI ? angle - 2.0*Math.PI
			     : angle < -Math.PI ? angle + 2.0*Math.PI
			     : angle;
		}
	}

	public class Ellipse {
		public Point center;
		public Point p1;
		public Point p2;
		public double r1;
		public double r2;
		public double angle;
		
		public Matrix matrix;
		public Matrix matrixInv;
	
		public Ellipse(Point p0, Point p1, Point p2) {
			center = p0;
			this.p1 = p1;
			Point d = p1 - p0;
			r1 = d.len();
			angle = d.atan();
			Point dp = d.rotate90()/r1;
			r2 = Math.Abs(dp*(p2 - p0));
			this.p2 = p0 + dp*r2;
			
			matrix = Matrix.identity()
					.translate(center)
			        .rotate(angle)
			        .scale(r1, r2);
			matrixInv = matrix.invert();
		}
		
		public void drawFull(Cairo.Context context) {
    		int segments = 100;
    		double da = 2.0*Math.PI/segments;
    		double s = Math.Sin(da);
    		double c = Math.Cos(da);
    		Point r = new Point(1.0, 0.0);

			context.Save();
			context.Matrix = (new Matrix(context.Matrix)*matrix).toCairo();
			context.SetSourceRGBA(1.0, 0.0, 0.0, 0.1);
			context.Arc(0.0, 0.0, 1.0, 0.0, 2.0*Math.PI);
			context.ClosePath();
			context.Fill();
			context.Restore();

    		context.Save();
			context.LineWidth = 0.5;
			context.SetSourceRGBA(1.0, 0.0, 0.0, 0.5);
			Point p = matrix*r;
			for(int i = 0; i < segments; ++i) {
				r = new Point(r.x*c - r.y*s, r.y*c + r.x*s);
  				p = matrix*r;
				context.LineTo(p.x, p.y);
			}
			context.ClosePath();
			context.Stroke();
    		context.Restore();
		}
		
		private static void swap(ref double a, ref double b)
			{ double c = a; a = b; b = c; }
		private static void swap(ref int a, ref int b)
			{ int c = a; a = b; b = c; }

		private bool cutRange(ref int index, AngleRange[] ranges, double da, double h) {
			if (h <= Geometry.precision - 1.0) {
				return true;
			} else
			if (h >= 1.0 - Geometry.precision) {
				return false;
			} else {
				double a = Math.Asin(h);
				ranges[index].angle = AngleRange.wrap( a < 0.0
				                                     ? da - a - Math.PI
				                                     : da - a + Math.PI );
				ranges[index].size = Math.PI + a + a;
				int j = index;
				for(int i = 0, r = 0; i < index;) {
					if (r > 0) ranges[i] = ranges[i + r];
					if (ranges[i].intersects(ranges[j])) {
						if (j < i) { ranges[j] = ranges[i].union(ranges[j]); ++r; --index; }
						      else { ranges[i] = ranges[i].union(ranges[j]); j = i; ++i; }
						if (ranges[j].isFull())
							return false;
					} else ++i;
				}
				if (j == index) ++index;
			}
			return true;
		}
		
		public void drawTruncated(Cairo.Context context, Point b0, Point b1, Point b2) {
			Point dx = matrixInv.turn(b1 - b0);
			Point dy = matrixInv.turn(b2 - b0);
			Point nx = dx.rotate90().normalize();
			Point ny = dy.rotate90().normalize();
			Point o = matrixInv*b0;
			
			double ax = dx.atan();
			double ay = dy.atan();
			double aax = AngleRange.wrap(ax + Math.PI);
			double aay = AngleRange.wrap(ay + Math.PI);

			double sign = nx*dy;
			if (Math.Abs(sign) <= Geometry.precision) return;
			if (sign < 0.0) {
				double a;
				nx *= -1.0; ny *= -1.0;
				a = ax; ax = aax; aax = a;
				a = ay; ay = aay; aay = a;
			}

			context.Save();
			context.Matrix = (new Matrix(context.Matrix)*matrix).toCairo();
			context.SetSourceRGBA(1.0, 0.0, 0.0, 0.1);
			context.MoveTo(o.x, o.y);
			context.RelLineTo(dx.x, dx.y);
			context.RelLineTo(dy.x, dy.y);
			context.RelLineTo(-dx.x, -dx.y);
			context.ClosePath();
			context.Fill();
			context.Restore();
			
			// gather invisible ranges
			AngleRange[] cutRanges = new AngleRange[4];
			int count = 0;
			if ( !cutRange(ref count, cutRanges, ax , (o*nx))          ) return;
			if ( !cutRange(ref count, cutRanges, aax, -((o+dx+dy)*nx)) ) return;
			if ( !cutRange(ref count, cutRanges, ay , (o+dx)*ny)       ) return;
			if ( !cutRange(ref count, cutRanges, aay, -((o+dy)*ny))    ) return;
			
			// sort bounds
			for(int i = 0; i < count; ++i)
				for(int j = i+1; j < count; ++j)
					if (cutRanges[j].angle < cutRanges[i].angle)
						{ AngleRange r = cutRanges[i]; cutRanges[i] = cutRanges[j]; cutRanges[j] = r; }
						
			// invert bounds
			AngleRange[] ranges = new AngleRange[4];
			for(int i = 0; i < count; ++i)
				ranges[i] = AngleRange.byAngles(cutRanges[(i > 0 ? i : count) - 1].end(), cutRanges[i].angle);
			
			// dummy 
			if (count == 0)
				ranges[count++] = new AngleRange(0.0, 2.0*Math.PI);
			
			// draw
    		int segments = 100;
    		double da = 2.0*Math.PI/segments;
    		double s = Math.Sin(da);
    		double c = Math.Cos(da);

    		context.Save();
			context.LineWidth = 2.0;
			context.SetSourceRGBA(0.0, 0.0, 1.0, 1.0);
			for(int i = 0; i < count; ++i) {
				double angle = ranges[i].angle;
				double size = ranges[i].size;
    			int cnt = (int)Math.Floor(size/da);
    			Point r = new Point(1.0, 0.0).rotate(angle);
    			Point p = matrix*r;
    			context.MoveTo(p.x, p.y);
				for(int j = 0; j < cnt; ++j) {
					r = new Point(r.x*c - r.y*s, r.y*c + r.x*s);
	  				p = matrix*r;
					context.LineTo(p.x, p.y);
				}
    			r = new Point(1.0, 0.0).rotate(angle + size);
    			p = matrix*r;
    			context.LineTo(p.x, p.y);
			}
			context.Stroke();
			context.Restore();
		}
	}
}

