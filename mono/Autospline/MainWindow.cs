using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Autospline {
	public struct Segment {
		public double previous;
		public double current;
		public double next;
		public double nextnext;

		public static double get(double s, double p0, double pm, double p1) {
			return p0*(1.0-s)*(1.0-s) + 2.0*pm*(1.0-s)*s + p1*s*s;
		}

		public static double get(double s, double p0, double p1, double t0, double t1) {
			double h0 =  2.0*s*s*s - 3.0*s*s + 1.0;
			double h1 = -2.0*s*s*s + 3.0*s*s;
			double h2 =      s*s*s - 2.0*s*s + s;
			double h3 =      s*s*s -     s*s;
			double p = h0*p0 + h1*p1 + h2*t0 + h3*t1;
			return p;
		}

		public double get(double s) {
			return get(s, current, next, current - previous, next - current);
		}

		public double getCool(double s) {
			return get(s, current, next, 0.5*(next - previous), 0.5*(nextnext - current));
		}

		public double getCool2(double s) {
			return get(s, 0.5*(previous + current), current, 0.5*(current + next));
		}
	}

	public struct Segment2d {
		public Segment x;
		public Segment y;
	}

	public class MainWindow : Form {
        static public void Main() { Application.Run(new MainWindow()); }

		bool pressed = false;
		List<Segment2d> segments = new List<Segment2d>();
		Bitmap buffer;

		void newCurve(double x, double y) {
			Segment2d segment;
			segment.x.previous = segment.x.current = segment.x.next = segment.x.nextnext = x;
			segment.y.previous = segment.y.current = segment.y.next = segment.y.nextnext = y;
			segments.Add(segment);
		}

		void addPoint(double x, double y) {
			Segment2d previous = segments[segments.Count - 1];
			previous.x.nextnext = x;
			previous.y.nextnext = y;
			segments[segments.Count - 1] = previous;

			Segment2d segment;
			segment.y.nextnext = y;
			segment.x.next = segment.x.nextnext = x;
			segment.y.next = segment.y.nextnext = y;
			segment.x.current = previous.x.next;
			segment.y.current = previous.y.next;
			segment.x.previous = previous.x.current;
			segment.y.previous = previous.y.current;
			segments.Add(segment);
		}

		public MainWindow() {
			OnResize(this, new EventArgs());
            Paint += OnPaint;
			MouseMove += OnMouseMove;
			MouseDown += OnMouseDown;
			MouseUp += OnMouseUp;
			Resize += OnResize;
            WindowState = FormWindowState.Maximized;
        }

		public void OnResize(Object sender, EventArgs e) {
			buffer = new Bitmap(ClientSize.Width, ClientSize.Height);
		}

        public void OnPaint(Object sender, PaintEventArgs e) {
			Graphics g = Graphics.FromImage(buffer);
            Draw(g);
			g.Flush();
			e.Graphics.DrawImageUnscaled(buffer, 0, 0);
        }

		public void OnMouseDown(Object sender, MouseEventArgs e) {
			if (e.Button == MouseButtons.Left) {
				newCurve(e.X, e.Y);
				pressed = true;
				Invalidate();
			} else
			if (e.Button == MouseButtons.Right) {
				segments.Clear();
				if (pressed) newCurve(e.X, e.Y);
				Invalidate();
			}
		}

		public void OnMouseUp(Object sender, MouseEventArgs e) {
			if (e.Button == MouseButtons.Left && pressed) {
				addPoint(e.X, e.Y);
				pressed = false;
				Invalidate();
			}
		}

		public void OnMouseMove(Object sender, MouseEventArgs e) {
			if (pressed) {
				addPoint(e.X, e.Y);
				Invalidate();
			}
        }

        public void Draw(Graphics g) {
            g.Clear(Color.White);
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
			int count = 100;
			double r = 2.0;
			double l = 2.0;

			Pen pen = new Pen(Color.FromArgb(128, 128, 128, 128));
			Pen cool = new Pen(Color.FromArgb(128, 0, 0, 255));

			foreach(Segment2d segment in segments) {
				double x0 = segment.x.get(0.0);
				double y0 = segment.y.get(0.0);
				for(int i = 1; i <= count; ++i) {
					double x1 = segment.x.get((double)i/(double)count);
					double y1 = segment.y.get((double)i/(double)count);
					if (i == count || (x1 - x0)*(x1 - x0) + (y1 - y0)*(y1 - y0) > l*l) {
						g.DrawLine(pen, (float)x0, (float)y0, (float)x1, (float)y1);
						x0 = x1;
						y0 = y1;
					}
				}

				x0 = segment.x.getCool2(0.0);
				y0 = segment.y.getCool2(0.0);
				for(int i = 1; i <= count; ++i) {
					double x1 = segment.x.getCool2((double)i/(double)count);
					double y1 = segment.y.getCool2((double)i/(double)count);
					if (i == count || (x1 - x0)*(x1 - x0) + (y1 - y0)*(y1 - y0) > l*l) {
						g.DrawLine(cool, (float)x0, (float)y0, (float)x1, (float)y1);
						x0 = x1;
						y0 = y1;
					}
				}

				g.FillEllipse(
					Brushes.Black,
					(float)(segment.x.current - r), (float)(segment.y.current - r),
					(float)(2.0*r), (float)(2.0*r) );
				g.FillEllipse(
					Brushes.Black,
					(float)(segment.x.next - r), (float)(segment.y.next - r),
					(float)(2.0*r), (float)(2.0*r) );
			}
        }
    }
}

