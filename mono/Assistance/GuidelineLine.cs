using System;
using System.Collections.Generic;

namespace Assistance {
	public class GuidelineLine: Guideline {
		protected Point p0, p1;
		protected Point direction;
		
		public GuidelineLine(Point p0, Point p1) {
			this.p0 = p0;
			this.p1 = p1;
			direction = (p1 - p0).normalize();
		}
		
		public override void draw(Cairo.Context context, bool active) {
			Point pp0 = p0;
			Point pp1 = p1;
			Rectangle bounds = Drawing.Helper.getBounds(context);
			Geometry.truncateInfiniteLine(bounds, ref pp0, ref pp1);
			
			context.Save();
			(active ? penActive : pen).apply(context);
			context.MoveTo(pp0.x, pp0.y);
			context.LineTo(pp1.x, pp1.y);
			context.Stroke();
			context.Restore();
		}
		
		public override Track.Point transformPoint(Track.Point p) {
			Track.Point np = p;
			np.position = Point.dot(p.position - p0, direction)*direction + p0;
			return np;
		}
	}
}

