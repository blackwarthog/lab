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
#include "rendersw.h"

class Surface;

clock_t get_clock();

class Test {
public:
	class Wrapper {
	private:
		std::string filename;
		Surface *surface;
		bool tga;
		clock_t t;

		Wrapper(const Wrapper&): surface(), tga(), t() { }
		Wrapper& operator= (const Wrapper&) { return *this; }
	public:
		Wrapper(const std::string &filename);
		Wrapper(const std::string &filename, Surface &surface);
		~Wrapper();
	};

	struct ContourInfo {
		bool invert;
		bool antialias;
		bool evenodd;
		Color color;
		Contour contour;
		ContourInfo(): invert(), antialias(), evenodd() { }
	};

private:
	class Helper;

public:
	static void check_gl(const std::string &s = std::string());

	static void load(std::vector<ContourInfo> &contours, const std::string &filename);

	static void test1();
	static void test2();
	static void test3();

	static void test4();
};

#endif
