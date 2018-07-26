/*
    ......... 2015-2018 Ivan Mahonin

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

#include <fstream>
#include <iostream>
#include <iomanip>

#include "test.h"
#include "contourbuilder.h"
#include "triangulator.h"
#include "measure.h"
#include "utils.h"
#include "clrender.h"

#ifdef CUDA
#include "cudarender.h"
#endif

using namespace std;


void Test::draw_contour(
	Environment &e,
	int start,
	int count,
	const rect<int> bounds,
	bool even_odd,
	bool invert,
	const Color &color
) {
	glScissor(bounds.p0.x, bounds.p0.y, bounds.p1.x-bounds.p0.x, bounds.p1.y-bounds.p0.y);
	glEnable(GL_SCISSOR_TEST);
	glEnable(GL_STENCIL_TEST);

	// render mask
	glColorMask(GL_FALSE, GL_FALSE, GL_FALSE, GL_FALSE);
	glClear(GL_STENCIL_BUFFER_BIT);
	glStencilFunc(GL_ALWAYS, 0, 0);
	if (even_odd) {
		glStencilOp(GL_INCR_WRAP, GL_INCR_WRAP, GL_INCR_WRAP);
	} else {
		glStencilOpSeparate(GL_FRONT, GL_INCR_WRAP, GL_INCR_WRAP, GL_INCR_WRAP);
		glStencilOpSeparate(GL_BACK, GL_DECR_WRAP, GL_DECR_WRAP, GL_DECR_WRAP);
	}
	e.shaders.simple();
	glDrawArrays(GL_TRIANGLE_FAN, start, count);
	glColorMask(GL_TRUE, GL_TRUE, GL_TRUE, GL_TRUE);

	// fill mask
	glStencilOp(GL_KEEP, GL_KEEP, GL_KEEP);
	if (!even_odd && !invert)
		glStencilFunc(GL_NOTEQUAL, 0, -1);
	if (!even_odd &&  invert)
		glStencilFunc(GL_EQUAL, 0, -1);
	if ( even_odd && !invert)
		glStencilFunc(GL_EQUAL, 1, 1);
	if ( even_odd &&  invert)
		glStencilFunc(GL_EQUAL, 0, 1);

	e.shaders.color(color);
	glDrawArrays(GL_TRIANGLE_STRIP, 0, 4);

	glDisable(GL_STENCIL_TEST);
	glDisable(GL_SCISSOR_TEST);
}

void Test::load(Data &contours, const std::string &filename) {
	vector<Vector> groups;
	groups.push_back(Vector());

	ifstream f(("data/" + filename).c_str());
	int vertices_count = 0;
	while(f) {
		string s;
		f >> s;
		if (s == "g") {
			Vector t;
			f >> t.x >> t.y;
			groups.push_back(groups.back() + t);
		} else
		if (s == "end") {
			groups.pop_back();
			if ((int)groups.size() == 1)
				break;
		} else
		if (s == "path") {
			contours.push_back(ContourInfo());
			ContourInfo &ci = contours.back();
			f >> ci.invert
			  >> ci.antialias
			  >> ci.evenodd
			  >> ci.color.r
			  >> ci.color.g
			  >> ci.color.b
			  >> ci.color.a;
			bool closed = true;
			while(true) {
				f >> s;
				Vector p1;
				if (s == "M") {
					f >> p1.x >> p1.y;
					ci.contour.move_to(p1 + groups.back());
					closed = false;
				} else
				if (s == "L") {
					f >> p1.x >> p1.y;
					if (closed) {
						ci.contour.move_to(p1 + groups.back());
						closed = false;
					}
					ci.contour.line_to(p1 + groups.back());
				} else
				if (s == "Z") {
					ci.contour.close();
					closed = true;
				} else
				if (s == "end") {
					break;
				} else {
					cout << "bug " << s << endl;
					if (!f) break;
				}
			}
			if (!closed)
				ci.contour.close();
			if (ci.color.a < 0.9999)
				contours.pop_back();
			else
				vertices_count += ci.contour.get_chunks().size();
		} else
		if (s != "") {
			cout << "bug " << s << endl;
		}
	}
	if ((int)groups.size() != 1)
		cout << "bug groups " << groups.size() << endl;

	cout << contours.size() << " contours" << endl;
	cout << vertices_count << " vertices" << endl;
}

void Test::transform(Data &data, const Rect &from, const Rect &to) {
	for(Data::iterator i = data.begin(); i != data.end(); ++i)
		i->contour.transform(from, to);
}

void Test::downgrade(Data &from, Data &to) {
	to = from;
	Measure t("downgrade");
	for(Data::iterator i = from.begin(); i != from.end(); ++i)
		i->contour.downgrade(to[i - from.begin()].contour, Vector(1.f, 1.f));
}

void Test::split(Data &from, Data &to) {
	to = from;
	Measure t("split");
	for(Data::iterator i = from.begin(); i != from.end(); ++i)
		i->contour.split(to[i - from.begin()].contour, Rect(0.f, 0.f, 100000.f, 100000.f), Vector(1.f, 1.f));
}

void Test::test_gl_stencil(Environment &e, Data &data) {
	Vector size = Utils::get_frame_size();
	GLuint buffer_id = 0;
	GLuint array_id = 0;
	vector<vec2f> vertices;
	vector<int> starts(data.size());
	vector<int> counts(data.size());
	vector< rect<int> > bounds(data.size());

	vertices.push_back(vec2f(-1.f, -1.f));
	vertices.push_back(vec2f( 1.f, -1.f));
	vertices.push_back(vec2f(-1.f,  1.f));
	vertices.push_back(vec2f( 1.f,  1.f));
	for(int i = 0; i < (int)data.size(); ++i) {
		starts[i] = (int)vertices.size();
		const Contour::ChunkList &chunks = data[i].contour.get_chunks();
		Rect r(chunks.front().p1, chunks.front().p1);
		for(Contour::ChunkList::const_iterator j = chunks.begin(); j != chunks.end(); ++j) {
			vertices.push_back(vec2f(j->p1));
			r = r.expand(j->p1);
		}
		counts[i] = (int)vertices.size() - starts[i];
		bounds[i].p0.x = (int)floor((r.p0.x + 1.0)*0.5*size.x);
		bounds[i].p0.y = (int)floor((r.p0.y + 1.0)*0.5*size.y);
		bounds[i].p1.x = (int)ceil ((r.p1.x + 1.0)*0.5*size.x) + 1;
		bounds[i].p1.y = (int)ceil ((r.p1.y + 1.0)*0.5*size.y) + 1;
	}

	glGenBuffers(1, &buffer_id);
	glBindBuffer(GL_ARRAY_BUFFER, buffer_id);
	glBufferData( GL_ARRAY_BUFFER,
				  vertices.size()*sizeof(vec2f),
				  &vertices.front(),
				  GL_DYNAMIC_DRAW );

	glGenVertexArrays(1, &array_id);
	glBindVertexArray(array_id);

	glEnableVertexAttribArray(0);
	glVertexAttribPointer(0, 2, GL_FLOAT, GL_TRUE, 0, NULL);

	e.shaders.color(Color(0.f, 0.f, 1.f, 1.f));
	glDrawArrays(GL_TRIANGLE_STRIP, 0, vertices.size());
	glFinish();
	glClear(GL_COLOR_BUFFER_BIT);
	glFinish();

	{
		Measure t("render");
		for(int i = 0; i < (int)data.size(); ++i) {
			draw_contour(
				e,
				starts[i],
				counts[i],
				bounds[i],
				data[i].invert,
				data[i].evenodd,
				data[i].color );
		}
		glFinish();
	}
}

void Test::test_sw(Environment &e, Data &data, Surface &surface) {
	const int warm_up_count = 1000;
	const int measure_count = 1000;
	Surface surface_tmp(surface.width, surface.height);

	// warm-up
	for(int ii = 0; ii < warm_up_count; ++ii) {
		vector<Polyspan> polyspans(data.size());
		for(int i = 0; i < (int)data.size(); ++i) {
			polyspans[i].init(0, 0, surface.width, surface.height);
			data[i].contour.to_polyspan(polyspans[i]);
			polyspans[i].sort_marks();
		}
		for(int i = 0; i < (int)data.size(); ++i)
			SwRender::polyspan(surface_tmp, polyspans[i], data[i].color, data[i].evenodd, data[i].invert);
	}

	// measure
	for(int ii = 0; ii < measure_count; ++ii) {
		Measure t("render", false, true);
		vector<Polyspan> polyspans(data.size());
		for(int i = 0; i < (int)data.size(); ++i) {
			polyspans[i].init(0, 0, surface.width, surface.height);
			data[i].contour.to_polyspan(polyspans[i]);
			polyspans[i].sort_marks();
		}
		for(int i = 0; i < (int)data.size(); ++i)
			SwRender::polyspan(surface_tmp, polyspans[i], data[i].color, data[i].evenodd, data[i].invert);
	}

	{ // draw
		vector<Polyspan> polyspans(data.size());
		for(int i = 0; i < (int)data.size(); ++i) {
			polyspans[i].init(0, 0, surface.width, surface.height);
			data[i].contour.to_polyspan(polyspans[i]);
			polyspans[i].sort_marks();
		}
		for(int i = 0; i < (int)data.size(); ++i)
			SwRender::polyspan(surface, polyspans[i], data[i].color, data[i].evenodd, data[i].invert);
	}
}

void Test::test_cl(Environment &e, Data &data, Surface &surface) {
	// prepare data

	vector<char> paths(sizeof(int));
	int count = 0;
	for(Data::const_iterator i = data.begin(); i != data.end(); ++i)
		if (int points_count = i->contour.get_chunks().size()) {
			++count;

			int flags = 0;
			if (i->invert)  flags |= 1;
			if (i->evenodd) flags |= 2;

			size_t s = paths.size();
			paths.resize(paths.size() + sizeof(int) + sizeof(int) + sizeof(Color) + (points_count+1)*sizeof(vec2f));

			*(int*)&paths[s] = points_count+1; s += sizeof(int);
			*(int*)&paths[s] = flags;          s += sizeof(int);
			*(Color*)&paths[s] = i->color;     s += sizeof(Color);
			vec2f *point = (vec2f*)&paths[s];

			for(Contour::ChunkList::const_iterator j = i->contour.get_chunks().begin(); j != i->contour.get_chunks().end(); ++j, ++point)
				*point = vec2f(j->p1);
			*point = vec2f(i->contour.get_chunks().front().p1);
		}
	*(int*)&paths.front() = count;

	// draw

	ClRender clr(e.cl);
	clr.send_surface(&surface);

	// warm-up
	//clr.send_paths(&paths.front(), paths.size());
	//for(int i = 0; i < 1000; ++i)
	//	clr.draw();
	clr.remove_paths();

	// actual task
	clr.send_surface(&surface);
	{
		Measure t("render");
		clr.send_paths(&paths.front(), paths.size());
		clr.draw();
		clr.wait();
	}
	clr.receive_surface();
}

void Test::test_cl2(Environment &e, Data &data, Surface &surface) {
	// prepare data

	vector<ClRender2::Path> paths;
	vector<ClRender2::Point> points;
	paths.reserve(data.size());
	for(Data::const_iterator i = data.begin(); i != data.end(); ++i)
		if (int points_count = i->contour.get_chunks().size()) {
			ClRender2::Path path;
			path.color = i->color;
			path.invert  = i->invert  ? -1 : 0;
			path.evenodd = i->evenodd ? -1 : 0;
			path.align0 = 0;
			path.align1 = 0;
			paths.push_back(path);

			int first_point_index = (int)points.size();
			int path_index = (int)paths.size() - 1;
			points.reserve(points.size() + points_count + 1);
			for(Contour::ChunkList::const_iterator j = i->contour.get_chunks().begin(); j != i->contour.get_chunks().end(); ++j) {
				ClRender2::Point point;
				point.coord = vec2f(j->p1);
				point.path_index = path_index;
				point.align0 = 0;
				points.push_back(point);
			}
			points.push_back(points[first_point_index]);
		}

	// draw

	ClRender2 clr(e.cl);

	// warm-up
	{
	//clr.send_surface(&surface);
	//clr.send_paths(&paths.front(), (int)paths.size(), &points.front(), (int)points.size());
	//for(int i = 0; i < 1000; ++i)
	//	clr.draw(), clr.wait();
	//clr.remove_paths();
	}

	// actual task
	clr.send_surface(&surface);
	clr.send_paths(&paths.front(), (int)paths.size(), &points.front(), (int)points.size());
	{
		Measure t("render");
		clr.draw();
		clr.wait();
	}
	clr.receive_surface();
}

void Test::test_cl3(Environment &e, Data &data, Surface &surface) {
	// prepare data
	int align = (1024 - 1)/sizeof(vec2f) + 1;
	vector<ClRender3::Path> paths;
	vector<vec2f> points;
	paths.reserve(data.size());
	for(Data::const_iterator i = data.begin(); i != data.end(); ++i) {
		if (!i->contour.get_chunks().empty()) {
			ClRender3::Path path = {};
			path.color = i->color;
			path.invert = i->invert;
			path.evenodd = i->evenodd;

			path.bounds.minx = path.bounds.maxx = (int)floor(i->contour.get_chunks().front().p1.x);
			path.bounds.miny = path.bounds.maxy = (int)floor(i->contour.get_chunks().front().p1.y);
			path.begin = (int)points.size();
			points.reserve(points.size() + i->contour.get_chunks().size() + 1);
			for(Contour::ChunkList::const_iterator j = i->contour.get_chunks().begin(); j != i->contour.get_chunks().end(); ++j) {
				int x = (int)floor(j->p1.x);
				int y = (int)floor(j->p1.y);
				if (path.bounds.minx > x) path.bounds.minx = x;
				if (path.bounds.maxx < x) path.bounds.maxx = x;
				if (path.bounds.miny > y) path.bounds.miny = y;
				if (path.bounds.maxy < y) path.bounds.maxy = y;
				points.push_back(vec2f(j->p1));
			}
			path.end = (int)points.size();
			do { points.push_back( points[path.begin] ); } while(points.size() % align);
			++path.bounds.maxx;
			++path.bounds.maxy;

			paths.push_back(path);
		}
	}

	// draw

	ClRender3 clr(e.cl);

	// warm-up
	clr.send_surface(&surface);
	clr.send_points(&points.front(), (int)points.size());
	for(int ii = 0; ii < 1000; ++ii)
		for(vector<ClRender3::Path>::const_iterator i = paths.begin(); i != paths.end(); ++i)
			clr.draw(*i);
	clr.wait();

	// measure
	{
		for(int ii = 0; ii < 1000; ++ii) {
			Measure t("render", false, true);
			for(vector<ClRender3::Path>::const_iterator i = paths.begin(); i != paths.end(); ++i)
				clr.draw(*i);
			clr.wait();
		}
	}
	clr.send_points(NULL, 0);
	clr.send_surface(NULL);

	// actual task
	clr.send_surface(&surface);
	clr.send_points(&points.front(), (int)points.size());
	{
		for(vector<ClRender3::Path>::const_iterator i = paths.begin(); i != paths.end(); ++i)
			clr.draw(*i);
		clr.wait();
	}
	clr.receive_surface();
}

void Test::test_cu(Environment &e, Data &data, Surface &surface) {
#ifdef CUDA
	// prepare data
	vector<CudaRender::Path> paths;
	vector<vec2f> points;
	paths.reserve(data.size());
	for(Data::const_iterator i = data.begin(); i != data.end(); ++i) {
		if (!i->contour.get_chunks().empty()) {
			CudaRender::Path path = {};
			path.color = i->color;
			path.invert = i->invert;
			path.evenodd = i->evenodd;

			path.bounds.minx = path.bounds.maxx = (int)floor(i->contour.get_chunks().front().p1.x);
			path.bounds.miny = path.bounds.maxy = (int)floor(i->contour.get_chunks().front().p1.y);
			path.begin = (int)points.size();
			points.reserve(points.size() + i->contour.get_chunks().size() + 1);
			for(Contour::ChunkList::const_iterator j = i->contour.get_chunks().begin(); j != i->contour.get_chunks().end(); ++j) {
				int x = (int)floor(j->p1.x);
				int y = (int)floor(j->p1.y);
				if (path.bounds.minx > x) path.bounds.minx = x;
				if (path.bounds.maxx < x) path.bounds.maxx = x;
				if (path.bounds.miny > y) path.bounds.miny = y;
				if (path.bounds.maxy < y) path.bounds.maxy = y;
				points.push_back(vec2f(j->p1));
			}
			path.end = (int)points.size();
			points.push_back( points[path.begin] );
			++path.bounds.maxx;
			++path.bounds.maxy;

			paths.push_back(path);
		}
	}

	// draw

	CudaRender cur(e.cu);

	// warm-up
	cur.send_surface(&surface);
	cur.send_points(&points.front(), (int)points.size());
	for(int ii = 0; ii < 1000; ++ii)
		for(vector<CudaRender::Path>::const_iterator i = paths.begin(); i != paths.end(); ++i)
			cur.draw(*i);
	cur.wait();

	// measure
	{
		for(int ii = 0; ii < 1000; ++ii) {
			Measure t("render", false, true);
			for(vector<CudaRender::Path>::const_iterator i = paths.begin(); i != paths.end(); ++i)
				cur.draw(*i);
			cur.wait();
		}
	}
	cur.send_points(NULL, 0);
	cur.send_surface(NULL);

	// actual task
	cur.send_surface(&surface);
	cur.send_points(&points.front(), (int)points.size());
	{
		for(vector<CudaRender::Path>::const_iterator i = paths.begin(); i != paths.end(); ++i)
			cur.draw(*i);
		cur.wait();
	}
	cur.receive_surface();
#endif
}
