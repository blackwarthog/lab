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

		public class Owner {
			public readonly Workarea workarea;
			public readonly List<ActivePoint> points = new List<ActivePoint>();
			
			public Owner(Workarea workarea) { this.workarea = workarea; }
			public virtual void onMovePoint(ActivePoint point, Point position) { point.position = position; }
			public virtual void bringToFront() { }
			public virtual void remove() { foreach(ActivePoint point in points) workarea.points.Remove(point); }
		}

		public static readonly double radius = 10.0;
		public static readonly double crossSize = 1.2*radius;
		public static readonly Pen pen = Pens.Gray;
		public static readonly Brush brush = Brushes.LightGray;
		public static readonly Pen penActive = Pens.Blue;
		public static readonly Brush brushActive = Brushes.LightBlue;

		public readonly Workarea workarea;
		public readonly Owner owner;
		public readonly Type type;
		public Point position;

		public ActivePoint(Owner owner, Type type, Point position = new Point()) {
			this.workarea = owner.workarea;
			this.owner = owner;
			this.type = type;
			this.position = position;
			workarea.points.Add(this);
			owner.points.Add(this);
		}

		public bool isInside(Point p) {
			return (position - p).lenSqr() <= radius*radius;
		}

		public void bringToFront() {
			owner.bringToFront();
			owner.points.Remove(this);
			owner.points.Add(this);
			workarea.points.Remove(this);
			workarea.points.Add(this);
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

