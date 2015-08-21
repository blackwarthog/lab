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

#include <cassert>

#include "contour.h"


using namespace std;


const Vector Contour::blank;


void Contour::clear() {
	if (!chunks.empty()) {
		chunks.clear();
		first = 0;
	}
}

void Contour::move_to(const Vector &v) {
	if (chunks.empty()) {
		if (!v.is_equal_to(blank))
			chunks.push_back(Chunk(MOVE, v));
	} else {
		if (!v.is_equal_to(chunks.back().p1)) {
			if (chunks.back().type == MOVE)
				chunks.back().p1 = v;
			else
			if (chunks.back().type == CLOSE)
				chunks.push_back(Chunk(MOVE, v));
			else {
				chunks.push_back(Chunk(CLOSE, chunks[first].p1));
				chunks.push_back(Chunk(MOVE, v));
			}
		}
	}
	first = chunks.size();
}

void Contour::line_to(const Vector &v) {
	if (!v.is_equal_to(current()))
		chunks.push_back(Chunk(LINE, v));
}

void Contour::conic_to(const Vector &v, const Vector &t) {
	if (!v.is_equal_to(current()))
		chunks.push_back(Chunk(CONIC, v, t));
}

void Contour::cubic_to(const Vector &v, const Vector &t0, const Vector &t1) {
	if (!v.is_equal_to(current()))
		chunks.push_back(Chunk(CUBIC, v, t0, t1));
}

void Contour::close() {
	if (chunks.size() > first) {
		if (first > 0)
			chunks.push_back(Chunk(CLOSE, chunks[first-1].p1));
		else
			chunks.push_back(Chunk(CLOSE, blank));
		first = chunks.size();
	}
}

void Contour::line_split(
	Rect &ref_line_bounds,
	const Rect &bounds,
	const Vector &min_size,
	const Vector &p1 )
{
	line_to(p1);
	return;

	// TODO: fix bugs

	ref_line_bounds = ref_line_bounds.expand(p1);

	if (bounds.intersects(ref_line_bounds)) {
		Vector s = ref_line_bounds.p1 - ref_line_bounds.p0;
		if ( fabs(s.x) > min_size.x
		  || fabs(s.y) > min_size.y )
		{
			line_to(p1);
			ref_line_bounds.p0 = p1;
			ref_line_bounds.p1 = p1;
			return;
		}
	}

	if (!chunks.empty())
		chunks.back().p1 = p1;
}

void Contour::conic_split(
	Rect &ref_line_bounds,
	const Rect &bounds,
	const Vector &min_size,
	const Vector &p1,
	const Vector &center,
	Real radius,
	Real radians0,
	Real radians1,
	int level )
{
	assert(level > 0);

	const Vector &p0 = current();
	if ( fabs(p1.x - p0.x) > min_size.x
	  || fabs(p1.y - p0.y) > min_size.y )
	{
		Rect b = conic_bounds(p0, p1, center, radius, radians0, radians1);
		if (bounds.intersects(b)) {
			Real radians = 0.5*(radians0 + radians1);
			Vector p( radius*cos(radians) + center.x,
					  radius*sin(radians) + center.y );
			conic_split(ref_line_bounds, bounds, min_size,  p, center, radius, radians0, radians, level - 1);
			conic_split(ref_line_bounds, bounds, min_size, p1, center, radius, radians, radians1, level - 1);
			return;
		}
	}
	line_split(ref_line_bounds, bounds, min_size, p1);
}

void Contour::cubic_split(
	Rect &ref_line_bounds,
	const Rect &bounds,
	const Vector &min_size,
	const Vector &p1,
	const Vector &bezier_pp0,
	const Vector &bezier_pp1,
	int level )
{
	assert(level > 0);

	const Vector &p0 = current();
	if ( fabs(p1.x - p0.x) > min_size.x
	  || fabs(p1.y - p0.y) > min_size.y )
	{
		Rect b = cubic_bounds(p0, p1, bezier_pp0, bezier_pp1);
		if (bounds.intersects(b)) {
			Vector pp = (bezier_pp0 + bezier_pp1)*0.5;
			Vector pp00 = (p0 + bezier_pp0)*0.5;
			Vector pp11 = (p1 + bezier_pp1)*0.5;
			Vector pp01 = (pp00 + pp)*0.5;
			Vector pp10 = (pp11 + pp)*0.5;
			Vector p = (pp01 + pp10)*0.5;

			cubic_split(ref_line_bounds, bounds, min_size,  p, pp00, pp01, level - 1);
			cubic_split(ref_line_bounds, bounds, min_size, p1, pp10, pp11, level - 1);
			return;
		}
	}
	line_split(ref_line_bounds, bounds, min_size, p1);
}

void Contour::split(Contour &c, const Rect &bounds, const Vector &min_size) const {
	Rect line_bounds;
	line_bounds.p0 = c.current();
	line_bounds.p1 = c.current();

	for(ChunkList::const_iterator i = chunks.begin(); i != chunks.end(); ++i) {
		switch(i->type) {
		case MOVE:
			c.move_to(i->p1);
			line_bounds.p0 = c.current();
			line_bounds.p1 = c.current();
			break;
		case LINE:
			c.line_split(line_bounds, bounds, min_size, i->p1);
			break;
		case CLOSE:
			c.close();
			break;
		case CONIC:
			{
				const Vector &p0 = c.current();
				Vector center;
				Real radius = 0.0;
				Real radians0 = 0.0;
				Real radians1 = 0.0;
				if (conic_convert(p0, i->p1, i->t0, center, radius, radians0, radians1))
					c.conic_split(line_bounds, bounds, min_size, i->p1, center, radius, radians0, radians1);
				else
					c.line_split(line_bounds, bounds, min_size, i->p1);
			}
			break;
		case CUBIC:
			{
				const Vector &p0 = c.current();
				Vector pp0, pp1;
				cubic_convert(p0, i->p1, i->t0, i->t1, pp0, pp1);
				c.cubic_split(line_bounds, bounds, min_size, i->p1, pp0, pp1);
			}
			break;
		}
	}
}

bool Contour::conic_convert(
	const Vector &p0,
	const Vector &p1,
	const Vector &t,
	Vector &out_center,
	Real &out_radius,
	Real &out_radians0,
	Real &out_radians1 )
{
	Real tl = sqrt(t.x*t.x + t.y*t.y);
	if (fabs(tl) < 1e-6) {
		out_center = Vector();
		out_radius = 0.0;
		out_radians0 = 0.0;
		out_radians1 = 0.0;
		return false;
	}

	Vector d = p1 - p0;
	Vector n(-t.y/tl, t.x/tl);

	Real r = 0.5*(d.x*d.x + d.y*d.y)/(d.x*n.x + d.y*n.y);
	out_center = p0 + n*r;
	out_radius = fabs(r);
	out_radians0 = atan2(p0.y - out_center.y, p0.x - out_center.x);
	out_radians1 = atan2(p1.y - out_center.y, p1.x - out_center.x);
	bool ccw = r > 0.0;

	out_radians0 = wrap_angle(out_radians0, 2.0*M_PI);
	out_radians1 = wrap_angle(out_radians1, 2.0*M_PI);
	if (ccw) { if (out_radians1 < out_radians0) out_radians1 += 2.0*M_PI; }
	    else { if (out_radians1 > out_radians0) out_radians1 -= 2.0*M_PI; }

	return true;
}

Rect Contour::conic_bounds(
	const Vector &p0,
	const Vector &p1,
	const Vector &center,
	Real radius,
	Real radians0,
	Real radians1 )
{
	radius = fabs(radius);

	Rect r;
	r.p0 = p0;
	r.p1 = p1;

	if (angle_between(radians0, radians1, 0.0*M_PI, 2.0*M_PI))
		r = r.expand(Vector(center.x + radius, center.y));
	if (angle_between(radians0, radians1, 0.5*M_PI, 2.0*M_PI))
		r = r.expand(Vector(center.x, center.y + radius));
	if (angle_between(radians0, radians1, 1.0*M_PI, 2.0*M_PI))
		r = r.expand(Vector(center.x - radius, center.y));
	if (angle_between(radians0, radians1, 1.5*M_PI, 2.0*M_PI))
		r = r.expand(Vector(center.x, center.y - radius));

	return r;
}

void Contour::cubic_convert(
	const Vector &p0,
	const Vector &p1,
	const Vector &t0,
	const Vector &t1,
	Vector &out_bezier_pp0,
	Vector &out_bezier_pp1 )
{
	out_bezier_pp0 = t0/3.0 + p0;
	out_bezier_pp1 = p1 - t1/3.0;
}

Rect Contour::cubic_bounds(
	const Vector &p0,
	const Vector &p1,
	const Vector &bezier_pp0,
	const Vector &bezier_pp1 )
{
	Rect r;
	r.p0.x = min(min(p0.x, bezier_pp0.x), min(p1.x, bezier_pp1.x));
	r.p0.y = min(min(p0.y, bezier_pp0.y), min(p1.y, bezier_pp1.y));
	r.p1.x = max(max(p0.x, bezier_pp0.x), max(p1.x, bezier_pp1.x));
	r.p1.y = max(max(p0.y, bezier_pp0.y), max(p1.y, bezier_pp1.y));
	return r;
}

void Contour::transform(const Rect &from, const Rect &to) {
	Vector s( (to.p1.x - to.p0.x)/(from.p1.x - from.p0.x),
			  (to.p1.y - to.p0.y)/(from.p1.y - from.p0.y) );
	Vector o( to.p0.x - from.p0.x*s.x,
			  to.p0.y - from.p0.y*s.y );
	for(Contour::ChunkList::iterator i = chunks.begin(); i != chunks.end(); ++i) {
		i->p1 = i->p1*s + o;
		i->t0 = i->t0*s;
		i->t1 = i->t1*s;
	}
}

void Contour::to_polyspan(Polyspan &polyspan) const {
	polyspan.move_to(0.0, 0.0);
	Vector p0;
	for(Contour::ChunkList::const_iterator i = chunks.begin(); i != chunks.end(); ++i) {
		switch(i->type) {
			case Contour::CLOSE:
				polyspan.close();
				break;
			case Contour::MOVE:
				polyspan.move_to(i->p1.x, i->p1.y);
				break;
			case Contour::LINE:
				polyspan.line_to(i->p1.x, i->p1.y);
				break;
			case Contour::CONIC: {
					Vector center;
					Real radius = 0.0;
					Real radians0 = 0.0;
					Real radians1 = 0.0;
					if (conic_convert(p0, i->p1, i->t0, center, radius, radians0, radians1)) {
						// TODO: fix bugs
						Vector pp0( center.x + 2.0*radius*cos(0.5*(radians0 + radians1)),
								    center.y + 2.0*radius*sin(0.5*(radians0 + radians1)) );
						polyspan.conic_to(pp0.x, pp0.y, i->p1.x, i->p1.y);
					} else {
						polyspan.line_to(i->p1.x, i->p1.y);
					}
				}
				break;
			case Contour::CUBIC: {
					Vector pp0, pp1;
					cubic_convert(p0, i->p1, i->t0, i->t1, pp0, pp1);
					polyspan.cubic_to(pp0.x, pp0.y, pp1.x, pp1.y, i->p1.x, i->p1.y);
				}
				break;
			default:
				break;
		}
		p0 = i->p1;
	}
}
