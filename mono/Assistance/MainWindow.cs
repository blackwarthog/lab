using System;
using System.Collections.Generic;

namespace Assistance {
    public class MainWindow : Gtk.Window {
        static public void Main() {
        	Gtk.Application.Init();
        	MainWindow win = new MainWindow();
			win.Show();
			win.Maximize();
        	Gtk.Application.Run();
        }

		Cairo.ImageSurface surface = new Cairo.ImageSurface(Cairo.Format.ARGB32, 1, 1);

		Workarea workarea = new Workarea();
		bool dragging = false;
		ActivePoint activePoint;

		uint timeStart = 0;
		long ticksStart = 0;
		long lastTicks = 0;
		Point offset;
		Point cursor;
		
		KeyState<Gdk.Key> keyState = new KeyState<Gdk.Key>();
		KeyState<uint> buttonState = new KeyState<uint>();
		
		Track track = null;

		public MainWindow(): base(Gtk.WindowType.Toplevel) {
			foreach(Gdk.Device device in Display.ListDevices()) {
				if (device.Name.Contains("tylus")) {
					device.SetMode(Gdk.InputMode.Screen);
					break;
				}
			}
		
			Events = Gdk.EventMask.KeyPressMask
				   | Gdk.EventMask.KeyReleaseMask
			       | Gdk.EventMask.ButtonPressMask
			       | Gdk.EventMask.ButtonReleaseMask
			       | Gdk.EventMask.ButtonMotionMask
			       | Gdk.EventMask.PointerMotionMask;
			ExtensionEvents = Gdk.ExtensionMode.All;
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

		private void beginTrack(Gdk.Device device) {
			endDragAndTrack();
			track = new Track(Track.getTouchId(), device);
			ticksStart = Timer.ticks();
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
				endDragAndTrack();
				break;
			case Gdk.Key.Key_2:
				new AssistantGrid(workarea.document, cursor);
				endDragAndTrack();
				break;
			case Gdk.Key.Q:
			case Gdk.Key.q:
				new ModifierSnowflake(workarea.document, cursor);
				endDragAndTrack();
				break;
			case Gdk.Key.I:
			case Gdk.Key.i:
				Gtk.InputDialog dialog = new Gtk.InputDialog();
				dialog.CloseButton.Clicked += (object sender, EventArgs args) => { dialog.Destroy(); };
				dialog.Show();
				break;
			case Gdk.Key.Delete:
				if (activePoint != null)
					activePoint.owner.remove();
				endDragAndTrack();
				break;
			default:
				keyState = keyState.press(e.Key, Timer.ticks());
				retryTrackPoint();
				break;
			}
			QueueDraw();
			return base.OnKeyPressEvent(e);
		}
		
		protected override bool OnKeyReleaseEvent(Gdk.EventKey e) {
			keyState = keyState.release(e.Key, Timer.ticks());
			retryTrackPoint();
			return base.OnKeyReleaseEvent(e);
		}
		
		void addTrackPoint(Point p, double time, double pressure, Point tilt) {
			if (track == null)
				return;

			TrackPoint point = new TrackPoint();
			point.point = p;
			point.time = time;
			point.pressure = pressure;
			point.tilt = tilt;
			
			long ticks = Timer.ticks();
			if (track.points.Count > 0) {
				double t = track.points[ track.points.Count-1 ].time;
				if (point.time - t < Geometry.precision)
					point.time = t + (ticks - lastTicks)*Timer.step;
			}
			lastTicks = ticks;

			point.keyState.state = keyState;
			point.keyState.ticks = ticksStart;
			point.keyState.timeOffset = point.time;
			
			point.buttonState.state = buttonState;
			point.buttonState.ticks = ticksStart;
			point.buttonState.timeOffset = point.time;
			
			track.points.Add(point);
		}

		void addTrackPoint(double x, double y, uint t, Gdk.Device device, double[] axes) {
			Point point = windowToWorkarea(new Point(x, y));
			double time = (double)(t - timeStart)*0.001;
			double pressure = 0.5;
			Point tilt = new Point(0.0, 0.0);
			
			if (device != null && axes != null) {
				double v;
				if (device.GetAxis(axes, Gdk.AxisUse.Pressure, out v))
					pressure = v;
				if (device.GetAxis(axes, Gdk.AxisUse.Xtilt, out v))
					tilt.x = v;
				if (device.GetAxis(axes, Gdk.AxisUse.Ytilt, out v))
					tilt.y = v;
			}
			
			long ticks = Timer.ticks();
			long dticks = Math.Max(1, ticks - lastTicks);
			lastTicks = ticks;
			if (track.points.Count > 0) {
				double prevTime = track.points[ track.points.Count-1 ].time;
				if (time - prevTime < Geometry.precision)
					time = prevTime + dticks*Timer.step;
			}

			addTrackPoint(point, time, pressure, tilt);
		}
		
		void addTrackPoint(Gdk.EventButton e) {
			addTrackPoint(e.X, e.Y, e.Time, e.Device, e.Axes);
		}

		void addTrackPoint(Gdk.EventMotion e) {
			addTrackPoint(e.X, e.Y, e.Time, e.Device, e.Axes);
		}

		void retryTrackPoint() {
			if (track == null || track.points.Count < 1) return;
			TrackPoint last = track.points[track.points.Count-1];
			addTrackPoint(last.point, last.time, last.pressure, last.tilt);
		}

		protected override bool OnButtonPressEvent(Gdk.EventButton e) {
			cursor = windowToWorkarea(new Point(e.X, e.Y));
			buttonState = buttonState.press(e.Button, Timer.ticks());
			retryTrackPoint();
			if (e.Button == 1) {
				timeStart = e.Time;
				activePoint = workarea.findPoint(cursor);
				if (activePoint != null) {
					beginDrag();
				} else {
					beginTrack(e.Device);
					addTrackPoint(e);
				}
			}
			QueueDraw();
			return true;
		}

		protected override bool OnButtonReleaseEvent(Gdk.EventButton e) {
			cursor = windowToWorkarea(new Point(e.X, e.Y));
			buttonState = buttonState.release(e.Button, Timer.ticks());
			retryTrackPoint();
			if (e.Button == 1) {
				if (!dragging && track != null)
					addTrackPoint(e);
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
				if (e.IsHint) Gdk.Display.Default.Beep();
				addTrackPoint(e);
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
