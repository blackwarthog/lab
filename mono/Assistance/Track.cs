using System;
using System.Collections.Generic;
using Assistance.Drawing;

namespace Assistance {
	public class Track {
		public static readonly Pen pen = new Pen("Dark Green", 3.0);
		public static readonly Pen penSpecial = new Pen("Blue", 3.0);
		public static readonly Pen penPreview = new Pen("Dark Green", 1.0, 0.25);
	
		public readonly Gdk.Device device;
		public readonly List<TrackPoint> points = new List<TrackPoint>();

		private readonly List<Track> parents = new List<Track>();
		private readonly List<Geometry.TransformFunc> transformFuncs = new List<Geometry.TransformFunc>();

		public Track(Gdk.Device device)
		{
			this.device = device;
		}

		public Track(Track parent, Geometry.TransformFunc transformFunc):
			this(parent.device)
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

		public Track createChild(Geometry.TransformFunc transformFunc){
			return new Track(this, transformFunc);
		}

		public Track createChildAndBuild(Geometry.TransformFunc transformFunc, double precision = 1.0) {
			return new Track(this, transformFunc, precision);
		}
		
		public Rectangle getBounds() {
			if (points.Count == 0)
				return new Rectangle();
			Rectangle bounds = new Rectangle(points[0].point);
			foreach(TrackPoint p in points)
				bounds = bounds.expand(p.point);
			return bounds.inflate(Math.Max(pen.width, penPreview.width) + 2.0);
		}

		public TrackPoint transform(TrackPoint p) {
			p.point = Geometry.transform(transformFuncs, p.point);
			return p;
		}
				
		private void addSpline(
			TrackPoint p0, TrackPoint p1,
			TrackPoint t0, TrackPoint t1,
			TrackPoint tp0, TrackPoint tp1,
			double l0, double l1,
			double precisionSqr
		) {
			if ((tp1.point - tp0.point).lenSqr() < precisionSqr) {
				points.Add(tp1);
			} else {
				double l = 0.5*(l0 + l1);
				TrackPoint p = p0.spawn(
					Geometry.splinePoint(p0.point, p1.point, t0.point, t1.point, l),
					p0.time + l*(p1.time - p0.time),
					Geometry.splinePoint(p0.pressure, p1.pressure, t0.pressure, t1.pressure, l),
					Geometry.splinePoint(p0.tilt, p1.tilt, t0.tilt, t1.tilt, l) );
				TrackPoint tp = transform(p);
				addSpline(p0, p1, t0, t1, tp0, tp, l0, l, precisionSqr);
				addSpline(p0, p1, t0, t1, tp, tp1, l, l1, precisionSqr);
			}
		}
		
		public void rebuild(double precision = 1.0) {
			if (parents.Count == 0) return;
			
			points.Clear();
			
			Track root = parents[0];
			if (root.points.Count < 2) {
				foreach(TrackPoint p in root.points)
					points.Add( transform(p) );
				return;
			}
			
			double precisionSqr = precision * precision;
			TrackPoint p0 = root.points[0];
			TrackPoint p1 = root.points[1];
			TrackPoint t0 = new TrackPoint();
			TrackPoint tp0 = transform(p0);
			points.Add(tp0);
			for(int i = 1; i < root.points.Count; ++i) {
				TrackPoint p2 = root.points[i+1 < root.points.Count ? i+1 : i];
				TrackPoint tp1 = transform(p1);
				double dt = p2.time - p0.time;
				TrackPoint t1 = dt > Geometry.precision
				              ? (p2 - p0)*(p1.time - p0.time)/dt
				              : new TrackPoint();
				addSpline(p0, p1, t0, t1, tp0, tp1, 0.0, 1.0, precisionSqr);

				p0 = p1;
				p1 = p2;
				tp0 = tp1;
				t0 = t1;
			}
		}

		public void draw(Cairo.Context context, bool preview = false) {
			if (preview) {
				if (points.Count < 2)
					return;
				context.Save();
				penPreview.apply(context);
				context.MoveTo(points[0].point.x, points[0].point.y);
				for(int i = 1; i < points.Count; ++i)
					context.LineTo(points[i].point.x, points[i].point.y);
				context.Stroke();
				context.Restore();
			} else {
				context.Save();
				pen.apply(context);
				foreach(TrackPoint p in points) {
					double t = p.keyState.howLongPressed(Gdk.Key.m)
					         + p.buttonState.howLongPressed(3);
					double w = p.pressure*pen.width + 5.0*t;
					context.Arc(p.point.x, p.point.y, 2.0*w, 0.0, 2.0*Math.PI);
					context.Fill();
				}
				context.Restore();
			}
		}
	}
}

