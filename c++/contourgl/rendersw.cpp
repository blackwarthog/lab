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

#include "rendersw.h"


using namespace std;


void RenderSW::fill(
	Surface &target,
	const Color &color )
{
	for(Color *i = target.data, *end = i + target.count(); i != end; ++i)
		*i = color;
}

void RenderSW::fill(
	Surface &target,
	const Color &color,
	int left,
	int top,
	int width,
	int height )
{
	int step = target.width - width;
	for(Color *i = &target[top][left], *end = i + height*target.width; i < end; i += step)
		for(Color *rowend = i + width; i < rowend; ++i)
			*i = color;
}

void RenderSW::row(
	Surface &target,
	const Color &color,
	int left,
	int top,
	int length )
{
	for(Color *i = &target[top][left], *end = i + length; i < end; ++i)
		*i = color;
}

void RenderSW::row_alpha(
	Surface &target,
	const Color &color,
	Color::type alpha,
	int left,
	int top,
	int length )
{
	for(Color *i = &target[top][left], *end = i + length; i < end; ++i) {
		i->r = i->r*(1.f - alpha) + color.r*alpha;
		i->g = i->g*(1.f - alpha) + color.g*alpha;
		i->b = i->b*(1.f - alpha) + color.b*alpha;
		i->a = i->a*(1.f - alpha) + color.a*alpha;
	}
}

void RenderSW::polyspan(
	Surface &target,
	const Polyspan &polyspan,
	const Color &color,
	bool evenodd,
	bool invert )
{
	const ContextRect &window = polyspan.get_window();
	const Polyspan::cover_array &covers = polyspan.get_covers();

	Polyspan::cover_array::const_iterator cur_mark = covers.begin();
	Polyspan::cover_array::const_iterator end_mark = covers.end();

	Real cover = 0, area = 0, alpha = 0;
	int	y = 0, x = 0;

	if (cur_mark == end_mark) {
		// no marks at all
		if (invert)
			fill( target, color,
				  window.minx, window.miny,
				  window.maxx - window.minx, window.maxy - window.miny );
		return;
	}

	// fill initial rect / line
	if (invert) {
		// fill all the area above the first vertex
		y = window.miny;
		int l = window.maxx - window.minx;

		fill( target, color,
			  window.minx, window.miny,
			  l, cur_mark->y - window.miny );

		// fill the area to the left of the first vertex on that line
		l = cur_mark->x - window.minx;
		if (l)
			row(target, color, window.minx, cur_mark->y, l);
	}

	while(true) {
		y = cur_mark->y;
		x = cur_mark->x;

		area = cur_mark->area;
		cover += cur_mark->cover;

		// accumulate for the current pixel
		while(++cur_mark != covers.end()) {
			if (y != cur_mark->y || x != cur_mark->x)
				break;
			area += cur_mark->area;
			cover += cur_mark->cover;
		}

		// draw pixel - based on covered area
		if (area) { // if we're ok, draw the current pixel
			alpha = polyspan.extract_alpha(cover - area, evenodd);
			if (invert) alpha = 1 - alpha;
			if (alpha) {
				Color::type a = (Color::type)alpha;
				Color &c = target[y][x];
				c.r = c.r*(1.f - a) + color.r*a;
				c.g = c.g*(1.f - a) + color.g*a;
				c.b = c.b*(1.f - a) + color.b*a;
				c.a = c.a*(1.f - a) + color.a*a;
			}
			++x;
		}

		// if we're done, don't use iterator and exit
		if (cur_mark == end_mark)
			break;

		// if there is no more live pixels on this line, goto next
		if (y != cur_mark->y) {
			if (invert) {
				// fill the area at the end of the line
				row(target, color, x, y, window.maxx - x);

				// fill area at the beginning of the next line
				row(target, color, window.minx, cur_mark->y, cur_mark->x - window.minx);
			}

			cover = 0;
			continue;
		}

		// draw span to next pixel - based on total amount of pixel cover
		if (x < cur_mark->x) {
			alpha = polyspan.extract_alpha(cover, evenodd);
			if (invert) alpha = 1.0 - alpha;
			if (alpha)
				row_alpha(target, color, alpha, x, y, cur_mark->x - x);
		}
	}

	// fill the after stuff
	if (invert) {
		// fill the area at the end of the line
		row(target, color, x, y, window.maxx - x);

		// fill area at the beginning of the next line
		fill( target, color,
			  window.minx, y+1,
			  window.maxx - window.minx, window.maxy - y - 1 );
	}
}
