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

#include "clrender.h"
#include "measure.h"


using namespace std;


ClRender::ClRender(ClContext &cl):
	cl(cl),
	contour_program(),
	contour_path_kernel(),
	contour_fill_kernel(),
	surface(),
	path_buffer(),
	mark_buffer(),
	surface_image(),
	prev_event()
{
	contour_program = cl.load_program("contour.cl");
	contour_clear_kernel = clCreateKernel(contour_program, "clear", NULL);
	assert(contour_clear_kernel);
	contour_path_kernel = clCreateKernel(contour_program, "path", NULL);
	assert(contour_path_kernel);
	contour_fill_kernel = clCreateKernel(contour_program, "fill", NULL);
	assert(contour_fill_kernel);
}

ClRender::~ClRender() {
	send_surface(NULL);
	send_path(NULL, 0);
	clReleaseKernel(contour_clear_kernel);
	clReleaseKernel(contour_fill_kernel);
	clReleaseKernel(contour_path_kernel);
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

		int width = surface->width;
		int height = surface->height;

		mark_buffer = clCreateBuffer(
			cl.context, CL_MEM_READ_WRITE,
			surface->count()*sizeof(cl_int4), NULL,
			NULL );
		assert(mark_buffer);

		cl.err |= clSetKernelArg(contour_clear_kernel, 0, sizeof(mark_buffer), &mark_buffer);
		cl.err |= clSetKernelArg(contour_clear_kernel, 1, sizeof(width), &width);
		assert(!cl.err);

		size_t pixels_count = (size_t)surface->count();
		cl.err |= clEnqueueNDRangeKernel(
			cl.queue,
			contour_clear_kernel,
			1,
			NULL,
			&pixels_count,
			NULL,
			0,
			NULL,
			NULL );
		assert(!cl.err);

		cl_image_format surface_format = { };
		surface_format.image_channel_order = CL_RGBA;
		surface_format.image_channel_data_type = CL_FLOAT;

		surface_image = clCreateImage2D(
			cl.context, CL_MEM_READ_WRITE,
			&surface_format, surface->width, surface->height,
			0, NULL, NULL );
		assert(surface_image);

		size_t origin[3] = { };
		size_t region[3] = { (size_t)surface->width, (size_t)surface->height, 1 };
		cl.err |= clEnqueueWriteImage(
			cl.queue, surface_image, CL_FALSE,
			origin, region, 0, 0, surface->data,
			0, NULL, NULL );
		assert(!cl.err);

		cl.err |= clSetKernelArg(contour_path_kernel, 0, sizeof(width), &width);
		cl.err |= clSetKernelArg(contour_path_kernel, 1, sizeof(height), &height);
		cl.err |= clSetKernelArg(contour_path_kernel, 2, sizeof(mark_buffer), &mark_buffer);
		assert(!cl.err);

		cl.err |= clSetKernelArg(contour_fill_kernel, 0, sizeof(width), &width);
		cl.err |= clSetKernelArg(contour_fill_kernel, 1, sizeof(mark_buffer), &mark_buffer);
		cl.err |= clSetKernelArg(contour_fill_kernel, 2, sizeof(surface_image), &surface_image);
		cl.err |= clSetKernelArg(contour_fill_kernel, 3, sizeof(surface_image), &surface_image);
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
			prev_event ? 1 : 0, &prev_event, NULL );
		assert(!cl.err);

		cl.err |= clFinish(cl.queue);
		assert(!cl.err);
		prev_event = NULL;
	}
	return surface;
}

void ClRender::send_path(const vec2f *path, int count) {
	if (!path_buffer && (!path || count <=0)) return;

	cl.err |= clFinish(cl.queue);
	assert(!cl.err);
	prev_event = NULL;

	if (path_buffer) {
		clReleaseMemObject(path_buffer);
		path_buffer = NULL;
	}

	if (path && count > 0) {
		//Measure t("ClRender::send_path");

		path_buffer = clCreateBuffer(
			cl.context, CL_MEM_READ_ONLY,
			count*sizeof(*path), NULL,
			NULL );
		assert(path_buffer);

		cl.err |= clEnqueueWriteBuffer(
			cl.queue, path_buffer, CL_FALSE,
			0, count*sizeof(*path), path,
			0, NULL, NULL );
		assert(!cl.err);

		cl.err |= clSetKernelArg(contour_path_kernel, 3, sizeof(path_buffer), &path_buffer);
		assert(!cl.err);

		cl.err |= clFinish(cl.queue);
		assert(!cl.err);
	}
}

void ClRender::path(int start, int count, const Color &color, bool invert, bool evenodd) {
	//Measure t("ClRender::contour");

	if (count <= 1) return;

	// kernel args

	int iinvert = invert, ievenodd = evenodd;
	cl.err |= clSetKernelArg(contour_fill_kernel, 4, sizeof(color), &color);
	cl.err |= clSetKernelArg(contour_fill_kernel, 5, sizeof(int), &iinvert);
	cl.err |= clSetKernelArg(contour_fill_kernel, 6, sizeof(int), &ievenodd);
	assert(!cl.err);

	// build marks

	cl_event path_event = NULL;
	size_t sstart = start;
	size_t scount = count-1;
	cl.err |= clEnqueueNDRangeKernel(
		cl.queue,
		contour_path_kernel,
		1,
		&sstart,
		&scount,
		NULL,
		prev_event ? 1 : 0,
		&prev_event,
		&path_event );
	assert(!cl.err);

	// fill
	size_t sheight = surface->height;
	cl.err |= clEnqueueNDRangeKernel(
		cl.queue,
		contour_fill_kernel,
		1,
		NULL,
		&sheight,
		NULL,
		1,
		&path_event,
		&prev_event );
	assert(!cl.err);
}

void ClRender::wait() {
	if (prev_event) {
		clWaitForEvents(1, &prev_event);
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
