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

#ifndef _RASTERIZER_H_
#define _RASTERIZER_H_

#include <vector>

#include "clcontext.h"
#include "geometry.h"
#include "contour.h"
#include "swrender.h"


class ClRender {
public:
	ClContext &cl;

	ClRender(ClContext &cl);
	~ClRender();

	void contour(const Contour &contour, Surface &target);
};


class SwRenderAlt {
public:
	struct Pixel {
		Real area;
		Real cover;
		Pixel(): area(), cover() { }
		void add(Real area, Real cover) { this->area += area; this->cover += cover; }
	};

private:
	std::vector<Pixel> data;

public:
	const int width;
	const int height;

	SwRenderAlt(int width, int height): data(width*height), width(width), height(height) { }

	Pixel* operator[] (int row) { return &data.front() + row*width; }
	const Pixel* operator[] (int row) const { return &data.front() + row*width; }

	void line(const Vector &p0, const Vector &p1);
};

#endif
