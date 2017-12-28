using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;

namespace Assistance {
	public class Assistant {
		public static readonly double maxLen = 1000.0;
		public static readonly int gridPointsCount = 100;
		public static readonly Pen pen = Pens.Gray;

		public readonly Workarea canvas;
		public readonly List<ActivePoint> points = new List<ActivePoint>();

		public Assistant(Workarea canvas) {
			this.canvas = canvas;
			canvas.assistants.Add(this);
		}

		public void remove() {
			foreach(ActivePoint point in points)
				canvas.points.Remove(point);
			canvas.assistants.Remove(this);
		}

		public void bringToFront() {
			canvas.assistants.Remove(this);
			canvas.assistants.Add(this);
		}

		public double getMaxLen() {
			double l = 0.0;
			foreach(ActivePoint point in points)
				l = Math.Max(l, point.position.len());
			return maxLen + l;
		}

		public virtual void onMovePoint(ActivePoint point, Point position) {
			point.position = position;
		}

		public virtual void draw(Graphics g) { }

		public virtual void getGuidelines(List<Guideline> outGuidelines, Point target) { }
	}
}