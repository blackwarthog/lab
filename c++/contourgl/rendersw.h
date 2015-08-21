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

#ifndef _RENDERSW_H_
#define _RENDERSW_H_

#include <cstring>

#include "polyspan.h"

class Color {
public:
	typedef float type;
	type r, g, b, a;
	Color(): r(), g(), b(), a() { }
	Color(type r, type g, type b, type a): r(r), g(g), b(b), a(a) { }
	Color(const Color &color, type a): r(color.r), g(color.g), b(color.b), a(color.a*a) { }
};

class Surface {
public:
	const int width, height;
	Color * const data;

	Surface(int width, int height):
		width(width), height(height), data(new Color[width*height])
		{ clear(); }

	~Surface()
		{ delete data; }

	void clear() { memset(data, 0, count()*sizeof(Color)); }
	int count() const { return width*height; }
	Color* operator[] (int row) { return data + row*width; }
	const Color* operator[] (int row) const { return data + row*width; }
};

class RenderSW {
public:
	static void fill(
		Surface &target,
		const Color &color );

	static void fill(
		Surface &target,
		const Color &color,
		int left,
		int top,
		int width,
		int height );

	static void row(
		Surface &target,
		const Color &color,
		int left,
		int top,
		int length );

	static void row_alpha(
		Surface &target,
		const Color &color,
		Color::type alpha,
		int left,
		int top,
		int length );

	static void polyspan(
		Surface &target,
		const Polyspan &polyspan,
		const Color &color,
		bool evenodd,
		bool invert );
};

#endif
