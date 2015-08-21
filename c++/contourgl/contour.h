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

#include <vector>

#include "geometry.h"
#include "polyspan.h"

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

	const ChunkList& get_chunks() const { return chunks; }

	const Vector& current() const
		{ return chunks.empty() ? blank : chunks.back().p1; }

	void split(Contour &c, const Rect &bounds, const Vector &min_size) const;

	void transform(const Rect &from, const Rect &to);

	void to_polyspan(Polyspan &polyspan) const;

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
		Real radius,
		Real radians0,
		Real radians1,
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
		Real &out_radius,
		Real &out_radians0,
		Real &out_radians1 );

	static Rect conic_bounds(
		const Vector &p0,
		const Vector &p1,
		const Vector &center,
		Real radius,
		Real radians0,
		Real radians1 );

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
