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

	Environment &e;

	Test(Environment &e): e(e) { }
	~Test() { }

	void draw_contour(int start, int count, bool even_odd, bool invert, const Color &color);
	void load(std::vector<ContourInfo> &contours, const std::string &filename);

	void test1();
	void test2();
	void test3();
	void test4();
};

#endif
