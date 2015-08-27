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
	vec2(type x, type y):
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

	vec2 operator*(type a) const
		{ return vec2(x*a, y*a); }
	vec2 operator/(type a) const
		{ return vec2(x/a, y/a); }

	type dot(const vec2 &a) const
		{ return x*a.x + y*a.y; }

	static vec2 zero() { return vec2(); }
};

typedef vec2<Real> Vector;

class Rect {
public:
	Vector p0, p1;

    bool intersects(const Rect &other) const
        { return ::intersects(p0.x, p1.x, other.p0.x, other.p1.x)
              && ::intersects(p0.y, p1.y, other.p0.y, other.p1.y); }

    Rect expand(const Vector &p) const {
    	Rect r;
    	r.p0.x = std::min(std::min(p0.x, p1.x), p.x);
    	r.p0.y = std::min(std::min(p0.y, p1.y), p.y);
    	r.p1.x = std::max(std::max(p0.x, p1.x), p.x);
    	r.p1.y = std::max(std::max(p0.y, p1.y), p.y);
    	return r;
    }
};

class ContextRect {
public:
	int minx, miny, maxx, maxy;
	ContextRect(): minx(), miny(), maxx(), maxy() { }
};

inline bool intersects(const Rect &a, const Rect &b)
    { return a.intersects(b); }

#endif
