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
			if (track.points.Count < 1)
				return double.PositiveInfinity;
			double sumWeight = 0.0;
			double sumLength = 0.0;
			double sumDeviation = 0.0;
			
			Point prev = track.points[0];
			foreach(Point p in track.points) {
				double length = (p - prev).len();
				sumLength += length;
				
				double midStepLength = sumLength - 0.5*length;
				if (midStepLength > Geometry.precision) {
					double weight = length*Geometry.logNormalDistribuitionUnscaled(midStepLength, snapLenght, snapScale);
					sumWeight += weight;
				
					double deviation = (transformPoint(p) - p).len();
					sumDeviation += weight*deviation;
				}
				prev = p;
			}
			if (sumWeight < Geometry.precision)
				return double.PositiveInfinity;
			return sumDeviation/sumWeight;
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

