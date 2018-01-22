using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;

namespace Assistance {
	public class ModifierSnowflake: Modifier {
		public static readonly int rays = 6;
	
		public ActivePoint center;

		public ModifierSnowflake(Workarea workarea, Point center): base(workarea) {
			this.center = new ActivePoint(this, ActivePoint.Type.CircleCross, center);
		}

		public override void draw(System.Drawing.Graphics g) {
			for(int i = 0; i < rays/2; ++i) {
				Point pp0 = center.position;
				Point pp1 = center.position + new Point(100.0, 0.0).rotate( i*2.0*Math.PI/(double)rays );
				Geometry.truncateInfiniteLine(new Rectangle(g.VisibleClipBounds), ref pp0, ref pp1);
				g.DrawLine(pen, pp0.toFloat(), pp1.toFloat());
			}
		}

		public override void getTransformFuncs(List<Geometry.TransformFunc> transformFuncs) {
			Point p0 = center.position;
			for(int i = 0; i < rays; ++i) {
				double angle = i*2.0*Math.PI/(double)rays;
				double s = Math.Sin(angle);
				double c = Math.Cos(angle);
				
				transformFuncs.Add(delegate(Point p) {
					double x = p.x - p0.x;
					double y = p.y - p0.y;
					return p0 + new Point(c*x - s*y, s*x + c*y);
				});

				transformFuncs.Add(delegate(Point p) {
					double x = p.x - p0.x;
					double y = p.y - p0.y;
					return p0 + new Point(c*x + s*y, s*x - c*y);
				});
			}
		}
	}
}

