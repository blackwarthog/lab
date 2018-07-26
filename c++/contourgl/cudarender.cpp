/*
    ......... 2018 Ivan Mahonin

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

#include "cudarender.h"
#include "measure.h"


using namespace std;


CudaRender::CudaRender(CudaContext &cu):
	cu(cu),
	contour_module(),
	contour_clear_kernel(),
	contour_path_kernel(),
	contour_fill_kernel(),
	surface(),
	points_buffer(),
	mark_buffer(),
	surface_image()
{
	cu.err = cuModuleLoad(&contour_module, "cuda/contour.ptx");
	assert(!cu.err);

	cu.err = cuModuleGetFunction(&contour_clear_kernel, contour_module, "clear");
	assert(!cu.err);

	cu.err = cuModuleGetFunction(&contour_path_kernel, contour_module, "path");
	assert(!cu.err);

	cu.err = cuModuleGetFunction(&contour_fill_kernel, contour_module, "fill");
	assert(!cu.err);
}

CudaRender::~CudaRender() {
	send_points(NULL, 0);
	send_surface(NULL);

	cu.err = cuModuleUnload(contour_module);
	assert(!cu.err);
}

void CudaRender::send_surface(Surface *surface) {
	if (this->surface) {
		wait();

		cu.err = cuMemFree(surface_image);
		assert(!cu.err);
		surface_image = 0;

		cu.err = cuMemFree(mark_buffer);
		assert(!cu.err);
		mark_buffer = 0;
	}

	this->surface = surface;

	if (this->surface) {
		int zero_mark[4] = { };

		cu.err = cuMemAlloc(&surface_image, surface->data_size());
		assert(!cu.err);

		cu.err = cuMemcpyHtoD(surface_image, surface->data, surface->data_size());
		assert(!cu.err);

		cu.err = cuMemAlloc(&mark_buffer, surface->count()*sizeof(zero_mark));
		assert(!cu.err);

		size_t group_size = 32;
		size_t count = surface->count();
		cu.err = cuLaunchKernel(
			contour_clear_kernel,
			(count - 1)/group_size + 1, 1, 1,
			group_size, 1, 1,
			0, 0, 0,
			CudaParams()
				.add(surface->width)
				.add(surface->height)
				.add(mark_buffer)
				.get_extra() );
		assert(!cu.err);

		wait();
	}
}

Surface* CudaRender::receive_surface() {
	if (surface) {
		wait();
		cu.err = cuMemcpyDtoH(surface->data, surface_image, surface->data_size());
		assert(!cu.err);
	}
	return surface;
}

void CudaRender::send_points(const vec2f *points, int count) {
	if (points_buffer) {
		wait();
		cu.err = cuMemFree(points_buffer);
		assert(!cu.err);
		points_buffer = 0;
	}

	if (points && count > 0) {
		cu.err = cuMemAlloc(&points_buffer, count*sizeof(vec2f));
		assert(!cu.err);

		cu.err = cuMemcpyHtoD(points_buffer, points, count*sizeof(vec2f));
		assert(!cu.err);
	}
}

void CudaRender::draw(const Path &path) {
	assert(surface);
	assert(points_buffer);

	ContextRect bounds;
	bounds.minx = max(1, path.bounds.minx);
	bounds.maxx = min(surface->width, path.bounds.maxx);
	bounds.miny = max(0, path.bounds.miny);
	bounds.maxy = min(surface->height, path.bounds.maxy);
	if ( bounds.minx >= bounds.maxx
	  || bounds.miny >= bounds.maxy
	  || path.begin >= path.end ) return;

	vec2i boundsx(bounds.minx, bounds.maxx);

	size_t group_size, count;

	count = path.end - path.begin;
	group_size = 8;

	count = (count - 1)/group_size + 1;
	cu.err = cuLaunchKernel(
		contour_path_kernel,
		count, 1, 1,
		group_size, 1, 1,
		0, 0, 0,
		CudaParams()
			.add(surface->width)
			.add(surface->height)
			.add(mark_buffer)
			.add(points_buffer)
			.add(path.begin)
			.add(path.end)
			.add(bounds.minx)
			.get_extra() );
	assert(!cu.err);

	count = bounds.maxy - bounds.miny;
	group_size = 1;

	count = (count - 1)/group_size + 1;
	cu.err = cuLaunchKernel(
		contour_fill_kernel,
		count, 1, 1,
		group_size, 1, 1,
		0, 0, 0,
		CudaParams()
			.add(surface->width)
			.add(mark_buffer)
			.add(surface_image)
			.add(path.color, 16)
			.add(bounds, 16)
			.get_extra() );
	assert(!cu.err);
}

void CudaRender::wait() {
	cu.err = cuStreamSynchronize(0);
	assert(!cu.err);
}

