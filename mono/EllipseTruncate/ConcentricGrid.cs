using System;

namespace EllipseTruncate {
	public class ConcentricGrid {
		Ellipse ellipse;
		double step;
		Point b0, b1, b2;
	
		public ConcentricGrid(Ellipse ellipse, double step, Point b0, Point b1, Point b2) {
			this.ellipse = ellipse;
			this.step = step;
			this.b0 = b0; this.b1 = b1; this.b2 = b2;
		}
	
		public void calcBounds(out double min, out double max) {
			Point o = ellipse.matrixInv*b0;
			Point dx = ellipse.matrixInv.turn(b1 - b0);
			Point dy = ellipse.matrixInv.turn(b2 - b0);
			max = 0.0;
			min = double.PositiveInfinity;
			
			// distance to points
			Point[] corners = new Point[] { o, o+dx, o+dx+dy, o+dy };
			foreach(Point p in corners) {
				double k = p.len();
				if (k < min) min = k;
				if (k > max) max = k;
			}
			
			// distance to sides
			Point[] lines = new Point[] { dx, dy, -1.0*dx, -1.0*dy };
			int positive = 0, negative = 0;
			for(int i = 0; i < 4; ++i) {
				double len2 = lines[i].lenSqr();
				if (len2 <= Geometry.precisionSqr) continue;
				double k = corners[i]*lines[i].rotate90()/Math.Sqrt(len2);
				if (k > Geometry.precision) ++positive;
				if (k < Geometry.precision) ++negative;
				double l = -(corners[i]*lines[i]);
				if (l <= Geometry.precision || l >= len2 - Geometry.precision) continue;
				k = Math.Abs(k);
				if (k < min) min = k;
				if (k > max) max = k;
			}
			
			// if center is inside bounds
			if (min < 0.0 || positive == 0 || negative == 0) min = 0.0;
		}
		
		public void draw(Cairo.Context context) {
			double r2 = Math.Min(
				ellipse.matrix.row0().lenSqr(),
				ellipse.matrix.row1().lenSqr() );
			if (r2 <= Geometry.precisionSqr) return;
			double actualStep = step/Math.Sqrt(r2);
		
			double min, max;
			calcBounds(out min, out max);
			if (max <= min) return;
			if (max - min > 1e5) return;
			int minI = (int)Math.Ceiling((min - 1.0)/actualStep + Geometry.precision);
			int maxI = (int)Math.Ceiling((max - 1.0)/actualStep - Geometry.precision);
			for(int i = minI; i < maxI; ++i) {
				double scale = i*actualStep + 1.0;
				Ellipse e = new Ellipse( ellipse.matrix.scale(scale) );
				e.drawFull(context, true);
				e.drawTruncated(context, b0, b1, b2, true);
			}
		}
	}
}

