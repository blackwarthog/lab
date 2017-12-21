using System;

namespace Assistance {
	public static class Geometry {
		public static readonly double precision = 0.01;
		public static readonly double sqrt2Pi = Math.Sqrt(2.0*Math.PI);
		
		public static double logNormalDistribuitionUnscaled(double x, double x0, double w) {
			return Math.Exp(-0.5*Math.Pow(Math.Log(x/x0)/w, 2.0))/x;
		}
		
		public static double logNormalDistribuition(double x, double x0, double w) {
			return logNormalDistribuition(x, x0, w)/(w*sqrt2Pi);
		}
		
		public static void truncateInfiniteLine(Rectangle bounds, ref Point p0, ref Point p1) {
			if (p0.isEqual(p1)) return;
			Point d = p0 - p1;
			if (Math.Abs(d.x)*bounds.height > bounds.width*Math.Abs(d.y)) {
				// horizontal
				double k = d.y/d.x;
				p1 = new Point(bounds.x1, p0.y + k*(bounds.x1 - p0.x));
				p0 = new Point(bounds.x0, p0.y + k*(bounds.x0 - p0.x));
			} else {
				// vertical
				double k = d.x/d.y;
				p1 = new Point(p0.x + k*(bounds.y1 - p0.y), bounds.y1);
				p0 = new Point(p0.x + k*(bounds.y0 - p0.y), bounds.y0);
			}
		}
	}
}
