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
private:
	ClContext &cl;
	cl_program contour_program;
	cl_kernel contour_draw_kernel;
	size_t contour_draw_workgroup_size;

	Surface *surface;
	cl_mem paths_buffer;
	cl_mem mark_buffer;
	cl_mem surface_image;
	cl_event prev_event;

public:
	ClRender(ClContext &cl);
	~ClRender();

	void send_surface(Surface *surface);
	Surface* receive_surface();
	void send_paths(const void *paths, int size);
	void remove_paths();
	void draw();
	void wait();
};


#endif
