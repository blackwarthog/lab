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
		ToolFull toolFull;
		
		bool dragging = false;
		bool painting = false;
		ActivePoint activePoint;

		Point offset;
		Point cursor;
		
		long touchId;
		long ticksStart = 0;
		double timeStart = 0;
		
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
			
			toolFull = new ToolFull(workarea);
			workarea.setTool(toolFull);
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

		private void beginTrack(double timeStart) {
			endDragAndTrack();
			++touchId;
			ticksStart = Timer.ticks();
			this.timeStart = timeStart;
		}

		private void endDragAndTrack() {
			dragging = false;
			offset = new Point();
			if (painting) {
				painting = false;
				workarea.inputManager.finishTracks();
			}
		}
		
		protected override bool OnKeyPressEvent(Gdk.EventKey e) {
			switch(e.Key) {
			case Gdk.Key.Key_1:
				new AssistantVanishingPoint(workarea.document, cursor);
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
				workarea.inputManager.keyEvent(true, e.Key, Timer.ticks());
				break;
			}
			QueueDraw();
			return base.OnKeyPressEvent(e);
		}
		
		protected override bool OnKeyReleaseEvent(Gdk.EventKey e) {
			workarea.inputManager.keyEvent(false, e.Key, Timer.ticks());
			return base.OnKeyReleaseEvent(e);
		}
		
		void addTrackPoint(Gdk.Device device, Point p, double time, double pressure, Point tilt, bool final) {
			if (!painting)
				return;

			Track.Point point = new Track.Point();
			point.position = p;
			point.pressure = pressure;
			point.tilt = tilt;	
			
			long ticks = ticksStart + (long)Math.Round((time - timeStart)*Timer.frequency);
			
			workarea.inputManager.trackEvent(device, touchId, point, final, ticks);
		}

		void addTrackPoint(double x, double y, uint t, Gdk.Device device, double[] axes, bool final) {
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
			addTrackPoint(device, point, time, pressure, tilt, final);
		}
		
		void addTrackPoint(Gdk.EventButton e, bool press)
			{ addTrackPoint(e.X, e.Y, e.Time, e.Device, e.Axes, !press); }
		void addTrackPoint(Gdk.EventMotion e)
			{ addTrackPoint(e.X, e.Y, e.Time, e.Device, e.Axes, false); }

		protected override bool OnButtonPressEvent(Gdk.EventButton e) {
			cursor = windowToWorkarea(new Point(e.X, e.Y));
			workarea.inputManager.buttonEvent(true, e.Device, e.Button, Timer.ticks());
			if (e.Button == 1) {
				timeStart = e.Time;
				activePoint = workarea.findPoint(cursor);
				if (activePoint != null) {
					beginDrag();
				} else {
					beginTrack((double)e.Time*0.001);
					addTrackPoint(e, true);
				}
			}
			QueueDraw();
			return true;
		}

		protected override bool OnButtonReleaseEvent(Gdk.EventButton e) {
			cursor = windowToWorkarea(new Point(e.X, e.Y));
			workarea.inputManager.buttonEvent(false, e.Device, e.Button, Timer.ticks());
			if (e.Button == 1) {
				if (!dragging && painting)
					addTrackPoint(e, false);
				endDragAndTrack();
				if (!dragging && !painting)
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
			if (painting) {
				addTrackPoint(e);
			} else {
				activePoint = workarea.findPoint(cursor);
			}
			QueueDraw();
			return true;
		}

        public void draw(Cairo.Context context) {
        	context.Translate(surface.Width/2, surface.Height/2);
			workarea.draw(context, activePoint);
        }
    }
}
