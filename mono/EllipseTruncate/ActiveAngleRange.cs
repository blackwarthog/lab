using System;

namespace EllipseTruncate {
	public class ActiveAngleRange {
		public double width = 0.1;
	
		public Point point;
		public double radius;
		public AngleRange.Entry current;
		public AngleRange a = new AngleRange();
		public AngleRange b = new AngleRange();
		
		public ActiveAngleRange(Point point, double radius) {
			this.point = point;
			this.radius = radius;
		}

		public void add() {
			a.add(current);
			current = new AngleRange.Entry();
		}

		public void xor() {
			a.xor(current);
			current = new AngleRange.Entry();
		}

		public void subtract() {
			a.subtract(current);
			current = new AngleRange.Entry();
		}
		
		public void scale(Cairo.Context context, double level = 1.0) {
			double k = Math.Pow(1.0 + 1.5*width, level);
			context.Scale(k, k);
		}
		
		public void draw(Cairo.Context context, uint a0, uint a1, double level = 0.0) {
			if (a0 == a1) return;
			
			context.Save();
			scale(context, level);
			context.LineWidth = width;
			double aa0 = AngleRange.toDouble(a0);
			double aa1 = AngleRange.toDouble(a1);
			if (a1 < a0) aa1 += AngleRange.period;
			if (aa1 < aa0) return;
			
			context.Arc(0.0, 0.0, 1.0, aa0, aa1);
			context.Stroke();
			context.Restore();
		}
		
		public void draw(Cairo.Context context, AngleRange r, double level = 0.0) {
			context.Save();
			scale(context, level);
			if (r.isEmpty()) {
				context.LineWidth = 0.25*width;
				context.SetDash(new double[] { 0.1, 0.02 }, 0.0);
				context.Arc(0.0, 0.0, 1.0, 0.0, 2.0*Math.PI);
				context.Stroke();
			} else
			if (r.isFull()) {
				context.LineWidth = width;
				context.Arc(0.0, 0.0, 1.0, 0.0, 2.0*Math.PI);
				context.Stroke();
				context.LineWidth = 0.25*width;
				context.SetDash(new double[] { 0.1, 0.03 }, 0.0);
				context.Arc(0.0, 0.0, 1.0, 0.0, 2.0*Math.PI);
				context.Stroke();
			} else {
				context.LineWidth = 0.25*width;
				context.Arc(0.0, 0.0, 1.0, 0.0, 2.0*Math.PI);
				context.Stroke();
				bool f = r.flip;
				uint prev = r.angles[r.angles.Count - 1];
				foreach(uint a in r.angles) {
					if (f) draw(context, prev, a);
					prev = a; f = !f;
				}
			}

			if (!r.check()) {
				context.LineWidth = width;
				context.SetDash(new double[] { 0.1, 0.02 }, 0.0);
				context.Arc(0.0, 0.0, 1.0, 0.0, 2.0*Math.PI);
				context.Stroke();
			}

			context.Restore();
		}
		
		public void draw(Cairo.Context context) {
			context.Save();
			context.Translate(point.x, point.y);
			context.Scale(radius, radius);
			
			AngleRange x = new AngleRange();

			// back circle
			context.SetSourceRGBA(1.0, 0.0, 0.0, 0.1);
			context.Arc(0.0, 0.0, 1.0, 0.0, 2.0*Math.PI);
			context.Fill();

			// a
			context.SetSourceRGBA(0.0, 0.0, 0.0, 0.5);
			draw(context, a);

			// b
			context.SetSourceRGBA(0.0, 0.0, 1.0, 0.5);
			draw(context, b, 1.0);

			// current
			context.SetSourceRGBA(0.0, 0.0, 1.0, 0.5);
			if (!current.isEmpty())
				draw(context, current.a0, current.a1);

			// !a
			context.SetSourceRGBA(0.5, 0.5, 0.5, 0.5);
			x.set(a); x.invert();	
			draw(context, x, -1.0);
			
			scale(context);
			
			// a xor b
			scale(context);
			context.SetSourceRGBA(0.0, 0.5, 0.0, 0.5);
			x.set(a);
			x.xor(b);
			draw(context, x);

			// a | b
			scale(context);
			context.SetSourceRGBA(0.0, 0.5, 0.0, 0.5);
			x.set(a);
			x.add(b);
			draw(context, x);
			
			// a & ~b
			scale(context);
			context.SetSourceRGBA(0.0, 0.5, 0.0, 0.5);
			x.set(a);
			x.subtract(b);
			draw(context, x);

			// a & b
			scale(context);
			context.SetSourceRGBA(0.0, 0.5, 0.0, 0.5);
			x.set(a);
			x.intersect(b);
			draw(context, x);
			
			context.Restore();
		}
	}
}

