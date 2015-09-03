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

#include <fstream>
#include <iostream>
#include <iomanip>

#include "test.h"
#include "contourbuilder.h"
#include "triangulator.h"
#include "measure.h"
#include "utils.h"
#include "clrender.h"


using namespace std;


void Test::draw_contour(Environment &e, int start, int count, bool even_odd, bool invert, const Color &color) {
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
	glDrawArrays(GL_TRIANGLE_STRIP, start, count);
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
		i->contour.downgrade(to[i - from.begin()].contour, Vector(0.5f, 0.5f));
}

void Test::split(Data &from, Data &to) {
	to = from;
	Measure t("split");
	for(Data::iterator i = from.begin(); i != from.end(); ++i)
		i->contour.split(to[i - from.begin()].contour, Rect(0.f, 0.f, 100000.f, 100000.f), Vector(1.f, 1.f));
}

void Test::test_gl_stencil(Environment &e, Data &data) {
	GLuint buffer_id = 0;
	GLuint array_id = 0;
	vector<vec2f> vertices;
	vector<int> starts(data.size());
	vector<int> counts(data.size());

	vertices.push_back(vec2f(-1.f, -1.f));
	vertices.push_back(vec2f( 1.f, -1.f));
	vertices.push_back(vec2f(-1.f,  1.f));
	vertices.push_back(vec2f( 1.f,  1.f));
	for(int i = 0; i < (int)data.size(); ++i) {
		starts[i] = (int)vertices.size();
		const Contour::ChunkList &chunks = data[i].contour.get_chunks();
		for(Contour::ChunkList::const_iterator j = chunks.begin(); j != chunks.end(); ++j) {
			if (j->type == Contour::LINE) {
				vertices.push_back(vec2f(j->p1));
				vertices.push_back(vec2f(-1.f, (float)j->p1.y));
			} else
			if (j->type == Contour::CLOSE) {
				vertices.push_back(vec2f(j->p1));
				vertices.push_back(vec2f(-1.f, (float)j->p1.y));
			} else {
				vertices.push_back(vertices.back());
				vertices.push_back(vec2f(j->p1));
				vertices.push_back(vertices.back());
				vertices.push_back(vec2f(-1.f, (float)j->p1.y));
			}
		}
		counts[i] = (int)vertices.size() - starts[i];
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
				data[i].invert,
				data[i].evenodd,
				data[i].color );
		}
	}
}

void Test::test_sw(Environment &e, Data &data, Surface &surface) {
	vector<Polyspan> polyspans(data.size());
	{
		Measure t("polyspans");
		for(int i = 0; i < (int)data.size(); ++i) {
			polyspans[i].init(0, 0, surface.width, surface.height);
			data[i].contour.to_polyspan(polyspans[i]);
			polyspans[i].sort_marks();
		}
	}

	{
		Measure t("render");
		for(int i = 0; i < (int)data.size(); ++i)
			SwRender::polyspan(surface, polyspans[i], data[i].color, data[i].evenodd, data[i].invert);
	}
}

void Test::test_cl(Environment &e, Data &data, Surface &surface) {
	vector<vec2f> paths;
	vector<int> starts(data.size());
	vector<int> counts(data.size());
	for(int i = 0; i < (int)data.size(); ++i) {
		starts[i] = paths.size();
		for(Contour::ChunkList::const_iterator j = data[i].contour.get_chunks().begin(); j != data[i].contour.get_chunks().end(); ++j)
			paths.push_back(vec2f(j->p1));
		paths.push_back(paths[starts[i]]);
		counts[i] = paths.size() - starts[i];
	}

	ClRender clr(e.cl);
	clr.send_surface(&surface);
	clr.send_path(&paths.front(), paths.size());

	{
		Measure t("render");
		for(int i = 0; i < (int)data.size(); ++i)
			clr.path(starts[i], counts[i], data[i].color, data[i].invert, data[i].evenodd);
		clr.wait();
	}

	clr.receive_surface();
}
