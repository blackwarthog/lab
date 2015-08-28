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

#ifndef _GEOMETRY_H_
#define _GEOMETRY_H_

#include <cmath>

#include <algorithm>


typedef double Real;

template<typename T>
bool intersects(const T &a0, const T &a1, const T &b0, const T &b1) {
	return !(std::max(b0, b1) < std::min(a0, a1))
	    && !(std::max(a0, a1) < std::min(b0, b1));
}

inline Real wrap_angle(Real a, Real round) {
	Real rounds = a/round + 0.5;
	return (rounds - floor(rounds) - 0.5)*round;
}

inline bool angle_between(Real a0, Real a1, Real a, Real round) {
	if (a1 < a0) std::swap(a0, a1);
	a0 = wrap_angle(a0, round);
	a1 = wrap_angle(a1, round);
	a = wrap_angle(a, round);
	if (a < a0) a += round;
	if (a1 < a0) a1 += round;
	return a0 < a && a < a1;
}

template<typename T>
class vec2 {
public:
	typedef T type;

	union {
		struct { type x, y; };
		struct { type coords[2]; };
	};

	vec2():
		x(), y() { }
	vec2(const type &x, const type &y):
		x(x), y(y) { }

	template<typename TT>
	explicit vec2(const vec2<TT> &other):
		x((type)other.x), y((type)other.y) { }

	type& operator[] (int index)
		{ return coords[index]; }
	const type& operator[] (int index) const
		{ return coords[index]; }
	bool is_equal_to(const vec2 &other) const
		{ return fabs(x - other.x) < 1e-6 && fabs(y - other.y) < 1e-6; }

	vec2 operator+(const vec2 &a) const
		{ return vec2(x + a.x, y + a.y); }
	vec2 operator-(const vec2 &a) const
		{ return vec2(x - a.x, y - a.y); }
	vec2 operator*(const vec2 &a) const
		{ return vec2(x*a.x, y*a.y); }
	vec2 operator/(const vec2 &a) const
		{ return vec2(x/a.x, y/a.y); }

	vec2 operator*(const type &a) const
		{ return vec2(x*a, y*a); }
	vec2 operator/(const type &a) const
		{ return vec2(x/a, y/a); }

	type dot(const vec2 &a) const
		{ return x*a.x + y*a.y; }

	static vec2 zero() { return vec2(); }
};

template<typename T>
class line2 {
public:
	typedef T type;
	vec2<type> p0, p1;
	line2() { }
	line2(const vec2<type> &p0, const vec2<type> &p1): p0(p0), p1(p1) { }
	line2(const type &x0, const type &y0, const type &x1, const type &y1): p0(x0, y0), p1(x1, y1) { }
};

template<typename T>
class rect {
public:
	typedef T type;
	vec2<type> p0, p1;

    bool intersects(const rect &other) const
        { return ::intersects(p0.x, p1.x, other.p0.x, other.p1.x)
              && ::intersects(p0.y, p1.y, other.p0.y, other.p1.y); }

    rect expand(const vec2<type> &p) const {
    	rect r;
    	r.p0.x = std::min(std::min(p0.x, p1.x), p.x);
    	r.p0.y = std::min(std::min(p0.y, p1.y), p.y);
    	r.p1.x = std::max(std::max(p0.x, p1.x), p.x);
    	r.p1.y = std::max(std::max(p0.y, p1.y), p.y);
    	return r;
    }

    rect() { }
    rect(const vec2<type> &p0, const vec2<type> &p1): p0(p0), p1(p1) { }
    rect(const type &x0, const type &y0, const type &x1, const type &y1): p0(x0, y0), p1(x1, y1) { }
};

typedef vec2<Real> Vector;
typedef line2<Real> Line;
typedef rect<Real> Rect;

typedef vec2<float> vec2f;
typedef line2<float> line2f;
typedef rect<float> rectf;

class ContextRect {
public:
	int minx, miny, maxx, maxy;
	ContextRect(): minx(), miny(), maxx(), maxy() { }
};

#endif
