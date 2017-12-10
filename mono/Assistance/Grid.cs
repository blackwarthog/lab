using System;

namespace Assistance {
	public class Grid: Assistant {
		public ActivePoint center;

		public Grid(Canvas canvas, Point center): base(canvas) {
			this.center = new ActivePoint(this, ActivePoint.Type.CircleCross, center);
		}

		public override void draw(System.Drawing.Graphics g) {
			foreach(Assistant assistant in canvas.assistants)
				foreach(Point p in assistant.getGridPoints(center.position, true))
					foreach(Assistant a in canvas.assistants)
						if (a != assistant)
							a.drawGuidlines(g, p, true);
		}
	}
}

