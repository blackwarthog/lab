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

#ifndef _CUDARENDER_H_
#define _CUDARENDER_H_

#include <vector>

#include "cudacontext.h"
#include "geometry.h"
#include "contour.h"
#include "swrender.h"


class CudaRender {
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
	CudaContext &cu;
	CUmodule contour_module;
	CUfunction contour_clear_kernel;
	CUfunction contour_path_kernel;
	CUfunction contour_fill_kernel;

	Surface *surface;
	CUdeviceptr points_buffer;
	CUdeviceptr mark_buffer;
	CUdeviceptr surface_image;

public:
	CudaRender(CudaContext &cl);
	~CudaRender();

	void send_surface(Surface *surface);
	Surface* receive_surface();

	void send_points(const vec2f *points, int count);

	void draw(const Path &path);
	void wait();
};


#endif
