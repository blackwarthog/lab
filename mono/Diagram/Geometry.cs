/*
    ......... 2015 Ivan Mahonin

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Drawing;

namespace Diagram {
    public static class Geometry {
        public static readonly float precision = 1e-5f;

        public static readonly float[][] splineMatrix = new float[][] {
            new float[] {  2f, -2f,  1f,  1f },
            new float[] { -3f,  3f, -2f, -1f },
            new float[] {  0f,  0f,  1f,  0f },
            new float[] {  1f,  0f,  0f,  0f } };

        // Compare to points place at line base0-base1
        public static int comparePointsAtLine(PointF a0, PointF a1, PointF base0, PointF base1) {
            if (base0.X < base1.X && a0.X < a1.X) return -1;
            if (base0.X < base1.X && a1.X < a0.X) return  1;
            if (base1.X < base0.X && a0.X < a1.X) return  1;
            if (base1.X < base0.X && a1.X < a0.X) return -1;
            if (base0.Y < base1.Y && a0.Y < a1.Y) return -1;
            if (base0.Y < base1.Y && a1.Y < a0.Y) return  1;
            if (base1.Y < base0.Y && a0.Y < a1.Y) return  1;
            if (base1.Y < base0.Y && a1.Y < a0.Y) return -1;
            return 0;
        }

        public static bool findIntersection(PointF a0, PointF a1, PointF b0, PointF b1, out PointF c) {
            c = new PointF(0f, 0f);
            PointF da = new PointF(a1.X - a0.X, a1.Y - a0.Y);
            PointF db = new PointF(b1.X - b0.X, b1.Y - b0.Y);

            float divider = da.X*db.Y - db.X*da.Y;
            if (Math.Abs(divider) < precision) return false;
            float numeratorX = da.X*(b1.Y*b0.X - b0.Y*b1.X)
                             - db.X*(a1.Y*a0.X - a0.Y*a1.X);
            float numeratorY = db.Y*(a1.X*a0.Y - a0.X*a1.Y)
                             - da.Y*(b1.X*b0.Y - b0.X*b1.Y);
            PointF p = new PointF(numeratorX/divider, numeratorY/divider);
            if ( comparePointsAtLine(p, a0, a0, a1) < 0
              || comparePointsAtLine(p, a1, a0, a1) > 0
              || comparePointsAtLine(p, b0, b0, b1) < 0
              || comparePointsAtLine(p, b1, b0, b1) > 0 )
                return false;

            c = p;
            return true;
        }

        public static float lineLength(PointF p0, PointF p1) {
            return (float)Math.Sqrt(
                (p1.X-p0.X)*(p1.X-p0.X)
              + (p1.Y-p0.Y)*(p1.Y-p0.Y) );
        }

        public static PointF pointAtLine(PointF p0, PointF p1, int index = 0, int count = 1, float padding = 0f) {
            float l = lineLength(p0, p1);
            float px = l > precision ? (p1.X - p0.X)*padding/l : 0f;
            float py = l > precision ? (p1.Y - p0.Y)*padding/l : 0f;
            return new PointF(
                (index+1)*(p1.X - p0.X - 2*px)/(count + 1) + p0.X + px,
                (index+1)*(p1.Y - p0.Y - 2*py)/(count + 1) + p0.Y + py);
        }

        public static PointF splineTangent(float s, PointF p0, PointF p1, PointF t0, PointF t1) {
            float h1 = 3f*splineMatrix[0][0]*s*s + 2f*splineMatrix[1][0]*s + splineMatrix[2][0];
            float h2 = 3f*splineMatrix[0][1]*s*s + 2f*splineMatrix[1][1]*s + splineMatrix[2][1];
            float h3 = 3f*splineMatrix[0][2]*s*s + 2f*splineMatrix[1][2]*s + splineMatrix[2][2];
            float h4 = 3f*splineMatrix[0][3]*s*s + 2f*splineMatrix[1][3]*s + splineMatrix[2][3];
            return new PointF(
                p0.X*h1 + p1.X*h2 + t0.X*h3 + t1.X*h4,
                p0.Y*h1 + p1.Y*h2 + t0.Y*h3 + t1.Y*h4);
        }

        public static PointF splinePoint(float s, PointF p0, PointF p1, PointF t0, PointF t1) {
            float h1 = splineMatrix[0][0]*s*s*s + splineMatrix[1][0]*s*s + splineMatrix[2][0]*s + splineMatrix[3][0];
            float h2 = splineMatrix[0][1]*s*s*s + splineMatrix[1][1]*s*s + splineMatrix[2][1]*s + splineMatrix[3][1];
            float h3 = splineMatrix[0][2]*s*s*s + splineMatrix[1][2]*s*s + splineMatrix[2][2]*s + splineMatrix[3][2];
            float h4 = splineMatrix[0][3]*s*s*s + splineMatrix[1][3]*s*s + splineMatrix[2][3]*s + splineMatrix[3][3];
            return new PointF(
                p0.X*h1 + p1.X*h2 + t0.X*h3 + t1.X*h4,
                p0.Y*h1 + p1.Y*h2 + t0.Y*h3 + t1.Y*h4 );
        }
    }
}

