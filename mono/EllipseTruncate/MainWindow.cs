using System;
using System.Collections.Generic;

namespace EllipseTruncate {
	public class ActivePoint {
		public Point point;
		public ActivePoint(double x, double y) {
			point = new Point(x, y);
		}
	}

    public class MainWindow : Gtk.Window {
        static public void Main() {
        	Gtk.Application.Init();
        	MainWindow win = new MainWindow();
			win.Show();
			win.Maximize();
        	Gtk.Application.Run();
        }
		
		Point cursor;
		Point offset;
		ActivePoint point;
		ActiveAngleRange rangeAdd;
		ActiveAngleRange rangeXor;
		ActiveAngleRange rangeSub;
		List<ActivePoint> points = new List<ActivePoint>();
		List<ActiveAngleRange> ranges = new List<ActiveAngleRange>();
		
		ActivePoint ellipse0 = new ActivePoint(500.0, 500.0),
		            ellipse1 = new ActivePoint(600.0, 500.0),
		            ellipse2 = new ActivePoint(500.0, 450.0);
		ActivePoint bounds0 = new ActivePoint(450.0, 500.0),
		            bounds1 = new ActivePoint(500.0, 700.0),
		            bounds2 = new ActivePoint(600.0, 550.0);
		ActiveAngleRange rangeA = new ActiveAngleRange(new Point(150.0, 150.0), 50.0);
		ActiveAngleRange rangeB = new ActiveAngleRange(new Point(400.0, 150.0), 50.0);
		
		public MainWindow(): base(Gtk.WindowType.Toplevel) {
			Events = Gdk.EventMask.KeyPressMask
				   | Gdk.EventMask.KeyReleaseMask
			       | Gdk.EventMask.ButtonPressMask
			       | Gdk.EventMask.ButtonReleaseMask
			       | Gdk.EventMask.ButtonMotionMask
			       | Gdk.EventMask.PointerMotionMask;
			ExtensionEvents = Gdk.ExtensionMode.All;
			rangeA.b = rangeB.a;
			rangeB.b = rangeA.a;
			points.AddRange(new ActivePoint[] {ellipse0, ellipse1, ellipse2, bounds0, bounds1, bounds2});
			ranges.AddRange(new ActiveAngleRange[] {rangeA, rangeB});
        }
        
        private bool refreshOnIdle()
        	{ QueueDraw(); return false; }
        private void Refresh() {
        	QueueDraw(); //GLib.Idle.Add(refreshOnIdle);
        }
        
        protected override bool OnDeleteEvent(Gdk.Event e) {
			Gtk.Application.Quit();
			return false;
		}

		protected override bool OnExposeEvent(Gdk.EventExpose e) {
            Cairo.Context context = Gdk.CairoHelper.Create(e.Window);

        	context.Save();
        	context.SetSourceRGBA(1.0, 1.0, 1.0, 1.0);
        	context.Rectangle(0, 0, Allocation.Width, Allocation.Height);
        	context.Fill();
        	context.Restore();
        	context.Save();
			context.Antialias = Cairo.Antialias.Gray;

			foreach(ActivePoint p in points) {
        		context.Save();
				context.SetSourceRGBA(0.0, point == p ? 1.0 : 0.0, 1.0, 1.0);
				context.Arc(p.point.x, p.point.y, 5.0, 0, 2.0*Math.PI);
				context.Fill();
        		context.Restore();
			}

			{ // draw bounds
				Point p0 = bounds0.point, p1 = bounds1.point, p2 = bounds2.point;
        		context.Save();
				context.SetSourceRGBA(0.5, 0.5, 0.5, 0.2);
				context.LineWidth = 2.0;
				context.MoveTo(p0.x, p0.y);
				context.LineTo(p1.x, p1.y);
				context.LineTo(p1.x + p2.x - p0.x, p1.y + p2.y - p0.y);
				context.LineTo(p2.x, p2.y);
				context.ClosePath();
				context.Stroke();
        		context.Restore();
			}
        	
			// draw ellipse
			Ellipse ellipse = new Ellipse(ellipse0.point, ellipse1.point, ellipse2.point);
			ellipse.drawFull(context);
			ellipse.drawTruncated(context, bounds0.point, bounds1.point, bounds2.point);
			
			// draw concentric grid
			ConcentricGrid cg = new ConcentricGrid(
				ellipse, 20.0, bounds0.point, bounds1.point, bounds2.point );
			cg.draw(context);
			
			// draw ranges
			foreach(ActiveAngleRange rl in ranges) {
				rl.draw(context);
				if ((cursor - rl.point).len() < 2.0*rl.radius || !rl.current.isEmpty()) {
					context.Save();
					context.SetSourceRGBA(0.0, 0.0, 0.0, 0.5);
					context.LineWidth = 1.0;
					context.MoveTo(rl.point.x, rl.point.y);
					context.LineTo(cursor.x, cursor.y);
					context.Stroke();
					context.Restore();
				}					
			}
			
        	context.Restore();
            context.Dispose();
			return false;
		}
		
		private void releaseButton() {
			if (rangeAdd != null) rangeAdd.add();
			if (rangeXor != null) rangeXor.xor();
			if (rangeSub != null) rangeSub.subtract();
			point = null;
			rangeAdd = null;
			rangeXor = null;
			rangeSub = null;
		}

		protected override bool OnButtonPressEvent(Gdk.EventButton e) {
			cursor = new Point(e.X, e.Y);
			releaseButton();

			ActivePoint ap = null;
			Point o = new Point();
		    foreach(ActivePoint p in points)
				if ((p.point - cursor).len() <= 5.0)
					{ o = p.point - cursor; ap = p; }
			ActiveAngleRange ar = null;
			uint a0 = 0;
		    foreach(ActiveAngleRange r in ranges)
				if ((r.point - cursor).len() < 2.0*r.radius)
					{ ar = r; a0 = AngleRange.toUIntDiscrete((cursor - r.point).atan()); }

			if (e.Button == 1) {
				if (ap != null) {
					point = ap;
					offset = o;
				} else
				if (ar != null) {
					ar.current = new AngleRange.Entry();
					ar.current.a0 = a0;
					rangeAdd = ar;
				}
			} else
			if (e.Button == 2) {
				if (ar != null) {
					ar.current = new AngleRange.Entry();
					ar.current.a0 = a0;
					rangeXor = ar;
				}
			} else
			if (e.Button == 3) {
				if (ar != null) {
					ar.current = new AngleRange.Entry();
					ar.current.a0 = a0;
					rangeSub = ar;
				}
			}
			
			Refresh();
			return false;
		}

		protected override bool OnButtonReleaseEvent(Gdk.EventButton e) {
			cursor = new Point(e.X, e.Y);
			releaseButton();
			Refresh();
			return false;
		}

		protected override bool OnMotionNotifyEvent(Gdk.EventMotion e) {
			cursor = new Point(e.X, e.Y);
			if (point != null) point.point = cursor + offset;
			if (rangeAdd != null)
				rangeAdd.current.a1 = AngleRange.toUIntDiscrete((cursor - rangeAdd.point).atan());
			if (rangeXor != null)
				rangeXor.current.a1 = AngleRange.toUIntDiscrete((cursor - rangeXor.point).atan());
			if (rangeSub != null)
				rangeSub.current.a1 = AngleRange.toUIntDiscrete((cursor - rangeSub.point).atan());
			Refresh();
			return false;
		}
    }
}
