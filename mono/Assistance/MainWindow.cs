using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Assistance {
    public class MainWindow : Form {
        static public void Main() { Application.Run(new MainWindow()); }

		Bitmap bitmap = new Bitmap(1, 1);
		Canvas canvas = new Canvas();
		bool dragging = false;
		ActivePoint activePoint;
		Point offset;
		Point cursor;

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
			e.Graphics.DrawImageUnscaled(bitmap, new Rectangle(0, 0, ClientSize.Width, ClientSize.Height));
        }

		public Point windowToCanvas(Point p) {
			return new Point(p.x - ClientSize.Width/2.0, p.y - ClientSize.Height/2.0);
		}

		public Point canvasToWindow(Point p) {
			return new Point(p.x + ClientSize.Width/2.0, p.y + ClientSize.Height/2.0);
		}

		private void beginDrag() {
			dragging = true;
			offset = activePoint.position - cursor;
			activePoint.bringToFront();
		}

		private void endDrag() {
			dragging = false;
			offset = new Point();
		}

		public void onKeyDown(Object sender, KeyEventArgs e) {
			switch(e.KeyCode) {
			case Keys.D1:
				new VanishingPoint(canvas, cursor);
				break;
			case Keys.D2:
				new Grid(canvas, cursor);
				break;
			case Keys.Delete:
				if (activePoint != null)
					activePoint.assistant.remove();
				endDrag();
				break;
			}
			endDrag();
			Invalidate();
		}

		public void onMouseDown(Object sender, MouseEventArgs e) {
			cursor = windowToCanvas(new Point(e.Location.X, e.Location.Y));
			if (e.Button == MouseButtons.Left) {
				activePoint = canvas.findPoint(cursor);
				if (activePoint != null)
					beginDrag();
			}
			Invalidate();
		}

		public void onMouseUp(Object sender, MouseEventArgs e) {
			cursor = windowToCanvas(new Point(e.X, e.Y));
			if (e.Button == MouseButtons.Left)
				endDrag();
			if (!dragging)
				activePoint = canvas.findPoint(cursor);
			Invalidate();
		}

		public void onMouseMove(Object sender, MouseEventArgs e) {
			cursor = windowToCanvas(new Point(e.Location.X, e.Location.Y));
			if (dragging) {
				activePoint.assistant.onMovePoint(activePoint, cursor + offset);
			} else {
				activePoint = canvas.findPoint(cursor);
			}
			Invalidate();
        }

        public void draw(Graphics g) {
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
			g.TranslateTransform(0.5f*ClientSize.Width, 0.5f*ClientSize.Height);
			canvas.draw(g, activePoint, cursor + offset);
        }
    }
}

