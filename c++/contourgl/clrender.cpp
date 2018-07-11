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

#include <cassert>

#include <algorithm>
#include <iostream>

#include "clrender.h"
#include "measure.h"


using namespace std;


ClRender::ClRender(ClContext &cl):
	cl(cl),
	contour_program(),
	contour_draw_kernel(),
	surface(),
	paths_buffer(),
	mark_buffer(),
	surface_image(),
	prev_event()
{
	contour_program = cl.load_program("contour.cl");
	contour_draw_kernel = clCreateKernel(contour_program, "draw", NULL);
	assert(contour_draw_kernel);
}

ClRender::~ClRender() {
	send_surface(NULL);
	send_paths(NULL, 0);
	clReleaseKernel(contour_draw_kernel);
	clReleaseProgram(contour_program);
}

void ClRender::send_surface(Surface *surface) {
	if (!surface && !this->surface) return;

	cl.err |= clFinish(cl.queue);
	assert(!cl.err);
	prev_event = NULL;

	if (this->surface) {
		clReleaseMemObject(mark_buffer);
		clReleaseMemObject(surface_image);
	}

	this->surface = surface;

	if (this->surface) {
		//Measure t("ClRender::send_surface");

		mark_buffer = clCreateBuffer(
			cl.context, CL_MEM_READ_WRITE,
			(surface->count() + 2)*sizeof(cl_int2), NULL, // extra two values to store contour bounds
			NULL );
		assert(mark_buffer);

		cl_image_format surface_format = { };
		surface_format.image_channel_order = CL_RGBA;
		surface_format.image_channel_data_type = CL_FLOAT;

		cl_image_desc surface_desc = { };
		surface_desc.image_type = CL_MEM_OBJECT_IMAGE2D;
		surface_desc.image_width = surface->width;
		surface_desc.image_height = surface->height;

		surface_image = clCreateImage(
			cl.context, CL_MEM_READ_WRITE,
			&surface_format, &surface_desc,
			NULL, NULL );
		assert(surface_image);

		size_t origin[3] = { };
		size_t region[3] = { (size_t)surface->width, (size_t)surface->height, 1 };
		cl.err |= clEnqueueWriteImage(
			cl.queue, surface_image, CL_FALSE,
			origin, region, 0, 0, surface->data,
			0, NULL, NULL );
		assert(!cl.err);

		cl.err |= clSetKernelArg(contour_draw_kernel, 1, sizeof(mark_buffer), &mark_buffer);
		cl.err |= clSetKernelArg(contour_draw_kernel, 2, sizeof(surface_image), &surface_image);
		cl.err |= clSetKernelArg(contour_draw_kernel, 3, sizeof(surface_image), &surface_image);
		assert(!cl.err);

		cl.err |= clFinish(cl.queue);
		assert(!cl.err);
	}
}

Surface* ClRender::receive_surface() {
	if (surface) {
		//Measure t("ClRender::receive_surface");

		size_t origin[3] = { };
		size_t region[3] = { (size_t)surface->width, (size_t)surface->height, 1 };
		cl.err |= clEnqueueReadImage(
			cl.queue, surface_image, CL_FALSE,
			origin, region, 0, 0, surface->data,
			prev_event ? 1 : 0,
			prev_event ? &prev_event : NULL,
			NULL );
		assert(!cl.err);

		cl.err |= clFinish(cl.queue);
		assert(!cl.err);
		prev_event = NULL;
	}
	return surface;
}

void ClRender::send_paths(const void *paths, int size) {
	if (!paths_buffer && (!paths || size <= 0)) return;

	cl.err |= clFinish(cl.queue);
	assert(!cl.err);
	prev_event = NULL;

	if (paths_buffer) {
		clReleaseMemObject(paths_buffer);
		paths_buffer = NULL;
	}

	if (paths && size > 0) {
		//Measure t("ClRender::send_path");

		paths_buffer = clCreateBuffer(
			cl.context, CL_MEM_READ_ONLY,
			size, NULL,
			NULL );
		assert(paths_buffer);

		cl.err |= clEnqueueWriteBuffer(
			cl.queue, paths_buffer, CL_FALSE,
			0, size, paths,
			0, NULL, NULL );
		assert(!cl.err);

		cl.err |= clSetKernelArg(contour_draw_kernel, 0, sizeof(paths_buffer), &paths_buffer);
		assert(!cl.err);

		cl.err |= clFinish(cl.queue);
		assert(!cl.err);
	}
}

void ClRender::draw() {
	//Measure t("ClRender::contour");

	cl_event event = prev_event;

	size_t start = 0;
	cl.err |= clEnqueueNDRangeKernel(
		cl.queue,
		contour_draw_kernel,
		1,
		&start,
		&cl.max_group_size,
		&cl.max_group_size,
		event ? 1 : 0,
		event ? &event : NULL,
		&prev_event );
	assert(!cl.err);
}

void ClRender::wait() {
	if (prev_event) {
		cl.err |= clWaitForEvents(1, &prev_event);
		assert(!cl.err);
		prev_event = NULL;
	}
}


void SwRenderAlt::line(const Vector &p0, const Vector &p1) {
	int iy0 = min(max((int)floor(p0.y), 0), height);
	int iy1 = min(max((int)floor(p1.y), 0), height);
	if (iy1 < iy0) swap(iy0, iy1);

	Vector d = p1 - p0;
	Vector k( fabs(d.y) < 1e-6 ? 0.0 : d.x/d.y,
		      fabs(d.x) < 1e-6 ? 0.0 : d.y/d.x );

	for(int r = iy0; r <= iy1; ++r) {
		Real y = (Real)iy0;

		Vector pp0 = p0;
		pp0.y -= y;
		if (pp0.y < 0.0) {
			pp0.y = 0.0;
			pp0.x = p0.x - k.x*y;
		} else
		if (pp0.y > 1.0) {
			pp0.y = 1.0;
			pp0.x = p0.x - k.x*(y - 1.0);
		}

		Vector pp1 = p1;
		pp1.y -= y;
		if (pp1.y < 0.0) {
			pp1.y = 0.0;
			pp1.x = p0.x - k.x*y;
		} else
		if (pp1.y > 1.0) {
			pp1.y = 1.0;
			pp1.x = p0.x - k.x*(y - 1.0);
		}

		int ix0 = min(max((int)floor(pp0.x), 0), width);
		int ix1 = min(max((int)floor(pp1.x), 0), width);
		if (ix1 < ix0) swap(ix0, ix1);
		for(int c = ix0; c <= ix1; ++c) {
			Real x = (Real)ix0;

			Vector ppp0 = pp0;
			ppp0.x -= x;
			if (ppp0.x < 0.0) {
				ppp0.x = 0.0;
				ppp0.y = pp0.y - k.y*x;
			} else
			if (ppp0.x > 1.0) {
				ppp0.x = 1.0;
				ppp0.y = pp0.y - k.y*(x - 1.0);
			}

			Vector ppp1 = pp1;
			ppp1.x -= x;
			if (ppp1.x < 0.0) {
				ppp1.x = 0.0;
				ppp1.y = pp0.y - k.y*x;
			} else
			if (ppp1.x > 1.0) {
				ppp1.x = 1.0;
				ppp1.y = pp0.y - k.y*(x - 1.0);
			}

			Real cover = ppp0.y - ppp1.y;
			Real area = (0.5*(ppp1.x + ppp1.x) - 1.0)*cover;
			(*this)[r][c].add(area, cover);
		}
	}
}
