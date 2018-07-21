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
	contour_draw_workgroup_size(),
	surface(),
	paths_buffer(),
	mark_buffer(),
	surface_image(),
	prev_event()
{
	contour_program = cl.load_program("contour-fs.cl");
	assert(contour_program);

	contour_draw_kernel = clCreateKernel(contour_program, "draw", NULL);
	assert(contour_draw_kernel);

	cl.err |= clGetKernelWorkGroupInfo(
		contour_draw_kernel,
		cl.device,
		CL_KERNEL_WORK_GROUP_SIZE,
		sizeof(contour_draw_workgroup_size),
		&contour_draw_workgroup_size,
		NULL );
	assert(!cl.err);
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
			(surface->count() + 2)*sizeof(cl_int2), NULL,
			&cl.err );
		assert(!cl.err);
		assert(mark_buffer);

		char zero = 0;
		cl.err |= clEnqueueFillBuffer(
			cl.queue, mark_buffer,
			&zero, 1,
			0, surface->count()*sizeof(cl_int2),
			0, NULL, NULL );
		assert(!cl.err);

		surface_image = clCreateBuffer(
			cl.context, CL_MEM_READ_WRITE | CL_MEM_COPY_HOST_PTR,
			surface->count()*sizeof(Color), surface->data,
			&cl.err );
		assert(!cl.err);
		assert(surface_image);

		cl.err |= clSetKernelArg(contour_draw_kernel, 0, sizeof(surface->width), &surface->width);
		cl.err |= clSetKernelArg(contour_draw_kernel, 1, sizeof(surface->width), &surface->height);
		cl.err |= clSetKernelArg(contour_draw_kernel, 2, sizeof(mark_buffer), &mark_buffer);
		cl.err |= clSetKernelArg(contour_draw_kernel, 3, sizeof(surface_image), &surface_image);
		assert(!cl.err);

		cl.err |= clFinish(cl.queue);
		assert(!cl.err);
	}
}

Surface* ClRender::receive_surface() {
	if (surface) {
		//Measure t("ClRender::receive_surface");

		cl.err |= clEnqueueReadBuffer(
			cl.queue, surface_image, CL_FALSE,
			0, surface->count()*sizeof(Color), surface->data,
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

void ClRender::remove_paths() {
	if (paths_buffer) {
		cl.err |= clFinish(cl.queue);
		assert(!cl.err);
		prev_event = NULL;

		clReleaseMemObject(paths_buffer);
		paths_buffer = NULL;
	}
}

void ClRender::send_paths(const void *paths, int size) {
	if (!paths_buffer && (!paths || size <= 0)) return;

	remove_paths();

	if (paths && size > 0) {
		//Measure t("ClRender::send_path");

		paths_buffer = clCreateBuffer(
			cl.context, CL_MEM_READ_ONLY | CL_MEM_COPY_HOST_PTR,
			size, const_cast<void*>(paths),
			&cl.err );
		assert(!cl.err);
		assert(paths_buffer);

		cl.err |= clSetKernelArg(contour_draw_kernel, 4, sizeof(paths_buffer), &paths_buffer);
		assert(!cl.err);
	}
}

void ClRender::draw() {
	//Measure t("ClRender::contour");

	cl_event event = prev_event;
	size_t count = contour_draw_workgroup_size;
	size_t group_size = count;
	cl.err |= clEnqueueNDRangeKernel(
		cl.queue,
		contour_draw_kernel,
		1,
		NULL,
		&count,
		&group_size,
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


// ------------------------------------------------


ClRender2::ClRender2(ClContext &cl):
	cl(cl),
	contour_program(),
	contour_reset_kernel(),
	contour_paths_kernel(),
	contour_draw_kernel(),
	surface(),
	points_count(),
	paths_buffer(),
	points_buffer(),
	samples_buffer(),
	surface_image(),
	prev_event()
{
	contour_program = cl.load_program("contour-sort.cl");
	assert(contour_program);

	contour_reset_kernel = clCreateKernel(contour_program, "reset", &cl.err);
	assert(!cl.err);
	assert(contour_reset_kernel);

	contour_paths_kernel = clCreateKernel(contour_program, "paths", &cl.err);
	assert(!cl.err);
	assert(contour_paths_kernel);

	contour_draw_kernel = clCreateKernel(contour_program, "draw", &cl.err);
	assert(!cl.err);
	assert(contour_draw_kernel);

	samples_buffer = clCreateBuffer(
		cl.context, CL_MEM_READ_WRITE,
		1024*1024*1024, NULL,
		&cl.err );
	assert(!cl.err);
	assert(samples_buffer);

	cl.err |= clSetKernelArg(contour_reset_kernel, 0, sizeof(samples_buffer), &samples_buffer);
	cl.err |= clSetKernelArg(contour_paths_kernel, 2, sizeof(samples_buffer), &samples_buffer);
	cl.err |= clSetKernelArg(contour_draw_kernel, 2, sizeof(samples_buffer), &samples_buffer);
	assert(!cl.err);
}

ClRender2::~ClRender2() {
	remove_paths();
	remove_surface();

	cl.err |= clReleaseMemObject(samples_buffer);
	assert(!cl.err);
	samples_buffer = NULL;

	clReleaseKernel(contour_reset_kernel);
	clReleaseKernel(contour_paths_kernel);
	clReleaseKernel(contour_draw_kernel);
	clReleaseProgram(contour_program);
}

void ClRender2::remove_surface() {
	wait();

	if (surface) {
		cl.err |= clReleaseMemObject(surface_image);
		assert(!cl.err);
		surface = NULL;
	}
}

void ClRender2::send_surface(Surface *surface) {
	if (!surface && !this->surface) return;

	remove_surface();

	assert(surface);
	this->surface = surface;

	//Measure t("ClRender::send_surface");

	surface_image = clCreateBuffer(
		cl.context, CL_MEM_READ_WRITE,
		surface->count()*sizeof(Color), NULL,
		&cl.err );
	assert(!cl.err);
	assert(surface_image);

	cl.err |= clEnqueueWriteBuffer(
		cl.queue, surface_image, false,
		0, surface->count()*sizeof(Color), surface->data,
		0, NULL, NULL );
	assert(!cl.err);

	cl.err |= clSetKernelArg(contour_paths_kernel, 0, sizeof(surface->width), &surface->width);
	cl.err |= clSetKernelArg(contour_paths_kernel, 1, sizeof(surface->height), &surface->height);
	cl.err |= clSetKernelArg(contour_draw_kernel, 0, sizeof(surface->width), &surface->width);
	cl.err |= clSetKernelArg(contour_draw_kernel, 1, sizeof(surface_image), &surface_image);
	assert(!cl.err);
}

Surface* ClRender2::receive_surface() {
	if (surface) {
		//Measure t("ClRender::receive_surface");

		cl.err |= clEnqueueReadBuffer(
			cl.queue, surface_image, CL_FALSE,
			0, surface->count()*sizeof(Color), surface->data,
			prev_event ? 1 : 0,
			prev_event ? &prev_event : NULL,
			NULL );
		assert(!cl.err);

		wait();
	}
	return surface;
}

void ClRender2::remove_paths() {
	wait();

	if (paths_buffer) {
		cl.err |= clReleaseMemObject(paths_buffer);
		assert(!cl.err);
		paths_buffer = NULL;
	}

	if (points_buffer) {
		cl.err |= clReleaseMemObject(points_buffer);
		assert(!cl.err);
		points_buffer = NULL;
		points_count = 0;
	}
}

void ClRender2::send_paths(const Path *paths, int paths_count, const Point *points, int points_count) {
	remove_paths();

	assert(paths);
	assert(paths_count > 0);

	assert(points);
	assert(points_count > 0);

	paths_buffer = clCreateBuffer(
		cl.context, CL_MEM_READ_ONLY,
		paths_count*sizeof(Path), NULL,
		&cl.err );
	assert(!cl.err);
	assert(paths_buffer);

	cl.err |= clEnqueueWriteBuffer(
		cl.queue, paths_buffer, false,
		0, paths_count*sizeof(Path), paths,
		0, NULL, NULL );
	assert(!cl.err);

	points_buffer = clCreateBuffer(
		cl.context, CL_MEM_READ_ONLY,
		points_count*sizeof(Point), NULL,
		&cl.err );
	assert(!cl.err);
	assert(points_buffer);
	this->points_count = points_count;

	cl.err |= clEnqueueWriteBuffer(
		cl.queue, points_buffer, false,
		0, points_count*sizeof(Point), points,
		0, NULL, NULL );
	assert(!cl.err);

	cl.err |= clSetKernelArg(contour_paths_kernel, 3, sizeof(points_buffer), &points_buffer);
	cl.err |= clSetKernelArg(contour_draw_kernel, 3, sizeof(paths_buffer), &paths_buffer);
	assert(!cl.err);

	wait();
}

void ClRender2::draw() {
	//Measure t("ClRender::contour");

	cl_event prepare_event;
	cl_event paths_event;

	size_t count = surface->height;
	cl.err |= clEnqueueNDRangeKernel(
		cl.queue,
		contour_reset_kernel,
		1,
		NULL,
		&count,
		NULL,
		prev_event ? 1 : 0,
		prev_event ? &prev_event : NULL,
		&prepare_event );
	assert(!cl.err);

	count = points_count - 1;
	cl.err |= clEnqueueNDRangeKernel(
		cl.queue,
		contour_paths_kernel,
		1,
		NULL,
		&count,
		NULL,
		1,
		&prepare_event,
		&paths_event );
	assert(!cl.err);

	count = surface->height;
	cl.err |= clEnqueueNDRangeKernel(
		cl.queue,
		contour_draw_kernel,
		1,
		NULL,
		&count,
		NULL,
		1,
		&paths_event,
		&prev_event );
	assert(!cl.err);
}

void ClRender2::wait() {
	cl.err |= clFinish(cl.queue);
	assert(!cl.err);
	prev_event = NULL;
}


// ------------------------------------------------


ClRender3::ClRender3(ClContext &cl):
	cl(cl),
	contour_program(),
	contour_clear_kernel(),
	contour_path_kernel(),
	contour_fill_kernel(),
	surface(),
	points_buffer(),
	mark_buffer(),
	surface_image(),
	prev_event()
{
	contour_program = cl.load_program("contour-base.cl");
	assert(contour_program);

	contour_clear_kernel = clCreateKernel(contour_program, "clear", &cl.err);
	assert(!cl.err);
	assert(contour_clear_kernel);

	contour_path_kernel = clCreateKernel(contour_program, "path", &cl.err);
	assert(!cl.err);
	assert(contour_path_kernel);

	contour_fill_kernel = clCreateKernel(contour_program, "fill", &cl.err);
	assert(!cl.err);
	assert(contour_fill_kernel);
}

ClRender3::~ClRender3() {
	send_points(NULL, 0);
	send_surface(NULL);

	clReleaseKernel(contour_path_kernel);
	clReleaseKernel(contour_fill_kernel);
	clReleaseKernel(contour_clear_kernel);
	clReleaseProgram(contour_program);
}

void ClRender3::send_surface(Surface *surface) {
	if (this->surface) {
		wait();
		cl.err |= clReleaseMemObject(surface_image);
		assert(!cl.err);
		surface_image = NULL;
	}

	this->surface = surface;

	if (this->surface) {
		//Measure t("ClRender::send_surface");

		int zero_mark[4] = { };

		surface_image = clCreateBuffer(
			cl.context, CL_MEM_READ_WRITE,
			surface->count()*sizeof(Color), NULL,
			&cl.err );
		assert(!cl.err);
		assert(surface_image);

		mark_buffer = clCreateBuffer(
			cl.context, CL_MEM_READ_WRITE,
			surface->count()*sizeof(zero_mark), NULL,
			&cl.err );
		assert(!cl.err);
		assert(mark_buffer);

		cl.err |= clEnqueueWriteBuffer(
			cl.queue, surface_image, false,
			0, surface->count()*sizeof(Color), surface->data,
			0, NULL, NULL );
		assert(!cl.err);

		cl.err |= clSetKernelArg(contour_clear_kernel, 0, sizeof(surface->width), &surface->width);
		cl.err |= clSetKernelArg(contour_clear_kernel, 1, sizeof(surface->height), &surface->height);
		cl.err |= clSetKernelArg(contour_clear_kernel, 2, sizeof(mark_buffer), &mark_buffer);
		assert(!cl.err);

		size_t count = surface->count();
		cl.err |= clEnqueueNDRangeKernel(
			cl.queue, contour_clear_kernel,
			1, NULL, &count, NULL,
			0, NULL, NULL );
		assert(!cl.err);

		cl.err |= clSetKernelArg(contour_path_kernel, 0, sizeof(surface->width), &surface->width);
		cl.err |= clSetKernelArg(contour_path_kernel, 1, sizeof(surface->height), &surface->height);
		cl.err |= clSetKernelArg(contour_path_kernel, 2, sizeof(mark_buffer), &mark_buffer);
		assert(!cl.err);

		cl.err |= clSetKernelArg(contour_fill_kernel, 0, sizeof(surface->width), &surface->width);
		cl.err |= clSetKernelArg(contour_fill_kernel, 1, sizeof(surface->height), &surface->height);
		cl.err |= clSetKernelArg(contour_fill_kernel, 2, sizeof(mark_buffer), &mark_buffer);
		cl.err |= clSetKernelArg(contour_fill_kernel, 3, sizeof(surface_image), &surface_image);
		assert(!cl.err);

		wait();
	}
}

Surface* ClRender3::receive_surface() {
	if (surface) {
		//Measure t("ClRender::receive_surface");

		cl.err |= clEnqueueReadBuffer(
			cl.queue, surface_image, CL_FALSE,
			0, surface->count()*sizeof(Color), surface->data,
			prev_event ? 1 : 0,
			prev_event ? &prev_event : NULL,
			NULL );
		assert(!cl.err);

		wait();
	}
	return surface;
}

void ClRender3::send_points(const vec2f *points, int count) {
	if (points_buffer) {
		wait();
		cl.err |= clReleaseMemObject(points_buffer);
		assert(!cl.err);
		points_buffer = NULL;
	}

	if (points && count > 0) {
		points_buffer = clCreateBuffer(
			cl.context, CL_MEM_READ_ONLY,
			count*sizeof(vec2f), NULL,
			&cl.err );
		assert(!cl.err);
		assert(points_buffer);

		cl.err |= clEnqueueWriteBuffer(
			cl.queue, points_buffer, false,
			0, count*sizeof(vec2f), points,
			0, NULL, NULL );
		assert(!cl.err);

		cl.err |= clSetKernelArg(contour_path_kernel, 3, sizeof(points_buffer), &points_buffer);
		assert(!cl.err);

		wait();
	}
}

void ClRender3::draw(const Path &path) {
	//Measure t("ClRender::contour");

	assert(surface);
	assert(points_buffer);

	ContextRect bounds;
	bounds.minx = max(1, path.bounds.minx);
	bounds.maxx = min(surface->width, path.bounds.maxx);
	bounds.miny = max(0, path.bounds.miny);
	bounds.maxy = min(surface->height, path.bounds.maxy);
	int invert_int  = path.invert  ? 1 : 0;
	int evenodd_int = path.evenodd ? 1 : 0;
	if ( bounds.minx >= bounds.maxx
	  || bounds.miny >= bounds.maxy
	  || path.begin >= path.end ) return;

	cl.err |= clSetKernelArg(contour_path_kernel, 4, sizeof(path.begin), &path.begin);
	cl.err |= clSetKernelArg(contour_path_kernel, 5, sizeof(path.end), &path.end);
	cl.err |= clSetKernelArg(contour_path_kernel, 6, sizeof(bounds), &bounds);
	assert(!cl.err);

	cl.err |= clSetKernelArg(contour_fill_kernel, 1, sizeof(bounds.maxy), &bounds.maxy); // restrict height
	cl.err |= clSetKernelArg(contour_fill_kernel, 4, sizeof(path.color), &path.color);
	cl.err |= clSetKernelArg(contour_fill_kernel, 5, sizeof(bounds), &bounds);
	cl.err |= clSetKernelArg(contour_fill_kernel, 6, sizeof(invert_int), &invert_int);
	cl.err |= clSetKernelArg(contour_fill_kernel, 7, sizeof(evenodd_int), &evenodd_int);
	assert(!cl.err);


	cl_event path_event;

	size_t group_size = 1;

	size_t offset = path.begin;
	size_t count = ((path.end - path.begin - 1)/group_size + 1)*group_size;
	cl.err |= clEnqueueNDRangeKernel(
		cl.queue,
		contour_path_kernel,
		1,
		&offset,
		&count,
		NULL,//&group_size,
		prev_event ? 1 : 0,
		prev_event ? &prev_event : NULL,
		&path_event );
	assert(!cl.err);

	offset = bounds.miny;
	count = ((bounds.maxy - bounds.miny - 1)/group_size + 1)*group_size;
	cl.err |= clEnqueueNDRangeKernel(
		cl.queue,
		contour_fill_kernel,
		1,
		&offset,
		&count,
		NULL,//&group_size,
		1,
		&path_event,
		&prev_event );
	assert(!cl.err);
}

void ClRender3::wait() {
	cl.err |= clFinish(cl.queue);
	assert(!cl.err);
	prev_event = NULL;
}

