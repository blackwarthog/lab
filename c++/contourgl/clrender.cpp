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
	contour_lines_kernel(),
	contour_fill_kernel(),
	surface(),
	rows_buffer(),
	mark_buffer(),
	surface_buffer(),
	rows_count(),
	even_rows_count(),
	odd_rows_count()
{
	contour_program = cl.load_program("contour.cl");
	contour_lines_kernel = clCreateKernel(contour_program, "lines", NULL);
	assert(contour_lines_kernel);
	contour_fill_kernel = clCreateKernel(contour_program, "fill", NULL);
	assert(contour_fill_kernel);
}

ClRender::~ClRender() {
	send_surface(NULL);
	clReleaseKernel(contour_fill_kernel);
	clReleaseKernel(contour_lines_kernel);
	clReleaseProgram(contour_program);
}

void ClRender::send_surface(Surface *surface) {
	if (this->surface == surface) return;

	cl.err = clFinish(cl.queue);
	assert(!cl.err);

	if (this->surface) {
		rows.clear();
		clReleaseMemObject(rows_buffer);
		clReleaseMemObject(mark_buffer);
		clReleaseMemObject(surface_buffer);
	}

	this->surface = surface;

	if (this->surface) {
		//Measure t("ClRender::send_surface");

		rows_count = surface->height;
		even_rows_count = (rows_count+1)/2;
		odd_rows_count = rows_count - even_rows_count;
		rows.resize(rows_count);
		marks.resize(surface->count());

		rows_buffer = clCreateBuffer(
			cl.context, CL_MEM_READ_ONLY,
			rows.size()*sizeof(rows.front()), NULL,
			NULL );
		assert(rows_buffer);

		mark_buffer = clCreateBuffer(
			cl.context, CL_MEM_READ_WRITE,
			marks.size()*sizeof(marks.front()), NULL,
			NULL );
		assert(mark_buffer);

		surface_buffer = clCreateBuffer(
			cl.context, CL_MEM_READ_WRITE,
			surface->data_size(), surface->data,
			NULL );
		assert(surface_buffer);

		cl_event event = NULL;
		cl.err |= clEnqueueWriteBuffer(
			cl.queue, surface_buffer, CL_TRUE,
			0, surface->data_size(), surface->data,
			0, NULL, &event );
		clWaitForEvents(1, &event);

		cl.err |= clFinish(cl.queue);
		assert(!cl.err);
	}
}

Surface* ClRender::receive_surface() {
	if (surface) {
		//Measure t("ClRender::receive_surface");

		cl_event event = NULL;
		cl.err |= clEnqueueReadBuffer(
			cl.queue, surface_buffer, CL_TRUE,
			0, surface->data_size(), surface->data,
			0, NULL, &event );
		assert(!cl.err);
		clWaitForEvents(1, &event);
	}
	return surface;
}


void ClRender::contour(const Contour &contour, const Rect &rect, const Color &color, bool invert, bool evenodd) {
	//Measure t("ClRender::contour");

	Contour transformed, splitted;
	Rect to(1.0, 1.0, surface->width - 1.0, surface->height - 1.0);

	{
		//Measure t("clone");
		transformed = contour;
	}

	{
		//Measure t("transform");
		transformed.transform(rect, to);
	}

	{
		//Measure t("split");
		splitted.allow_split_lines = true;
		transformed.split(splitted, to, Vector(0.5, 0.5));
	}

	vector<line2f> lines;
	vector<line2f> sorted_lines;
	vector<int> line_rows;

	{
		//Measure t("sort lines");

		// reset rows
		for(int i = 0; i < (int)rows_count; ++i)
			rows[i].second = 0;

		// count lines
		Vector prev;
		lines.reserve(splitted.get_chunks().size());
		line_rows.reserve(splitted.get_chunks().size());
		float x0 = (float)to.p0.x;
		float x1 = (float)to.p1.x;
		for(Contour::ChunkList::const_iterator i = splitted.get_chunks().begin(); i != splitted.get_chunks().end(); ++i) {
			if ( i->type == Contour::LINE
			  || i->type == Contour::CLOSE )
			{
				if (i->p1.y > to.p0.y && i->p1.y < to.p1.y) {
					line2f l(vec2f(prev), vec2f(i->p1));
					l.p0.x = min(max(l.p0.x, x0), x1);
					l.p1.x = min(max(l.p1.x, x0), x1);
					assert( (int)floorf(l.p0.x) >= 0 && (int)floorf(l.p0.x) < surface->width
						 && (int)floorf(l.p1.x) >= 0 && (int)floorf(l.p1.x) < surface->width
						 && (int)floorf(l.p0.y) >= 0 && (int)floorf(l.p1.y) < surface->height
						 && (int)floorf(l.p1.y) >= 0 && (int)floorf(l.p1.y) < surface->height
						 && abs((int)floorf(l.p1.x) - (int)floorf(l.p0.x)) <= 1
						 && abs((int)floorf(l.p1.y) - (int)floorf(l.p0.y)) <= 1 );
					int row = (int)floorf(min(l.p0.y, l.p1.y));
					row = row % 2 ? row/2 : even_rows_count + row/2;
					assert(row >= 0 && row < (int)rows_count);
					line_rows.push_back(row);
					lines.push_back(l);
					++rows[row].second;
				}
			}
			prev = i->p1;
		}

		// calc rows offsets
		int lines_count = (int)lines.size();
		rows[0].first = rows[0].second;
		for(int i = 1; i < (int)rows_count; ++i)
			rows[i].first = rows[i-1].first + rows[i].second;

		// make sorted list
		sorted_lines.resize(lines_count);
		for(int i = 0; i < lines_count; ++i) {
			assert(rows[line_rows[i]].first > 0 && rows[line_rows[i]].first <= lines_count);
			sorted_lines[ --rows[line_rows[i]].first ] = lines[i];
		}
	}

	if (sorted_lines.empty()) return;

	cl_mem lines_buffer = NULL;

	{
		//Measure t("create lines buffer");

		lines_buffer = clCreateBuffer(
			cl.context, CL_MEM_READ_ONLY,
			sorted_lines.size()*sizeof(sorted_lines.front()), NULL,
			NULL );
		assert(lines_buffer);
	}

	{
		//Measure t("enqueue commands");

		clFinish(cl.queue);

		// kernel args
		int width = surface->width;

		cl.err |= clSetKernelArg(contour_lines_kernel, 0, sizeof(width), &width);
		cl.err |= clSetKernelArg(contour_lines_kernel, 1, sizeof(lines_buffer), &lines_buffer);
		cl.err |= clSetKernelArg(contour_lines_kernel, 2, sizeof(rows_buffer), &rows_buffer);
		cl.err |= clSetKernelArg(contour_lines_kernel, 3, sizeof(mark_buffer), &mark_buffer);
		assert(!cl.err);

		int iinvert = invert, ievenodd = evenodd;
		cl.err |= clSetKernelArg(contour_fill_kernel, 0, sizeof(width), &width);
		cl.err |= clSetKernelArg(contour_fill_kernel, 1, sizeof(mark_buffer), &mark_buffer);
		cl.err |= clSetKernelArg(contour_fill_kernel, 2, sizeof(surface_buffer), &surface_buffer);
		cl.err |= clSetKernelArg(contour_fill_kernel, 3, sizeof(Color::type), &color.r);
		cl.err |= clSetKernelArg(contour_fill_kernel, 4, sizeof(Color::type), &color.g);
		cl.err |= clSetKernelArg(contour_fill_kernel, 5, sizeof(Color::type), &color.b);
		cl.err |= clSetKernelArg(contour_fill_kernel, 6, sizeof(Color::type), &color.a);
		cl.err |= clSetKernelArg(contour_fill_kernel, 7, sizeof(int), &iinvert);
		cl.err |= clSetKernelArg(contour_fill_kernel, 8, sizeof(int), &ievenodd);
		assert(!cl.err);

		// prepare buffers

		cl_event prepare_buffers_events[3] = { };

		cl.err |= clEnqueueWriteBuffer(
			cl.queue, lines_buffer, CL_TRUE,
			0, sorted_lines.size()*sizeof(sorted_lines.front()), &sorted_lines.front(),
			0, NULL, &prepare_buffers_events[0] );
		assert(!cl.err);

		cl.err |= clEnqueueWriteBuffer(
			cl.queue, rows_buffer, CL_TRUE,
			0, rows.size()*sizeof(rows.front()), &rows.front(),
			0, NULL, &prepare_buffers_events[1] );
		assert(!cl.err);

		cl.err |= clEnqueueWriteBuffer(
			cl.queue, mark_buffer, CL_TRUE,
			0, marks.size()*sizeof(marks.front()), &marks.front(),
			0, NULL, &prepare_buffers_events[2] );
		assert(!cl.err);

		// run kernels

		cl_event lines_odd_event = NULL;
		cl.err |= clEnqueueNDRangeKernel(
			cl.queue,
			contour_lines_kernel,
			1,
			NULL,
			&even_rows_count,
			NULL,
			3,
			prepare_buffers_events,
			&lines_odd_event );
		assert(!cl.err);

		cl_event lines_even_event = NULL;
		cl.err |= clEnqueueNDRangeKernel(
			cl.queue,
			contour_lines_kernel,
			1,
			&even_rows_count,
			&odd_rows_count,
			NULL,
			1,
			&lines_odd_event,
			&lines_even_event );
		assert(!cl.err);

		cl_event fill_event = NULL;
		cl.err |= clEnqueueNDRangeKernel(
			cl.queue,
			contour_fill_kernel,
			1,
			NULL,
			&rows_count,
			NULL,
			1,
			&lines_even_event,
			&fill_event );
		assert(!cl.err);

		clWaitForEvents(1, &fill_event);
	}

	{
		//Measure t("release lines buffer");
		clReleaseMemObject(lines_buffer);
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
