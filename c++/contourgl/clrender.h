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


class ClRender2 {
public:
	struct Path {
		Color color;
		int invert;
		int evenodd;
		int align0;
		int align1;
	};

	struct Point {
		vec2f coord;
		int path_index;
		int align0;
	};

private:
	ClContext &cl;
	cl_program contour_program;
	cl_kernel contour_reset_kernel;
	cl_kernel contour_paths_kernel;
	cl_kernel contour_draw_kernel;

	Surface *surface;
	int points_count;
	cl_mem paths_buffer;
	cl_mem points_buffer;
	cl_mem samples_buffer;
	cl_mem surface_image;
	cl_event prev_event;

public:
	ClRender2(ClContext &cl);
	~ClRender2();

	void send_surface(Surface *surface);
	Surface* receive_surface();
	void remove_surface();

	void send_paths(const Path *paths, int paths_count, const Point *points, int points_count);
	void remove_paths();

	void draw();
	void wait();
};


class ClRender3 {
public:
	struct Path {
		ContextRect bounds;
		int begin;
		int end;
		Color color;
		bool invert;
		bool evenodd;
	};

private:
	ClContext &cl;
	cl_program contour_program;
	cl_kernel contour_path_kernel;
	cl_kernel contour_fill_kernel;

	Surface *surface;
	cl_mem points_buffer;
	cl_mem mark_buffer;
	cl_mem surface_image;
	cl_event prev_event;

public:
	ClRender3(ClContext &cl);
	~ClRender3();

	void send_surface(Surface *surface);
	Surface* receive_surface();

	void send_points(const vec2f *points, int count);

	void draw(const Path &path);
	void wait();
};


#endif
