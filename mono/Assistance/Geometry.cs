using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Assistance {
	public static class Geometry {
		public static readonly double precision = 1e-8;
		public static readonly double precisionSqr = precision*precision;
		public static readonly double sqrt2Pi = Math.Sqrt(2.0*Math.PI);
		
		public static bool isEqual(double a, double b)
			{ return Math.Abs(b - a) <= precision; }
		public static bool isLess(double a, double b)
			{ return b - a > precision; }
		public static bool isGreater(double a, double b)
			{ return a - b > precision; }
		public static bool isLessOrEqual(double a, double b)
			{ return a - b <= precision; }
		public static bool isGreaterOrEqual(double a, double b)
			{ return b - a <= precision; }
		
		public static double logNormalDistribuitionUnscaled(double x, double x0, double w) {
			return Math.Exp(-0.5*Math.Pow(Math.Log(x/x0)/w, 2.0))/x;
		}
		
		public static double logNormalDistribuition(double x, double x0, double w) {
			return logNormalDistribuition(x, x0, w)/(w*sqrt2Pi);
		}
		
		public static void truncateInfiniteLine(Rectangle bounds, ref Point p0, ref Point p1) {
			if (p0.isEqual(p1)) return;
			Point d = p0 - p1;
			if (Math.Abs(d.x)*bounds.height > bounds.width*Math.Abs(d.y)) {
				// horizontal
				double k = d.y/d.x;
				p1 = new Point(bounds.x1, p0.y + k*(bounds.x1 - p0.x));
				p0 = new Point(bounds.x0, p0.y + k*(bounds.x0 - p0.x));
			} else {
				// vertical
				double k = d.x/d.y;
				p1 = new Point(p0.x + k*(bounds.y1 - p0.y), bounds.y1);
				p0 = new Point(p0.x + k*(bounds.y0 - p0.y), bounds.y0);
			}
		}
		
		public static class Interpolation<T> {
			public delegate T HalfFunc(T a, double b);
			public delegate T FullFunc(T a, T b);
			
			public static FullFunc add;
			public static FullFunc sub;
			public static HalfFunc mul;
			public static HalfFunc div;
		
   			static Interpolation() {
   				{ // add
   					ParameterExpression a = Expression.Parameter(typeof(T));
				    ParameterExpression b = Expression.Parameter(typeof(T));
				    add = Expression.Lambda<FullFunc>(Expression.Add(a, b), a, b).Compile();
				}
   				{ // sub
   					ParameterExpression a = Expression.Parameter(typeof(T));
				    ParameterExpression b = Expression.Parameter(typeof(T));
				    sub = Expression.Lambda<FullFunc>(Expression.Subtract(a, b), a, b).Compile();
				}
   				{ // mul
   					ParameterExpression a = Expression.Parameter(typeof(T));
				    ParameterExpression b = Expression.Parameter(typeof(double));
				    mul = Expression.Lambda<HalfFunc>(Expression.Multiply(a, b), a, b).Compile();
				}
   				{ // div
   					ParameterExpression a = Expression.Parameter(typeof(T));
				    ParameterExpression b = Expression.Parameter(typeof(double));
				    div = Expression.Lambda<HalfFunc>(Expression.Divide(a, b), a, b).Compile();
				}
   			}
   			
			public static T linear(T p0, T p1, double l)
				{ return add(mul(sub(p1, p0), l), p0); }
	
			public static T spline(T p0, T p1, T t0, T t1, double l) {
				double ll = l*l;
				double lll = ll*l;
				return add( add( mul(p0, ( 2.0*lll - 3.0*ll + 1.0)),
				                 mul(p1, (-2.0*lll + 3.0*ll      )) ),
				            add( mul(t0, (     lll - 2.0*ll + l  )),
				                 mul(t1, (     lll - 1.0*ll      )) ));
	        }
	
			public static T splineTangent(T p0, T p1, T t0, T t1, double l) {
				double ll = l*l;
				return add( mul(sub(p0, p1), 6.0*(ll - l)),
				            add( mul(t0, ( 3.0*ll - 4.0*l + 1.0)),
				                 mul(t1, ( 3.0*ll - 2.0*l      )) ));
	        }
		}
		
		public static double interpolationLinear(double p0, double p1, double l)
			{ return (p1 - p0)*l + p0; }

		public static double interpolationSpline(double p0, double p1, double t0, double t1, double l) {
			double ll = l*l;
			double lll = ll*l;
			return p0*( 2.0*lll - 3.0*ll + 1.0)
			     + p1*(-2.0*lll + 3.0*ll      )
			     + t0*(     lll - 2.0*ll + l  )
			     + t1*(     lll - 1.0*ll      );
        }

		public static double interpolationSplineTangent(double p0, double p1, double t0, double t1, double l) {
			double ll = l*l;
			return (p0 - p1)*6.0*(ll - l)
			     + t0*( 3.0*ll - 4.0*l + 1.0)
			     + t1*( 3.0*ll - 2.0*l      );
        }

		public static Track.Point interpolationSpline(Track.Point p0, Track.Point p1, Track.Point t0, Track.Point t1, double l) {
			double ll = l*l;
			double lll = ll*l;
			return p0*( 2.0*lll - 3.0*ll + 1.0)
			     + p1*(-2.0*lll + 3.0*ll      )
			     + t0*(     lll - 2.0*ll + l  )
			     + t1*(     lll - 1.0*ll      );
        }

		public static Track.Point interpolationSplineTangent(Track.Point p0, Track.Point p1, Track.Point t0, Track.Point t1, double l) {
			double ll = l*l;
			return (p0 - p1)*6.0*(ll - l)
			     + t0*( 3.0*ll - 4.0*l + 1.0)
			     + t1*( 3.0*ll - 2.0*l      );
        }
	}
}
