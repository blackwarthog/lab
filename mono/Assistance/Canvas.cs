using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Linq;

namespace Assistance {
	public class Canvas {
		public readonly List<Assistant> assistants = new List<Assistant>();
		public readonly List<ActivePoint> points = new List<ActivePoint>();

		public ActivePoint ActivePoint = null;

		public ActivePoint findPoint(Point position) {
			foreach(ActivePoint point in points.Reverse<ActivePoint>())
				if (point.isInside(position))
					return point;
			return null;
		}

		public void drawGuidlines(Graphics g, Point point) {
			foreach(Assistant assistant in assistants)
				assistant.drawGuidlines(g, point);
		}

		public void drawAssistants(Graphics g) {
			foreach(Assistant assistant in assistants)
				assistant.draw(g);
		}

		public void drawPoints(Graphics g, ActivePoint activePoint) {
			foreach(ActivePoint point in points)
				point.draw(g, activePoint == point ? ActivePoint.Mode.Active : ActivePoint.Mode.Common);
		}

		public void draw(Graphics g, ActivePoint activePoint, Point guidlinesPoint) {
			drawGuidlines(g, guidlinesPoint);
			drawAssistants(g);
			drawPoints(g, activePoint);
		}

		public void draw(Graphics g, ActivePoint activePoint) {
			drawAssistants(g);
			drawPoints(g, activePoint);
		}
	}
}

