using System;
using System.Collections.Generic;
using Assistance.Drawing;

namespace Assistance {
	public class ActivePoint {
		public enum Type {
			Circle,
			CircleFill,
			CircleCross,
		};

		public class Owner {
			public readonly Document document;
			public readonly List<ActivePoint> points = new List<ActivePoint>();
			
			public Owner(Document document) { this.document = document; }
			public virtual void onMovePoint(ActivePoint point, Point position) { point.position = position; }
			public virtual void bringToFront() { }
			public virtual void remove() { foreach(ActivePoint point in points) document.points.Remove(point); }
		}

		public static readonly double radius = 10.0;
		public static readonly double crossSize = 1.2*radius;
		public static readonly Pen pen = new Pen("Gray");
		public static readonly Brush brush = new Brush("Light Gray");
		public static readonly Pen penActive = new Pen("Blue");
		public static readonly Brush brushActive = new Brush("Light Blue");

		public readonly Document document;
		public readonly Owner owner;
		public readonly Type type;
		public Point position;

		public ActivePoint(Owner owner, Type type, Point position = new Point()) {
			this.document = owner.document;
			this.owner = owner;
			this.type = type;
			this.position = position;
			document.points.Add(this);
			owner.points.Add(this);
		}

		public bool isInside(Point p) {
			return (position - p).lenSqr() <= radius*radius;
		}

		public void bringToFront() {
			owner.bringToFront();
			owner.points.Remove(this);
			owner.points.Add(this);
			document.points.Remove(this);
			document.points.Add(this);
		}

		private Pen getPen(bool active) {
			return active ? penActive : pen;
		}

		private Brush getBrush(bool active) {
			return active ? brushActive : brush;
		}

		private void drawCircle(Cairo.Context context, bool active) {
			context.Save();
			getPen(active).apply(context);
			context.Arc(position.x, position.y, radius, 0.0, 2.0*Math.PI);
			context.Stroke();
			context.Restore();
		}

		private void fillCircle(Cairo.Context context, bool active) {
			context.Save();
			getBrush(active).apply(context);
			context.Arc(position.x, position.y, radius, 0.0, 2.0*Math.PI);
			context.Fill();
			context.Restore();
		}

		private void drawCross(Cairo.Context context, bool active) {
			context.Save();
			getPen(active).apply(context);
			context.MoveTo(position.x - crossSize, position.y);
			context.LineTo(position.x + crossSize, position.y);
			context.Stroke();
			context.MoveTo(position.x, position.y - crossSize);
			context.LineTo(position.x, position.y + crossSize);
			context.Stroke();
			context.Restore();
		}

		public void draw(Cairo.Context context, bool active = false) {
			switch(type) {
			case Type.Circle:
				drawCircle(context, active);
				break;
			case Type.CircleFill:
				fillCircle(context, active);
				drawCircle(context, active);
				break;
			case Type.CircleCross:
				drawCircle(context, active);
				drawCross(context, active);
				break;
			}
		}
	}
}

