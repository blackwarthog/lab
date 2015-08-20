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

#ifndef _CONTOUR_H_
#define _CONTOUR_H_

#include <cstddef>
#include <cmath>

#include <algorithm>
#include <vector>

template<typename T>
bool intersects(const T &a0, const T &a1, const T &b0, const T &b1) {
	return !(std::max(b0, b1) < std::min(a0, a1))
	    && !(std::max(a0, a1) < std::min(b0, b1));
}

inline double wrap_angle(double a, double round) {
	double rounds = a/round + 0.5;
	return (rounds - floor(rounds) - 0.5)*round;
}

inline bool angle_between(double a0, double a1, double a, double round) {
	if (a1 < a0) std::swap(a0, a1);
	a0 = wrap_angle(a0, round);
	a1 = wrap_angle(a1, round);
	a = wrap_angle(a, round);
	if (a < a0) a += round;
	if (a1 < a0) a1 += round;
	return a0 < a && a < a1;
}

class Vector {
public:
	union {
		struct { double x, y; };
		struct { double coords[]; };
	};

	Vector():
		x(), y() { }
	Vector(double x, double y):
		x(x), y(y) { }

	double& operator[] (int index)
		{ return coords[index]; }
	const double& operator[] (int index) const
		{ return coords[index]; }
	bool is_equal_to(const Vector &other) const
		{ return fabs(x - other.x) < 1e-6 && fabs(y - other.y) < 1e-6; }

	Vector operator+(const Vector &a) const
		{ return Vector(x + a.x, y + a.y); }
	Vector operator-(const Vector &a) const
		{ return Vector(x - a.x, y - a.y); }
	Vector operator*(double a) const
		{ return Vector(x*a, y*a); }
	Vector operator/(double a) const
		{ return Vector(x/a, y/a); }

	static Vector zero() { return Vector(); }
};

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

inline bool intersects(const Rect &a, const Rect &b)
    { return a.intersects(b); }

class Contour
{
public:
	enum ChunkType {
		CLOSE,
		MOVE,
		LINE,
		CUBIC,
		CONIC
	};

	struct Chunk {
		ChunkType type;
		Vector p1, t0, t1;
		Chunk(): type() { }
		Chunk(ChunkType type, const Vector &p1, const Vector &t0 = Vector(), const Vector &t1 = Vector()):
			type(type), p1(p1), t0(t0), t1(t1) { }
	};

	typedef std::vector<Chunk> ChunkList;

private:
	static const Vector blank;
	ChunkList chunks;
	size_t first;

public:
	Contour(): first(0) { }

	void clear();
	void move_to(const Vector &v);
	void line_to(const Vector &v);
	void cubic_to(const Vector &v, const Vector &t0, const Vector &t1);
	void conic_to(const Vector &v, const Vector &t);
	void close();

	void assign(const Contour &other);

	const ChunkList& get_chunks() const { return chunks; }

	const Vector& current() const
		{ return chunks.empty() ? blank : chunks.back().p1; }

	void split(Contour &c, const Rect &bounds, const Vector &min_size) const;

private:
	void line_split(
		Rect &ref_line_bounds,
		const Rect &bounds,
		const Vector &min_size,
		const Vector &p1 );

	void conic_split(
		Rect &ref_line_bounds,
		const Rect &bounds,
		const Vector &min_size,
		const Vector &p1,
		const Vector &center,
		double radius,
		double radians0,
		double radians1,
		int level = 64 );

	void cubic_split(
		Rect &ref_line_bounds,
		const Rect &bounds,
		const Vector &min_size,
		const Vector &p1,
		const Vector &bezier_pp0,
		const Vector &bezier_pp1,
		int level = 64 );

	static bool conic_convert(
		const Vector &p0,
		const Vector &p1,
		const Vector &t,
		Vector &out_center,
		double &out_radius,
		double &out_radians0,
		double &out_radians1 );

	static Rect conic_bounds(
		const Vector &p0,
		const Vector &p1,
		const Vector &center,
		double radius,
		double radians0,
		double radians1 );

	static void cubic_convert(
		const Vector &p0,
		const Vector &p1,
		const Vector &t0,
		const Vector &t1,
		Vector &out_bezier_pp0,
		Vector &out_bezier_pp1 );

	static Rect cubic_bounds(
		const Vector &p0,
		const Vector &p1,
		const Vector &bezier_pp0,
		const Vector &bezier_pp1 );
};

#endif
