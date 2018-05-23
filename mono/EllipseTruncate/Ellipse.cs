using System;
using System.Collections.Generic;

namespace EllipseTruncate {
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

		private bool cutRange(AngleRange range, uint da, double h) {
			if (h <= Geometry.precision - 1.0)
				return true;
			if (h >= 1.0 - Geometry.precision)
				return false;
			uint a = AngleRange.toUInt(Math.Asin(h));
			range.subtract(new AngleRange.Entry(
				da - a, (da + a)^AngleRange.half ));
			return !range.isEmpty();
		}
		
		public void putSegment(Cairo.Context context, double da, double s, double c, double a0, double a1) {
			int cnt = (int)Math.Floor((a1 - a0)/da);
			Point r = new Point(1.0, 0.0).rotate(a0);
			Point p = matrix*r;
			context.MoveTo(p.x, p.y);
			for(int j = 0; j < cnt; ++j) {
				r = new Point(r.x*c - r.y*s, r.y*c + r.x*s);
  				p = matrix*r;
				context.LineTo(p.x, p.y);
			}
			r = new Point(1.0, 0.0).rotate(a1);
			p = matrix*r;
			context.LineTo(p.x, p.y);
		}

		public void drawTruncated(Cairo.Context context, Point b0, Point b1, Point b2) {
			Point dx = matrixInv.turn(b1 - b0);
			Point dy = matrixInv.turn(b2 - b0);
			Point nx = dx.rotate90().normalize();
			Point ny = dy.rotate90().normalize();
			Point o = matrixInv*b0;
			
			uint ax = AngleRange.toUInt(dx.atan());
			uint ay = AngleRange.toUInt(dy.atan());

			double sign = nx*dy;
			if (Math.Abs(sign) <= Geometry.precision) return;
			if (sign < 0.0) {
				nx *= -1.0; ny *= -1.0;
				ax ^= AngleRange.half; ay ^= AngleRange.half;
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
			
			// build ranges
			AngleRange range = new AngleRange(true);
			if ( !cutRange(range, ax, o*nx) ) return;
			if ( !cutRange(range, ax^AngleRange.half, -((o+dx+dy)*nx)) ) return;
			if ( !cutRange(range, ay, (o+dx)*ny) ) return;
			if ( !cutRange(range, ay^AngleRange.half, -((o+dy)*ny)) ) return;
			
			// draw
    		int segments = 100;
    		double da = 2.0*Math.PI/segments;
    		double s = Math.Sin(da);
    		double c = Math.Cos(da);

    		context.Save();
			context.LineWidth = 2.0;
			context.SetSourceRGBA(0.0, 0.0, 1.0, 1.0);
			if (range.isFull()) {
				putSegment(context, da, s, c, 0.0, AngleRange.period);
			} else {
				bool f = range.flip;
				uint prev = range.angles[range.angles.Count - 1];
				foreach(uint a in range.angles) {
					if (f) {
						double a0 = AngleRange.toDouble(prev);
						double a1 = AngleRange.toDouble(a);
						if (a < prev) a1 += AngleRange.period;
						putSegment(context, da, s, c, a0, a1);
					}
					prev = a; f = !f;
				}
			}
			context.Stroke();
			context.Restore();
		}
	}
}

