/*
    ......... 2016 Ivan Mahonin

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

#ifndef _SURFACE_H_
#define _SURFACE_H_

#include <cassert>

#include <string>

class Surface {
public:
	typedef unsigned int Color;

	Color* const buffer;
	const int width;
	const int height;

	Color color;
	int x, y;
	bool blending;

	Surface(int width, int height);
	~Surface();

	static unsigned int convert_channel(double c);
	static unsigned int blend_channel(unsigned int a, unsigned int dest, unsigned int src);
	static Color convert_color(double r, double g, double b, double a = 1.0);
	static Color blend(Color dest, Color src);

	bool is_inside(int x, int y) const
		{ return x >= 0 && x < width && y >= 0 && y < height; }

	Color& point(int x, int y)
		{ assert(is_inside(x, y)); return buffer[y*width + x]; }
	const Color& point(int x, int y) const
		{ assert(is_inside(x, y)); return buffer[y*width + x]; }

	void set_color(Color color)
		{ this->color = color; }
	void set_color(double r, double g, double b, double a = 1.0)
		{ set_color(convert_color(r, g, b, a)); }
	void set_blending(bool blending) { this->blending = blending; }

	void line(Color color, int x0, int y0, int x1, int y1, bool blending = true);
	void rect(Color color, int x0, int y0, int x1, int y1, bool blending = true);

	void move_to(int x, int y) { this->x = x; this->y = y; }
	void line_to(int x, int y) { line(color, this->x, this->y, x, y, blending); move_to(x, y); };
	void rect_to(int x, int y) { rect(color, this->x, this->y, x, y, blending); move_to(x, y); };

	void move_by(int x, int y) { move_to(this->x + x, this->y + y); }
	void line_by(int x, int y) { line_to(this->x + x, this->y + y); }
	void rect_by(int x, int y) { rect_to(this->x + x, this->y + y); }

	void clear(Color color = 0) { rect(color, 0, 0, width, height, false); }
	void clear(double r, double g, double b, double a) { clear(convert_color(r, g, b, a)); }

	void set_point(int x, int y, unsigned int color)
		{ if (is_inside(x, y)) point(x, y) = color; }
	void blend_point(int x, int y, unsigned int color) {
		if (is_inside(x, y)) {
			Color& p = point(x, y);
			p = blend(p, color);
		}
	}

	void save(const std::string &filename) const;
};

#endif
