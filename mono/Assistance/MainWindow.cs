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
		
		Workarea workarea = new Workarea();
		ToolFull toolFull;
		
		bool dragging = false;
		bool painting = false;
		ActivePoint activePoint;

		Point offset;
		Point cursor;
		List<Point> hovers = new List<Point>();
		
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
        
        private bool refreshOnIdle()
        	{ QueueDraw(); return false; }
        private void Refresh()
        	{ GLib.Idle.Add(refreshOnIdle); }
        
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
            draw(context);
        	context.Restore();
            
            context.Dispose();
            
			return false;
		}

		public Point windowToWorkarea(Point p) {
			return new Point(p.x - Allocation.Width/2.0, p.y - Allocation.Height/2.0);
		}

		public Point workareaToWindow(Point p) {
			return new Point(p.x + Allocation.Width/2.0, p.y + Allocation.Height/2.0);
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
			painting = true;
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
			case Gdk.Key.W:
			case Gdk.Key.w:
				new ModifierSpiro(workarea.document, cursor);
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
			Refresh();
			return base.OnKeyPressEvent(e);
		}
		
		protected override bool OnKeyReleaseEvent(Gdk.EventKey e) {
			workarea.inputManager.keyEvent(false, e.Key, Timer.ticks());
			return base.OnKeyReleaseEvent(e);
		}
		
		void addTrackPoint(Gdk.Device device, Point p, double time, double pressure, Point tilt, bool final) {
			if (!painting) return;
			long ticks = ticksStart + (long)Math.Round((time - timeStart)*Timer.frequency);
			workarea.inputManager.trackEvent(device, touchId, p, pressure, tilt, final, ticks);
		}

		void addTrackPoint(double x, double y, uint t, Gdk.Device device, double[] axes, bool final) {
			Point point = windowToWorkarea(new Point(x, y));
			double time = (double)t*0.001;
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
				activePoint = workarea.findPoint(cursor);
				if (activePoint != null) {
					beginDrag();
				} else {
					beginTrack((double)e.Time*0.001);
					addTrackPoint(e, true);
				}
			}
			Refresh();
			return false;
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
			Refresh();
			return false;
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
			Refresh();
			return false;
		}

        public void draw(Cairo.Context context) {
        	context.Translate(Allocation.Width/2, Allocation.Height/2);
        	hovers.Clear();
        	if (!painting) hovers.Add(cursor);
			workarea.draw(context, hovers, activePoint);
        }
    }
}
