using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;

namespace Assistance {
	public class ActivePoint {
		public enum Mode {
			Common = 0,
			Active = 1
		}

		public enum Type {
			Circle,
			CircleFill,
			CircleCross,
		};

		public static readonly double radius = 10.0;
		public static readonly double crossSize = 1.2*radius;
		public static readonly Pen[] pens = new Pen[] { Pens.Gray, Pens.Blue };
		public static readonly Brush[] brushes = new Brush[] { Brushes.LightGray, Brushes.LightBlue };

		public readonly Canvas canvas;
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

		private void drawCircle(Graphics g, Mode mode) {
			g.DrawEllipse(pens[(int)mode], (float)(position.x - radius), (float)(position.y - radius), (float)(2.0*radius), (float)(2.0*radius));
		}

		private void fillCircle(Graphics g, Mode mode) {
			g.FillEllipse(brushes[(int)mode], (float)(position.x - radius), (float)(position.y - radius), (float)(2.0*radius), (float)(2.0*radius));
		}

		private void drawCross(Graphics g, Mode mode) {
			g.DrawLine(pens[(int)mode], (float)(position.x - crossSize), (float)position.y, (float)(position.x + crossSize), (float)position.y);
			g.DrawLine(pens[(int)mode], (float)position.x, (float)(position.y - crossSize), (float)position.x, (float)(position.y + crossSize));
		}

		public void draw(Graphics g, Mode mode) {
			switch(type) {
			case Type.Circle:
				drawCircle(g, mode);
				break;
			case Type.CircleFill:
				fillCircle(g, mode);
				drawCircle(g, mode);
				break;
			case Type.CircleCross:
				drawCircle(g, mode);
				drawCross(g, mode);
				break;
			}
		}
	}
}

