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
	contour_program = cl.load_program("contour.cl");
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
	cl.err |= clEnqueueNDRangeKernel(
		cl.queue,
		contour_draw_kernel,
		1,
		NULL,
		&count,
		&count,
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

