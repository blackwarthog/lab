using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;

namespace Assistance {
	public class Track {
		public static readonly Pen pen = new Pen(Brushes.DarkGreen, 3f);
		public static readonly Pen penPreview = new Pen(new SolidBrush(Color.FromArgb(64, Color.DarkGreen)), 1f);
	
		public readonly List<Point> points = new List<Point>();

		public Rectangle getBounds() {
			if (points.Count == 0)
				return new Rectangle();
			Rectangle bounds = new Rectangle(points[0]);
			foreach(Point p in points)
				bounds = bounds.expand(p);
			return bounds.inflate(Math.Max(pen.Width, penPreview.Width) + 2.0);
		}

		public void draw(Graphics g, bool preview = false) {
			if (points.Count < 2)
				return;
			PointF[] ps = new PointF[points.Count];
			for(int i = 0; i < ps.Length; ++i)
				ps[i] = points[i].toFloat();
			g.DrawLines(preview ? penPreview : pen, ps);
		}
	}
}

