using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;

namespace Assistance {
	public class Track {
		public static readonly Pen pen = new Pen(Brushes.DarkGreen, 3f);
		public static readonly Pen penPreview = new Pen(new SolidBrush(Color.FromArgb(64, Color.DarkGreen)), 1f);
	
		public readonly List<Point> points = new List<Point>();

		private readonly List<Track> parents = new List<Track>();
		private readonly List<Geometry.TransformFunc> transformFuncs = new List<Geometry.TransformFunc>();

		public Track() { }

		public Track(Track parent, Geometry.TransformFunc transformFunc):
			this()
		{
			parents.AddRange(parent.parents);
			parents.Add(parent);
			transformFuncs.AddRange(parent.transformFuncs);
			transformFuncs.Add(transformFunc);
		}

		public Track(Track parent, Geometry.TransformFunc transformFunc, double precision):
			this(parent, transformFunc)
		{
			rebuild(precision);
		}

		public Track createChild(Geometry.TransformFunc transformFunc, double precision){
			return new Track(this, transformFunc);
		}

		public Track createChildAndBuild(Geometry.TransformFunc transformFunc, double precision = 1.0) {
			return new Track(this, transformFunc, precision);
		}

		public Rectangle getBounds() {
			if (points.Count == 0)
				return new Rectangle();
			Rectangle bounds = new Rectangle(points[0]);
			foreach(Point p in points)
				bounds = bounds.expand(p);
			return bounds.inflate(Math.Max(pen.Width, penPreview.Width) + 2.0);
		}
		
		private void addSpline(Point p0, Point p1, Point t0, Point t1, Point tp0, Point tp1, double l0, double l1, double precisionSqr) {
			if ((tp1 - tp0).lenSqr() < precisionSqr) {
				points.Add(tp1);
			} else {
				double l = 0.5*(l0 + l1);
				Point p = Geometry.splinePoint(p0, p1, t0, t1, l);
				Point tp = Geometry.transform(transformFuncs, p);
				addSpline(p0, p1, t0, t1, tp0, tp, l0, l, precisionSqr);
				addSpline(p0, p1, t0, t1, tp, tp1, l, l1, precisionSqr);
			}
		}
		
		public void rebuild(double precision = 1.0) {
			if (parents.Count == 0) return;
			
			points.Clear();
			
			Track root = parents[0];
			if (root.points.Count < 2) {
				foreach(Point p in root.points)
					points.Add( Geometry.transform(transformFuncs, p) );
				return;
			}
			
			double precisionSqr = precision * precision;
			Point p0 = root.points[0];
			Point p1 = root.points[1];
			Point t0 = 0.5*(p1 - p0);
			Point tp0 = Geometry.transform(transformFuncs, p0);
			points.Add(tp0);
			for(int i = 1; i < root.points.Count; ++i) {
				Point p2 = root.points[i+1 < root.points.Count ? i+1 : i];
				Point tp1 = Geometry.transform(transformFuncs, p1);
				Point t1 = 0.5*(p2 - p0);
				addSpline(p0, p1, t0, t1, tp0, tp1, 0.0, 1.0, precisionSqr);

				p0 = p1;
				p1 = p2;
				tp0 = tp1;
				t0 = t1;
			}
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

