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
		
		Point offset;
		ActivePoint point;
		List<ActivePoint> points = new List<ActivePoint>();
		
		ActivePoint ellipse0 = new ActivePoint(500.0, 500.0),
		            ellipse1 = new ActivePoint(600.0, 500.0),
		            ellipse2 = new ActivePoint(500.0, 450.0);
		ActivePoint bounds0 = new ActivePoint(450.0, 500.0),
		            bounds1 = new ActivePoint(500.0, 700.0),
		            bounds2 = new ActivePoint(600.0, 550.0);
		
		public MainWindow(): base(Gtk.WindowType.Toplevel) {
			Events = Gdk.EventMask.KeyPressMask
				   | Gdk.EventMask.KeyReleaseMask
			       | Gdk.EventMask.ButtonPressMask
			       | Gdk.EventMask.ButtonReleaseMask
			       | Gdk.EventMask.ButtonMotionMask
			       | Gdk.EventMask.PointerMotionMask;
			ExtensionEvents = Gdk.ExtensionMode.All;
			points.AddRange(new ActivePoint[] {ellipse0, ellipse1, ellipse2, bounds0, bounds1, bounds2});
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
			
        	context.Restore();
            context.Dispose();
			return false;
		}

		protected override bool OnButtonPressEvent(Gdk.EventButton e) {
			if (e.Button == 1) {
				Point cursor = new Point(e.X, e.Y);
				point = null;
			    foreach(ActivePoint p in points)
					if ((p.point - cursor).len() <= 5.0)
						{ offset = p.point - cursor; point = p; }
			}
			Refresh();
			return false;
		}

		protected override bool OnButtonReleaseEvent(Gdk.EventButton e) {
			if (e.Button == 1) point = null;
			Refresh();
			return false;
		}

		protected override bool OnMotionNotifyEvent(Gdk.EventMotion e) {
			if (point != null) {
				Point cursor = new Point(e.X, e.Y);
				point.point = cursor + offset;
			}
			Refresh();
			return false;
		}
    }
}
