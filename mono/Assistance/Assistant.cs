using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;

namespace Assistance {
	public class Assistant: ActivePoint.Owner {
		public static readonly double maxLen = 1000.0;
		//public static readonly int gridPointsCount = 100;
		public static readonly Pen pen = Pens.Gray;

		public Assistant(Document document): base(document) {
			document.assistants.Add(this);
		}

		public override void remove() {
			base.remove();
			document.assistants.Remove(this);
		}

		public override void bringToFront() {
			document.assistants.Remove(this);
			document.assistants.Add(this);
		}

		// TODO: ?
		//public double getMaxLen() {
		//	double l = 0.0;
		//	foreach(ActivePoint point in points)
		//		l = Math.Max(l, point.position.len());
		//	return maxLen + l;
		//}

		public virtual void draw(Graphics g) { }

		public virtual void getGuidelines(List<Guideline> outGuidelines, Point target) { }
	}
}
