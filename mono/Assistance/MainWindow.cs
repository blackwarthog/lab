using System;
using System.Collections.Generic;

namespace Assistance {
    public class MainWindow : Gtk.Window {
        static public void Main() {
        	Gtk.Application.Init();
        	MainWindow win = new MainWindow();
        	win.Show();
        	Gtk.Application.Run();
        }

		Cairo.ImageSurface surface = new Cairo.ImageSurface(Cairo.Format.ARGB32, 1, 1);

		Workarea workarea = new Workarea();
		bool dragging = false;
		ActivePoint activePoint;

		uint timeStart = 0;
		Point offset;
		Point cursor;
		
		Track track = null;

		public MainWindow(): base(Gtk.WindowType.Toplevel) {
			this.Events = Gdk.EventMask.KeyPressMask
						| Gdk.EventMask.KeyReleaseMask
			            | Gdk.EventMask.ButtonPressMask
			            | Gdk.EventMask.ButtonReleaseMask
			            | Gdk.EventMask.ButtonMotionMask
			            | Gdk.EventMask.PointerMotionMask;
			Maximize();
        }
        
        protected override bool OnDeleteEvent(Gdk.Event e) {
			Gtk.Application.Quit();
			return true;
		}

		protected override void OnSizeAllocated(Gdk.Rectangle allocation) {
			if ( surface.Width != allocation.Width
	          || surface.Height != allocation.Height )
	        {
	        	surface.Dispose();
	        	surface = new Cairo.ImageSurface(Cairo.Format.ARGB32, allocation.Width, allocation.Height);
	       	}
			base.OnSizeAllocated(allocation);
		}

		protected override bool OnExposeEvent(Gdk.EventExpose e) {
			Cairo.Context context = new Cairo.Context(surface);
        	context.Antialias = Cairo.Antialias.Gray;
        	
        	context.Save();
        	context.SetSourceRGBA(1.0, 1.0, 1.0, 1.0);
        	context.Rectangle(0, 0, surface.Width, surface.Height);
        	context.Fill();
        	context.Restore();
            
            draw(context);
            context.Dispose();
            
            context = Gdk.CairoHelper.Create(GdkWindow);
            context.SetSource(surface);
            context.Paint();
            context.Dispose();

			return true;
		}

		public Point windowToWorkarea(Point p) {
			return new Point(p.x - surface.Width/2.0, p.y - surface.Height/2.0);
		}

		public Point workareaToWindow(Point p) {
			return new Point(p.x + surface.Width/2.0, p.y + surface.Height/2.0);
		}

		private void beginDrag() {
			endDragAndTrack();
			dragging = true;
			offset = activePoint.position - cursor;
			activePoint.bringToFront();
		}

		private void beginTrack() {
			endDragAndTrack();
			track = new Track();
		}

		private void endDragAndTrack() {
			dragging = false;
			offset = new Point();
			
			if (track != null)
				workarea.paintTrack(track);
			track = null;
		}
		
		protected override bool OnKeyPressEvent(Gdk.EventKey e) {
			switch(e.Key) {
			case Gdk.Key.Key_1:
				new AssistantVanishingPoint(workarea.document, cursor);
				break;
			case Gdk.Key.Key_2:
				new AssistantGrid(workarea.document, cursor);
				break;
			case Gdk.Key.Q:
			case Gdk.Key.q:
				new ModifierSnowflake(workarea.document, cursor);
				break;
			case Gdk.Key.Delete:
				if (activePoint != null)
					activePoint.owner.remove();
				endDragAndTrack();
				break;
			}
			endDragAndTrack();
			QueueDraw();
			return base.OnKeyPressEvent(e);
		}
		
		TrackPoint makeTrackPoint(double x, double y, uint time, Gdk.Device device, double[] axes) {
			TrackPoint point = new TrackPoint(
				windowToWorkarea(new Point(x, y)),
				(double)(time - timeStart)*0.001 );
			if (device != null && axes != null) {
				double v;
				if (device.GetAxis(axes, Gdk.AxisUse.Pressure, out v))
					point.pressure = v;
				if (device.GetAxis(axes, Gdk.AxisUse.Xtilt, out v))
					point.tilt.x = v;
				if (device.GetAxis(axes, Gdk.AxisUse.Ytilt, out v))
					point.tilt.y = v;
			}
			return point;
		}

		TrackPoint makeTrackPoint(Gdk.EventButton e) {
			return makeTrackPoint(e.X, e.Y, e.Time, e.Device, e.Axes);
		}

		TrackPoint makeTrackPoint(Gdk.EventMotion e) {
			return makeTrackPoint(e.X, e.Y, e.Time, e.Device, e.Axes);
		}

		protected override bool OnButtonPressEvent(Gdk.EventButton e) {
			cursor = windowToWorkarea(new Point(e.X, e.Y));
			if (e.Button == 1) {
				timeStart = e.Time;
				activePoint = workarea.findPoint(cursor);
				if (activePoint != null) {
					beginDrag();
				} else {
					beginTrack();
					track.points.Add(makeTrackPoint(e));
				}
			}
			QueueDraw();
			return true;
		}

		protected override bool OnButtonReleaseEvent(Gdk.EventButton e) {
			cursor = windowToWorkarea(new Point(e.X, e.Y));
			if (e.Button == 1) {
				if (!dragging && track != null)
					track.points.Add(makeTrackPoint(e));
				endDragAndTrack();
				if (!dragging && track == null)
					activePoint = workarea.findPoint(cursor);
			}
			QueueDraw();
			return true;
		}

		protected override bool OnMotionNotifyEvent(Gdk.EventMotion e) {
			cursor = windowToWorkarea(new Point(e.X, e.Y));
			if (dragging) {
				activePoint.owner.onMovePoint(activePoint, cursor + offset);
			} else
			if (track != null) {
				track.points.Add(makeTrackPoint(e));
			} else {
				activePoint = workarea.findPoint(cursor);
			}
			QueueDraw();
			return true;
		}

        public void draw(Cairo.Context context) {
        	context.Translate(surface.Width/2, surface.Height/2);
			workarea.draw(context, activePoint, cursor + offset, track);
        }
    }
}
