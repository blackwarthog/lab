using System;
using System.Drawing;
using System.Drawing.Imaging;
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
		
		public override void draw(Graphics g, bool active) {
			Point pp0 = p0;
			Point pp1 = p1;
			Geometry.truncateInfiniteLine(new Rectangle(g.VisibleClipBounds), ref pp0, ref pp1);
			g.DrawLine(active ? penActive : pen , pp0.toFloat(), pp1.toFloat());
		}
		
		public override Point transformPoint(Point p) {
			return Point.dot(p - p0, direction)*direction + p0;
		}
	}
}

