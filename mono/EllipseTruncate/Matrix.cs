using System;

namespace EllipseTruncate {
	public struct Matrix {
		public double m00, m01, m02,
		              m10, m11, m12,
		              m20, m21, m22;
		
		public Matrix(Point p0, Point p1, Point p2 = new Point()) {
			m00 = p0.x; m01 = p0.y; m02 = 0.0;
			m10 = p1.x; m11 = p1.y; m12 = 0.0;
			m20 = p2.x; m21 = p2.y; m22 = 1.0;
		}

		public Matrix(Cairo.Matrix m) {
			m00 = m.Xx; m01 = m.Yx; m02 = 0.0;
			m10 = m.Xy; m11 = m.Yy; m12 = 0.0;
			m20 = m.X0; m21 = m.Y0; m22 = 1.0;
		}

		public Point row0()
			{ return new Point(m00, m01); }
		public Point row1()
			{ return new Point(m10, m11); }
		public Point row2()
			{ return new Point(m20, m21); }

		public Point row(int index) {
			switch(index) {
			case 0: return row0();
			case 1: return row1();
			case 2: return row2();
			}
			return new Point();
		}

		public static Matrix zero()
			{ return new Matrix(); }

		public static Matrix identity() {
			Matrix m = new Matrix();
			m.m00 = m.m11 = m.m22 = 1.0;
			return m;
		}

		public static Matrix rotation(double angle) {
			double s = Math.Sin(angle);
			double c = Math.Cos(angle);
			Matrix m = identity();
			m.m00 = c; m.m01 = s;
			m.m10 =-s; m.m11 = c;
			return m;
		}

		public static Matrix translation(double x, double y) {
			Matrix m = identity();
			m.m20 = x; m.m21 = y;
			return m;
		}

		public static Matrix scaling(double x, double y) {
			Matrix m = identity();
			m.m00 = x; m.m11 = y;
			return m;
		}
		
		public static Matrix translation(Point t)
			{ return translation(t.x, t.y); }
		public static Matrix scaling(Point s)
			{ return scaling(s.x, s.y); }
		public static Matrix scaling(double s)
			{ return scaling(s, s); }
		
		public Matrix rotate(double angle)
			{ return this*rotation(angle); }
		public Matrix translate(double x, double y)
			{ return this*translation(x, y); }
		public Matrix translate(Point t)
			{ return this*translation(t); }
		public Matrix scale(double x, double y)
			{ return this*scaling(x, y); }
		public Matrix scale(Point s)
			{ return this*scaling(s); }
			
		public Point transform(Point p) {
			return new Point( m00*p.x + m10*p.y + m20,
			                  m01*p.x + m11*p.y + m21 );
		}

		public Point turn(Point p) {
			return new Point( m00*p.x + m10*p.y,
			                  m01*p.x + m11*p.y );
		}

		public static Point operator* (Matrix m, Point p)
			{ return m.transform(p); }
		
		public static Matrix operator* (Matrix a, Matrix b) {
			Matrix m = new Matrix();
			
			m.m00 = a.m00*b.m00 + a.m10*b.m01 + a.m20*b.m02;
			m.m01 = a.m01*b.m00 + a.m11*b.m01 + a.m21*b.m02;
			m.m02 = a.m02*b.m00 + a.m12*b.m01 + a.m22*b.m02;
		
			m.m10 = a.m00*b.m10 + a.m10*b.m11 + a.m20*b.m12;
			m.m11 = a.m01*b.m10 + a.m11*b.m11 + a.m21*b.m12;
			m.m12 = a.m02*b.m10 + a.m12*b.m11 + a.m22*b.m12;

			m.m20 = a.m00*b.m20 + a.m10*b.m21 + a.m20*b.m22;
			m.m21 = a.m01*b.m20 + a.m11*b.m21 + a.m21*b.m22;
			m.m22 = a.m02*b.m20 + a.m12*b.m21 + a.m22*b.m22;
		
			return m;
		}
		
		public bool isEqual(Matrix other) {
			return Math.Abs(m00 - other.m00) <= Geometry.precision
			    && Math.Abs(m01 - other.m01) <= Geometry.precision
			    && Math.Abs(m02 - other.m02) <= Geometry.precision
			    && Math.Abs(m10 - other.m10) <= Geometry.precision
			    && Math.Abs(m11 - other.m11) <= Geometry.precision
			    && Math.Abs(m12 - other.m12) <= Geometry.precision
			    && Math.Abs(m20 - other.m20) <= Geometry.precision
			    && Math.Abs(m21 - other.m21) <= Geometry.precision
			    && Math.Abs(m22 - other.m22) <= Geometry.precision;
		}
		
		static double det2x2(double m00, double m01, double m10, double m11)
			{ return m00*m11 - m10*m01; }
		
		public double det() {
			return m00*m11*m22 - m00*m12*m21
			     + m01*m12*m20 - m01*m10*m22
			     + m02*m10*m21 - m02*m11*m20;
		}
		
		public Matrix invert() {
			double d = det();
			Matrix m = new Matrix();
			if (Math.Abs(d) > Geometry.precision) {
				d = 1.0/d;
				double e = -d;
				m.m00 = det2x2(m11, m12, m21, m22)*d;
				m.m10 = det2x2(m10, m12, m20, m22)*e;
				m.m20 = det2x2(m10, m11, m20, m21)*d;

				m.m01 = det2x2(m01, m02, m21, m22)*e;
				m.m11 = det2x2(m00, m02, m20, m22)*d;
				m.m21 = det2x2(m00, m01, m20, m21)*e;

				m.m02 = det2x2(m01, m02, m11, m12)*d;
				m.m12 = det2x2(m00, m02, m10, m12)*e;
				m.m22 = det2x2(m00, m01, m10, m11)*d;
			}
			return m;
		}

		public Cairo.Matrix toCairo()
			{ return new Cairo.Matrix(m00, m01, m10, m11, m20, m21); }
	}
}

