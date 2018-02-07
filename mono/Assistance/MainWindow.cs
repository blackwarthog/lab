using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Assistance {
    public class MainWindow : Form {
        static public void Main() { Application.Run(new MainWindow()); }

		Bitmap bitmap = new Bitmap(1, 1);
		Workarea workarea = new Workarea();
		bool dragging = false;
		ActivePoint activePoint;
		Point offset;
		Point cursor;
		
		Track track = null;

		public MainWindow() {
			Paint += onPaint;
			MouseMove += onMouseMove;
			MouseDown += onMouseDown;
			MouseUp += onMouseUp;
			KeyDown += onKeyDown;
            WindowState = FormWindowState.Maximized;
        }

		protected override void OnPaintBackground(PaintEventArgs e) { }

        public void onPaint(Object sender, PaintEventArgs e) {
			if (bitmap.Size != ClientSize)
				bitmap = new Bitmap(ClientSize.Width, ClientSize.Height);
			Graphics g = Graphics.FromImage(bitmap);
			g.Clear(Color.White);
            draw(g);
			g.Flush();
			e.Graphics.DrawImageUnscaled(bitmap, 0, 0);
        }

		public Point windowToWorkarea(Point p) {
			return new Point(p.x - ClientSize.Width/2.0, p.y - ClientSize.Height/2.0);
		}

		public Point workareaToWindow(Point p) {
			return new Point(p.x + ClientSize.Width/2.0, p.y + ClientSize.Height/2.0);
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

		public void onKeyDown(Object sender, KeyEventArgs e) {
			switch(e.KeyCode) {
			case Keys.D1:
				new AssistantVanishingPoint(workarea.document, cursor);
				break;
			case Keys.D2:
				new AssistantGrid(workarea.document, cursor);
				break;
			case Keys.Q:
				new ModifierSnowflake(workarea.document, cursor);
				break;
			case Keys.Delete:
				if (activePoint != null)
					activePoint.owner.remove();
				endDragAndTrack();
				break;
			}
			endDragAndTrack();
			Invalidate();
		}

		public void onMouseDown(Object sender, MouseEventArgs e) {
			cursor = windowToWorkarea(new Point(e.Location.X, e.Location.Y));
			if (e.Button == MouseButtons.Left) {
				activePoint = workarea.findPoint(cursor);
				if (activePoint != null) {
					beginDrag();
				} else {
					beginTrack();
					track.points.Add(cursor);
				}
			}
			Invalidate();
		}

		public void onMouseUp(Object sender, MouseEventArgs e) {
			cursor = windowToWorkarea(new Point(e.X, e.Y));
			if (e.Button == MouseButtons.Left)
				endDragAndTrack();
			if (!dragging && track == null)
				activePoint = workarea.findPoint(cursor);
			Invalidate();
		}

		public void onMouseMove(Object sender, MouseEventArgs e) {
			cursor = windowToWorkarea(new Point(e.Location.X, e.Location.Y));
			if (dragging) {
				activePoint.owner.onMovePoint(activePoint, cursor + offset);
			} else
			if (track != null) {
				track.points.Add(cursor);
			} else {
				activePoint = workarea.findPoint(cursor);
			}
			Invalidate();
        }

        public void draw(Graphics g) {
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
			g.TranslateTransform(ClientSize.Width/2, ClientSize.Height/2);
			workarea.draw(g, activePoint, cursor + offset, track);
        }
    }
}
