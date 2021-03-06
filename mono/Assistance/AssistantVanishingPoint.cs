using System;
using System.Collections.Generic;

namespace Assistance {
	public class AssistantVanishingPoint: Assistant {
		public ActivePoint center;
		public ActivePoint a0;
		public ActivePoint a1;
		public ActivePoint b0;
		public ActivePoint b1;
		public ActivePoint step;

		public AssistantVanishingPoint(Document document, Point center): base(document) {
			this.center = new ActivePoint(this, ActivePoint.Type.CircleCross, center);
			a0 = new ActivePoint(this, ActivePoint.Type.CircleFill, center + new Point(-100.0, 0.0));
			a1 = new ActivePoint(this, ActivePoint.Type.Circle, center + new Point(-200.0, 0.0));
			b0 = new ActivePoint(this, ActivePoint.Type.CircleFill, center + new Point(100.0, 0.0));
			b1 = new ActivePoint(this, ActivePoint.Type.Circle, center + new Point(200.0, 0.0));
			step = new ActivePoint(this, ActivePoint.Type.Circle, (a0.position + a1.position)/2);
		}

		private void fixCenter() {
			if (!a0.position.isEqual(a1.position) && !b0.position.isEqual(b1.position)) {
				Point a = a0.position;
				Point b = b0.position;
				Point da = a1.position - a;
				Point db = b1.position - b;
				Point ab = b - a;
				double k = db.x*da.y - db.y*da.x;
				if (Math.Abs(k) > 0.00001) {
					double lb = (da.x*ab.y - da.y*ab.x)/k;
					center.position.x = lb*db.x + b.x;
					center.position.y = lb*db.y + b.y;
				}
			}
		}

		private void fixSidePoint(ActivePoint p0, ActivePoint p1, Point previousP0) {
			if (!p0.position.isEqual(center.position)) {
				Point dp0 = p0.position - center.position;
				Point dp1 = p1.position - previousP0;
				p1.position = center.position + dp0*(dp0.len() + dp1.len())/dp0.len();
			}
		}

		private void fixSidePoint(ActivePoint p0, ActivePoint p1) {
			fixSidePoint(p0, p1, p0.position);
		}

		public void fixPoints() {
			fixSidePoint(a0, a1);
			fixSidePoint(a0, step);
			fixSidePoint(b0, b1);
			fixCenter();
		}

		public override void onMovePoint(ActivePoint point, Point position) {
			Point previous = point.position;
			point.position = position;
			if (point == center) {
				a0.position += point.position - previous;
				a1.position += point.position - previous;
				step.position += point.position - previous;
				b0.position += point.position - previous;
				b1.position += point.position - previous;
			} else
			if (point == a0) {
				fixSidePoint(a0, a1, previous);
				fixSidePoint(a0, step, previous);
				fixSidePoint(b0, b1);
			} else
			if (point == b0) {
				fixSidePoint(a0, a1);
				fixSidePoint(a0, step);
				fixSidePoint(b0, b1, previous);
			} else {
				fixCenter();
				fixSidePoint(a0, a1);
				fixSidePoint(a0, step);
				fixSidePoint(b0, b1);
			}
		}

		/*
		public override Point[] getGridPoints(Point target, bool truncate) {
			double k = (a0.position - center.position).len();
			if (k < 0.00001)
				return new Point[0];
			k = (step.position - center.position).len()/k;

			//if (Math.Abs(k - 1.0) < 0.1) return new Point[0];

			Point[] points = new Point[truncate ? gridPointsCount + 1 : gridPointsCount*2 + 1];
			Point a = target - center.position;
			Point b = a;
			points[gridPointsCount] = a + center.position;
			for(int i = 1; i <= gridPointsCount; ++i) {
				a /= k;
				b *= k;
				points[gridPointsCount - i] = a + center.position;
				if (!truncate)
					points[gridPointsCount + i] = b + center.position;
			}
			return points;
		}
		*/
		
		public override void draw(Cairo.Context context) {
			context.Save();
			pen.apply(context);
			context.MoveTo(center.position.x, center.position.y);
			context.LineTo(a0.position.x, a0.position.y);
			context.MoveTo(center.position.x, center.position.y);
			context.LineTo(a1.position.x, a1.position.y);
			context.MoveTo(center.position.x, center.position.y);
			context.LineTo(b0.position.x, b0.position.y);
			context.MoveTo(center.position.x, center.position.y);
			context.LineTo(b1.position.x, b1.position.y);
			context.Stroke();
			context.Restore();
		}

		public override void getGuidelines(List<Guideline> outGuidelines, Point target) {
			if (!target.isEqual(center.position))
				outGuidelines.Add(new GuidelineLine(target, center.position));
		}
	}
}

