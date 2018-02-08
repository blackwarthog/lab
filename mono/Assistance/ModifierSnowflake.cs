using System;
using System.Collections.Generic;

namespace Assistance {
	public class ModifierSnowflake: Modifier {
		public static readonly int rays = 6;
	
		public ActivePoint center;

		public ModifierSnowflake(Document document, Point center): base(document) {
			this.center = new ActivePoint(this, ActivePoint.Type.CircleCross, center);
		}

		public override void draw(Cairo.Context context) {
			for(int i = 0; i < rays/2; ++i) {
				Point pp0 = center.position;
				Point pp1 = center.position + new Point(100.0, 0.0).rotate( i*2.0*Math.PI/(double)rays );
				Rectangle bounds = Drawing.Helper.getBounds(context);
				Geometry.truncateInfiniteLine(bounds, ref pp0, ref pp1);
				
				context.Save();
				pen.apply(context);
				context.MoveTo(pp0.x, pp0.y);
				context.LineTo(pp1.x, pp1.y);
				context.Stroke();
				context.Restore();
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

