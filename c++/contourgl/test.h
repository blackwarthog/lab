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

#ifndef _TEST_H_
#define _TEST_H_

#include <ctime>
#include <string>

#include "contour.h"
#include "environment.h"

class Test {
public:
	struct ContourInfo {
		bool invert;
		bool antialias;
		bool evenodd;
		Color color;
		Contour contour;
		ContourInfo(): invert(), antialias(), evenodd() { }
	};

	typedef std::vector<ContourInfo> Data;

	static void draw_contour(
		Environment &e,
		int start,
		int count,
		const rect<int> bounds,
		bool even_odd,
		bool invert,
		const Color &color );

	static void load(Data &data, const std::string &filename);
	static void transform(Data &data, const Rect &from, const Rect &to);
	static void downgrade(Data &from, Data &to);
	static void split(Data &from, Data &to);

	static void test_gl_stencil(Environment &e, Data &data);
	static void test_sw(Environment &e, Data &data, Surface &surface);
	static void test_cl(Environment &e, Data &data, Surface &surface);
	static void test_cl2(Environment &e, Data &data, Surface &surface);
};

#endif
