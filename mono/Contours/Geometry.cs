using System;
using System.Drawing;

namespace Contours {
    public static class Geometry {
        public enum IntersectionType {
            None,
            Cross,
            Identical,
            Inverted,
            Touch_a0,
            Touch_a1,
            Touch_b0,
            Touch_b1,
            Touch_a0_b0,
            Touch_a0_b1,
            Touch_a1_b0,
            Touch_a1_b1,
            Along_a0_b0_a1_b1,
            Along_a0_b0_b1_a1,
            Along_a0_b1_a1_b0,
            Along_a0_b1_b0_a1,
            Along_b0_a0_a1_b1,
            Along_b0_a0_b1_a1,
            Along_b1_a0_a1_b0,
            Along_b1_a0_b0_a1
        }

        // Compare to points place at line base0-base1
        public static int comparePointsAtLine(Point a0, Point a1, Point base0, Point base1) {
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

        public static bool isPointAtLine(Point p, Point p0, Point p1) {
            if (p == p0 || p == p1)
                return true;
            if (p0 == p1)
                return false;
            if ((long)(p.Y-p0.Y)*(long)(p1.X-p0.X) != (long)(p.X-p0.X)*(long)(p1.Y-p0.Y))
                return false;
            if (p1.X > p0.X)
                return p.X >= p0.X && p.X <= p1.X;
            if (p1.X < p0.X)
                return p.X >= p1.X && p.X <= p0.X;
            if (p1.Y > p0.Y)
                return p.Y >= p0.Y && p.Y <= p1.Y;
            //if (p1.Y < p0.Y)
                return p.Y >= p1.Y && p.Y <= p0.Y;
        }

        public static IntersectionType findIntersection(Point a0, Point a1, Point b0, Point b1, out Point c) {
            c = new Point(0, 0);
            Point da = new Point(a1.X - a0.X, a1.Y - a0.Y);
            Point db = new Point(b1.X - b0.X, b1.Y - b0.Y);

            if (a0.X == b0.X && a0.Y == b0.Y && a1.X == b1.X && a1.Y == b1.Y)
                return IntersectionType.Identical;

            if (a0.X == b1.X && a0.Y == b1.Y && a1.X == b0.X && a1.Y == b0.Y)
                return IntersectionType.Inverted;

            long divider = (long)da.X*(long)db.Y - (long)db.X*(long)da.Y;
            if (divider == 0) {
                if ((long)da.X*(long)(b0.Y - a0.Y) != (long)da.Y*(long)(b0.X - a0.X))
                    return IntersectionType.None;

                int a0b0 = comparePointsAtLine(a0, b0, a0, a1);
                int a0b1 = comparePointsAtLine(a0, b1, a0, a1);
                int a1b0 = comparePointsAtLine(a1, b0, a0, a1);
                int a1b1 = comparePointsAtLine(a1, b1, a0, a1);
                int b0b1 = comparePointsAtLine(b0, b1, a0, a1);
                int b0a0 = -a0b0;
                int b0a1 = -a1b0;
                int b1a0 = -a0b1;
                int b1a1 = -a1b1;
                int b1b0 = -b0b1;

                // a0a1b0b1
                if (a1b0 == 0 && b0b1 <= 0)
                    return IntersectionType.Touch_a1_b0;
                // a0a1b1b0
                if (a1b1 == 0 && b1b0 <= 0)
                    return IntersectionType.Touch_a1_b1;
                // b0b1a0a1
                if (b0b1 <= 0 && b1a0 == 0)
                    return IntersectionType.Touch_a0_b1;
                // b1b0a0a1
                if (b1b0 <= 0 && b0a0 == 0)
                    return IntersectionType.Touch_a0_b0;

                if (a0b0 <= 0 && b0a1 <= 0 && a1b1 <= 0)
                    return IntersectionType.Along_a0_b0_a1_b1;
                if (a0b0 <= 0 && b0b1 <= 0 && b1a1 <= 0)
                    return IntersectionType.Along_a0_b0_b1_a1;
                if (a0b1 <= 0 && b1a1 <= 0 && a1b0 <= 0)
                    return IntersectionType.Along_a0_b1_a1_b0;
                if (a0b1 <= 0 && b1b0 <= 0 && b0a1 <= 0)
                    return IntersectionType.Along_a0_b1_b0_a1;
                if (b0a0 <= 0 && /*  a0a1  */ a1b1 <= 0)
                    return IntersectionType.Along_b0_a0_a1_b1;
                if (b0a0 <= 0 && a0b1 <= 0 && b1a1 <= 0)
                    return IntersectionType.Along_b0_a0_b1_a1;
                if (b1a0 <= 0 && /*  a0a1  */ a1b0 <= 0)
                    return IntersectionType.Along_b1_a0_a1_b0;
                if (b1a0 <= 0 && a0b0 <= 0 && b0a1 <= 0)
                    return IntersectionType.Along_b1_a0_b0_a1;

                return IntersectionType.None;
            }

            if (a0.X == b0.X && a0.Y == b0.Y)
                return IntersectionType.Touch_a0_b0;
            if (a0.X == b1.X && a0.Y == b1.Y)
                return IntersectionType.Touch_a0_b1;
            if (a1.X == b0.X && a1.Y == b0.Y)
                return IntersectionType.Touch_a1_b0;
            if (a1.X == b1.X && a1.Y == b1.Y)
                return IntersectionType.Touch_a1_b1;

            long numeratorX = (long)da.X*((long)b1.Y*(long)b0.X - (long)b0.Y*(long)b1.X)
                            - (long)db.X*((long)a1.Y*(long)a0.X - (long)a0.Y*(long)a1.X);
            long numeratorY = (long)db.Y*((long)a1.X*(long)a0.Y - (long)a0.X*(long)a1.Y)
                            - (long)da.Y*((long)b1.X*(long)b0.Y - (long)b0.X*(long)b1.Y);
            Point p = new Point((int)(numeratorX/divider), (int)(numeratorY/divider));
            if ( comparePointsAtLine(p, a0, a0, a1) < 0
              || comparePointsAtLine(p, a1, a0, a1) > 0
              || comparePointsAtLine(p, b0, b0, b1) < 0
              || comparePointsAtLine(p, b1, b0, b1) > 0 )
                return IntersectionType.None;

            if (p.X == a0.X && p.Y == a0.Y)
                return IntersectionType.Touch_a0;
            if (p.X == a1.X && p.Y == a1.Y)
                return IntersectionType.Touch_a1;
            if (p.X == b0.X && p.Y == b0.Y)
                return IntersectionType.Touch_b0;
            if (p.X == b1.X && p.Y == b1.Y)
                return IntersectionType.Touch_b1;

            c = p;
            return IntersectionType.Cross;
        }
        
        public static bool isCCW(Point a, Point b, Point c) {
            long d = (long)a.X*(long)c.Y - (long)a.Y*(long)c.X;
            // angle AC < 180 deg
            if (d > 0)
                return (long)a.X*(long)b.Y >= (long)a.Y*(long)b.X
                    && (long)c.X*(long)b.Y <= (long)c.Y*(long)b.X;
            // angle AC > 180 deg
            if (d < 0)
                return (long)a.X*(long)b.Y >= (long)a.Y*(long)b.X
                    || (long)c.X*(long)b.Y <= (long)c.Y*(long)b.X;
            // angle AC == 180 deg
            if ((a.X >= 0) != (c.X >= 0) || (a.Y >= 0) != (c.Y >= 0))
                return (long)a.X*(long)b.Y >= (long)a.Y*(long)b.X;
            // angle AC == 0 deg
            return true;
        }

        public static bool isCCW(Point center, Point a, Point b, Point c) {
            return isCCW(
                new Point(a.X - center.X, a.Y - center.Y),
                new Point(b.X - center.X, b.Y - center.Y),
                new Point(c.X - center.X, c.Y - center.Y) );
        }
        
        public static void makeLongestLine(Point p0, ref Point p1) {
            int MaxValue = int.MaxValue >> 1;
            Point direction = new Point(p1.X - p0.X, p1.Y - p0.Y);
            int amplifierX =
                direction.X > 0 ? (MaxValue - p0.X)/direction.X + 1
              : direction.X < 0 ? (MaxValue + p0.X)/(-direction.X) + 1
              : int.MaxValue;
            int amplifierY =
                direction.Y > 0 ? (MaxValue - p0.Y)/direction.Y + 1
              : direction.Y < 0 ? (MaxValue + p0.Y)/(-direction.Y) + 1
              : int.MaxValue;
            int amplifier = Math.Min(amplifierX, amplifierY);
            p1 = new Point(
                    p0.X + direction.X*amplifier,
                    p0.Y + direction.Y*amplifier );
        }
                
    }
}

