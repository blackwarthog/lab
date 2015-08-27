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

#include <iostream>

#include "triangulator.h"


using namespace std;


bool Triangulator::intersect_lines(Vector a0, Vector a1, Vector b0, Vector b1) {
	static const Real precision = 1e-6;

	Vector da(a1 - a0);
	Vector pa(-da.y, da.x);

	Real d0, d1;

	d0 = pa.dot(b0 - a0);
	if (fabs(d0) < precision) return false;
	d1 = pa.dot(b1 - a0);
	if (fabs(d1) < precision) return false;
	if ((d0 > 0.0) == (d1 > 0.0)) return false;

	Vector db(b1 - b0);
	Vector pb(-db.y, db.x);

	d0 = pa.dot(a0 - b0);
	if (fabs(d0) < precision) return false;
	d1 = pa.dot(a1 - b0);
	if (fabs(d1) < precision) return false;
	if ((d0 > 0.0) == (d1 > 0.0)) return false;

	return true;
}

void Triangulator::build_path(const Contour &contour, Path &path, int index_offset) {
	// TODO: connect multiple contours
	path.clear();
	path.resize(contour.get_chunks().size());
	for(int i = 0; i < (int)contour.get_chunks().size(); ++i) {
		const Contour::Chunk &c = contour.get_chunks()[i];
		Vertex &v = path[i];
		v.index = i + index_offset;
		v.p = c.p1;
		v.next = &v + 1;
	}
	path.back().next = &path.front();
}

bool Triangulator::check_triangle(Vertex *first, TriangleList &triangles) {
	// path is a dot
	if (first->next == first)
		return true;

	// path is a two lines
	if (first->next->next == first)
		return true;

	// path is triangle
	if (first->next->next->next == first) {
		triangles.push_back(first->index);
		triangles.push_back(first->next->index);
		triangles.push_back(first->next->next->index);
		return true;
	}

	return false;
}

void Triangulator::split_path(Vertex *first, TriangleList &triangles) {
	if (check_triangle(first, triangles))
		return;

	// split path
	Vertex *va0 = first;
	for(Vertex *va1 = va0->next; va1 != first; va1 = va1->next) {
		if (va0 != va1 && va0->next != va1 && va1->next != va0) {
			bool intersects = false;
			for(Vertex *vb0 = first->next; vb0->next != first; vb0 = vb0->next) {
				Vertex *vb1 = vb0->next;
				if ( va0 != vb0 && va0 != vb1 && va1 != vb0 && va1 != vb1
				  && intersect_lines(va0->p, va1->p, vb0->p, vb1->p) )
					{ intersects = true; break; }
			}
			if (!intersects) {
				Vertex *next = va1->next;
				va1->next = va0;
				if (!check_triangle(va0, triangles))
					split_path(va0, triangles);
				va1->next = next;
				va0->next = va1;
				if (check_triangle(va0, triangles))
					return;
				va1 = va0;
			}
		}
	}

	cout << "bug - path not fully triangulated" << endl;
}

void Triangulator::triangulate(const Contour &contour, TriangleList &triangles, int index_offset) {
	Path path;
	build_path(contour, path, index_offset);
	triangles.reserve(triangles.size() + 3*(path.size() - 2));
	split_path(&path.front(), triangles);
}

