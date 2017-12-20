using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;

namespace Assistance {
	public class ActivePoint {
		public enum Type {
			Circle,
			CircleFill,
			CircleCross,
		};

		public static readonly double radius = 10.0;
		public static readonly double crossSize = 1.2*radius;
		public static readonly Pen pen = Pens.Gray;
		public static readonly Brush brush = Brushes.LightGray;
		public static readonly Pen penActive = Pens.Blue;
		public static readonly Brush brushActive = Brushes.LightBlue;

		public readonly Workarea canvas;
		public readonly Assistant assistant;
		public readonly Type type;
		public Point position;

		public ActivePoint(Assistant assistant, Type type, Point position = new Point()) {
			this.canvas = assistant.canvas;
			this.assistant = assistant;
			this.type = type;
			this.position = position;
			canvas.points.Add(this);
			assistant.points.Add(this);
		}

		public bool isInside(Point p) {
			return (position - p).lenSqr() <= radius*radius;
		}

		public void bringToFront() {
			assistant.bringToFront();
			assistant.points.Remove(this);
			assistant.points.Add(this);
			canvas.points.Remove(this);
			canvas.points.Add(this);
		}

		private Pen getPen(bool active) {
			return active ? penActive : pen;
		}

		private Brush getBrush(bool active) {
			return active ? brushActive : brush;
		}

		private void drawCircle(Graphics g, bool active) {
			g.DrawEllipse(getPen(active), (float)(position.x - radius), (float)(position.y - radius), (float)(2.0*radius), (float)(2.0*radius));
		}

		private void fillCircle(Graphics g, bool active) {
			g.FillEllipse(getBrush(active), (float)(position.x - radius), (float)(position.y - radius), (float)(2.0*radius), (float)(2.0*radius));
		}

		private void drawCross(Graphics g, bool active) {
			g.DrawLine(getPen(active), (float)(position.x - crossSize), (float)position.y, (float)(position.x + crossSize), (float)position.y);
			g.DrawLine(getPen(active), (float)position.x, (float)(position.y - crossSize), (float)position.x, (float)(position.y + crossSize));
		}

		public void draw(Graphics g, bool active = false) {
			switch(type) {
			case Type.Circle:
				drawCircle(g, active);
				break;
			case Type.CircleFill:
				fillCircle(g, active);
				drawCircle(g, active);
				break;
			case Type.CircleCross:
				drawCircle(g, active);
				drawCross(g, active);
				break;
			}
		}
	}
}

