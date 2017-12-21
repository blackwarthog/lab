using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;

namespace Assistance {
	public class Guideline {
		public static readonly Pen pen = Pens.LightGray;
		public static readonly Pen penActive = Pens.DeepSkyBlue;
		public static readonly double snapLenght = 20.0;
		public static readonly double snapScale = 1.0;
	
		public virtual Point transformPoint(Point p) {
			return p;
		}
		
		public virtual void draw(Graphics g, bool active) { }

		public void draw(Graphics g) {
			draw(g, false);
		}
		
		public double calcTrackWeight(Track track) {
			if (track.points.Count < 2)
				return double.PositiveInfinity;
			double sumWeight = 0.0;
			double sumLength = 0.0;
			double sumDeviation = 0.0;
			
			for(int i = 1; i < track.points.Count; ++i) {
				Point point = track.points[i];
			
				double length = (point - track.points[i - 1]).len();
				sumLength += length;
				
				double weight = Geometry.logNormalDistribuitionUnscaled(sumLength, snapLenght, snapScale);
				sumWeight += weight;
				
				double deviation = (transformPoint(point) - point).len();
				sumDeviation += weight*deviation;
			}
			return sumDeviation/sumWeight;
		}

		public Track modifyTrack(Track track) {
			Track t = new Track();
			foreach(Point p in track.points)
				t.points.Add( transformPoint(p) );
			return t;
		}
		
		public static Guideline findBest(List<Guideline> guidelines, Track track) {
			double bestWeight = double.PositiveInfinity;
			Guideline best = null;
			foreach(Guideline guideline in guidelines) {
				double weight = guideline.calcTrackWeight(track);
				if (weight < bestWeight) {
					bestWeight = weight;
					best = guideline;
				}
			}
			return best;
		}
	}
}

