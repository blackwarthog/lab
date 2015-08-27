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

#ifndef _TRIANGULATOR_H_
#define _TRIANGULATOR_H_

#include <vector>

#include "geometry.h"
#include "contour.h"

class Triangulator {
public:
	class Vertex {
	public:
		int index;
		Vector p;
		Vertex *next;
		Vertex(): index(), next() { }
	};

	typedef std::vector<Vertex> Path;
	typedef std::vector<int> TriangleList;

	static bool intersect_lines(Vector a0, Vector a1, Vector b0, Vector b1);
	static void build_path(const Contour &contour, Path &path, int index_offset);
	static bool check_triangle(Vertex *first, TriangleList &triangles);
	static void split_path(Vertex *first, TriangleList &triangles);
	static void triangulate(const Contour &contour, TriangleList &triangles, int index_offset);
};

#endif
