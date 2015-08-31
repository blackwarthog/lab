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

#include "test.h"
#include "contourbuilder.h"
#include "triangulator.h"
#include "measure.h"
#include "utils.h"
#include "clrender.h"


using namespace std;


void Test::draw_contour(int start, int count, bool even_odd, bool invert, const Color &color) {
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

void Test::load(std::vector<ContourInfo> &contours, const std::string &filename) {
	vector<Vector> groups;
	groups.push_back(Vector());

	ifstream f("data/contours.txt");
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


void Test::test2() {
	Contour c, cc;
	ContourBuilder::build(cc);
	cout << cc.get_chunks().size() << " commands" << endl;

	Vector frame_size = Utils::get_frame_size();

	Rect bounds;
	bounds.p0 = Vector(-1.0, -1.0);
	bounds.p1 = Vector( 1.0,  1.0);
	Vector min_size(1.75/frame_size.x, 1.75/frame_size.y);

	{
		Measure t("test_2_split");
		cc.split(c, bounds, min_size);
	}

	const Contour::ChunkList &chunks = c.get_chunks();
	cout << chunks.size() << " vertices" << endl;

	GLuint buffer_id = 0;
	GLuint array_id = 0;
	int count = 0;
	vector<vec2f> vertices;

	{
		Measure t("test_2_init_buffer");
		vertices.resize(4+4*chunks.size());
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

		vertices.clear();
		vertices.reserve(4+4*chunks.size());
	}

	{
		Measure t("test_2_prepare_data");
		vertices.push_back(vec2f(bounds.p0.x, bounds.p0.y));
		vertices.push_back(vec2f(bounds.p0.x, bounds.p1.y));
		vertices.push_back(vec2f(bounds.p1.x, bounds.p0.y));
		vertices.push_back(vec2f(bounds.p1.x, bounds.p1.y));
		vertices.push_back(vec2f());
		vertices.push_back(vec2f());
		for(Contour::ChunkList::const_iterator i = chunks.begin(); i != chunks.end(); ++i) {
			if ( i->type == Contour::LINE
			  || i->type == Contour::CLOSE)
			{
				vertices.push_back(vec2f(i->p1));
				vertices.push_back(vec2f(-1.f, (float)i->p1.y));
			} else {
				vertices.push_back(vertices.back());
				vertices.push_back(vertices.back());
				vertices.push_back(vec2f(i->p1));
				vertices.push_back(vertices.back());
			}
		}
		count = vertices.size() - 4;
	}

	{
		Measure t("test_2_send_data");
		glBufferSubData( GL_ARRAY_BUFFER,
						 0,
					     vertices.size()*sizeof(vertices.front()),
					     &vertices.front() );
	}

	{
		Measure t("test_2_simple_fill.tga");
		glDrawArrays(GL_TRIANGLE_STRIP, 0, 4);
	}

	{
		Measure t("test_2_array.tga");
		glColorMask(GL_FALSE, GL_FALSE, GL_FALSE, GL_FALSE);
		glDrawArrays(GL_TRIANGLE_STRIP, 4, count);
		glColorMask(GL_TRUE, GL_TRUE, GL_TRUE, GL_TRUE);
	}

	{
		Measure t("test_2_contour_fill.tga");
		draw_contour(4, count, false, false, Color(0.f, 0.f, 1.f, 1.f));
	}

	{
		Measure t("test_2_contour_fill_invert.tga");
		draw_contour(4, count, false, true, Color(0.f, 0.f, 1.f, 1.f));
	}

	{
		Measure t("test_2_contour_evenodd.tga");
		draw_contour(4, count, true, false, Color(0.f, 0.f, 1.f, 1.f));
	}

	{
		Measure t("test_2_contour_evenodd_invert.tga");
		draw_contour(4, count, true, true, Color(0.f, 0.f, 1.f, 1.f));
	}

	glBindBuffer(GL_ARRAY_BUFFER, 0);
	glDeleteBuffers(1, &buffer_id);
}

void Test::test3() {
	Contour c;
	ContourBuilder::build(c);
	cout << c.get_chunks().size() << " commands" << endl;

	Vector frame_size = Utils::get_frame_size();
	int width = (int)frame_size.x;
	int height = (int)frame_size.y;

	Rect bounds;
	bounds.p0 = Vector(-1.0, -1.0);
	bounds.p1 = Vector( 1.0,  1.0);
	Rect pixel_bounds;
	pixel_bounds.p0 = Vector::zero();
	pixel_bounds.p1 = frame_size;

	c.transform(bounds, pixel_bounds);

	Polyspan polyspan;
	polyspan.init(0, 0, width, height);

	Surface surface(width, height);
	Color color(0.f, 0.f, 1.f, 1.f);

	{
		Measure t("test_3_build_polyspan");
		c.to_polyspan(polyspan);
	}

	cout << polyspan.get_covers().size() << " covers" << endl;

	glPushAttrib(GL_ALL_ATTRIB_BITS);
	glColor4d(0.0, 0.0, 1.0, 1.0);
	{
		Measure t("test_3_polyspan_gl_lines.tga");
		glBegin(GL_LINE_STRIP);
		for(Polyspan::cover_array::const_iterator i = polyspan.get_covers().begin(); i != polyspan.get_covers().end(); ++i)
			glVertex2d((double)i->x/1024.0*2.0 - 1.0, (double)i->y/1024.0*2.0 - 1.0);
		glEnd();
	}
	glPopAttrib();


	{
		Measure t("test_3_polyspan_sort");
		polyspan.sort_marks();
	}

	{
		Measure t("test_3_polyspan_fill.tga", surface);
		SwRender::polyspan(surface, polyspan, color, false, false);
	}

	{
		Measure t("test_3_polyspan_fill_invert.tga", surface);
		SwRender::polyspan(surface, polyspan, color, false, true);
	}

	{
		Measure t("test_3_polyspan_evenodd.tga", surface);
		SwRender::polyspan(surface, polyspan, color, true, false);
	}

	{
		Measure t("test_3_polyspan_evenodd_invert.tga", surface);
		SwRender::polyspan(surface, polyspan, color, true, true);
	}
}

void Test::test4() {
	Vector frame_size = Utils::get_frame_size();
	int width = (int)frame_size.x;
	int height = (int)frame_size.y;

	Rect bounds_gl;
	bounds_gl.p0 = Vector(-1.0, -1.0);
	bounds_gl.p1 = Vector( 1.0,  1.0);

	Rect bounds_sw;
	bounds_sw.p0 = Vector();
	bounds_sw.p1 = frame_size;

	Rect bounds_file;
	bounds_file.p0 = Vector(0.0, 450.0);
	bounds_file.p1 = Vector(500.0, -50.0);

	vector<ContourInfo> contours;
	load(contours, "contours.txt");

	{
		// opengl

		vector<ContourInfo> contours_gl = contours;
		int commands_count = 0;
		for(vector<ContourInfo>::iterator i = contours_gl.begin(); i != contours_gl.end(); ++i) {
			i->contour.transform(bounds_file, bounds_gl);
			commands_count += i->contour.get_chunks().size();
		}

		// gl_stencil

		{
			Measure t("test_4_gl_stencil", true);

			GLuint buffer_id = 0;
			GLuint array_id = 0;
			vector<vec2f> vertices;
			vector<int> starts(contours_gl.size());
			vector<int> counts(contours_gl.size());

			{
				//Measure t("test_4_gl_init_buffer");
				vertices.resize(4 + 4*commands_count + 2*contours_gl.size());
				glGenBuffers(1, &buffer_id);
				glBindBuffer(GL_ARRAY_BUFFER, buffer_id);
				glBufferData( GL_ARRAY_BUFFER,
							  vertices.size()*sizeof(vec2f),
							  &vertices.front(),
							  GL_DYNAMIC_DRAW );
				vertices.clear();
				vertices.reserve(4 + 4*commands_count);

				glGenVertexArrays(1, &array_id);
				glBindVertexArray(array_id);

				glEnableVertexAttribArray(0);
				glVertexAttribPointer(0, 2, GL_FLOAT, GL_TRUE, 0, NULL);

				e.shaders.color(Color(0.f, 0.f, 1.f, 1.f));
				glDrawArrays(GL_TRIANGLE_STRIP, 0, vertices.size());
				glFinish();
				glClear(GL_COLOR_BUFFER_BIT);
				glFinish();
			}

			{
				//Measure t("test_4_gl_stencil_prepare_data");
				vertices.push_back(vec2f(bounds_gl.p0.x, bounds_gl.p0.y));
				vertices.push_back(vec2f(bounds_gl.p0.x, bounds_gl.p1.y));
				vertices.push_back(vec2f(bounds_gl.p1.x, bounds_gl.p0.y));
				vertices.push_back(vec2f(bounds_gl.p1.x, bounds_gl.p1.y));
				for(int i = 0; i < (int)contours_gl.size(); ++i) {
					starts[i] = (int)vertices.size();
					const Contour::ChunkList &chunks = contours_gl[i].contour.get_chunks();
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
			}

			{
				Measure t("test_4_gl_stencil_send_data");
				glBufferSubData( GL_ARRAY_BUFFER,
								 0,
								 vertices.size()*sizeof(vertices.front()),
								 &vertices.front() );
			}

			{
				//Measure t("test_4_gl_stencil_points.tga");
				//glDrawArrays(GL_POINTS, 0, vertices.size());
			}

			{
				Measure t("test_4_gl_stencil.tga");
				for(int i = 0; i < (int)contours_gl.size(); ++i) {
					draw_contour(
						starts[i],
						counts[i],
						contours_gl[i].invert,
						contours_gl[i].evenodd,
						contours_gl[i].color );
				}
			}
		}

		// gl_triangles

		/*
		{
			Measure t("test_4_gl_triangles.tga", true);

			GLuint index_buffer_id = 0;
			vector<int> triangle_starts(contours_gl.size());
			vector<int> triangle_counts(contours_gl.size());
			vector<int> triangles;
			vertices.clear();
			vertices.reserve(commands_count);

			{
				//Measure t("test_4_gl_init_index_buffer");
				triangles.resize(3*commands_count);
				glGenBuffers(1, &index_buffer_id);
				glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, index_buffer_id);
				glBufferData( GL_ELEMENT_ARRAY_BUFFER,
							  triangles.size()*sizeof(triangles.front()),
							  &triangles.front(),
							  GL_DYNAMIC_DRAW );
				triangles.clear();
				triangles.reserve(3*commands_count);
			}

			{
				Measure t("test_4_gl_triangulate");
				int index_offset = 4;
				for(int i = 0; i < (int)contours_gl.size(); ++i) {
					triangle_starts[i] = (int)triangles.size();
					Triangulator::triangulate(contours_gl[i].contour, triangles, index_offset);
					triangle_counts[i] = (int)triangles.size() - triangle_starts[i];
					index_offset += (int)contours_gl[i].contour.get_chunks().size();
				}
			}

			//cout << triangles.size() << " triangles" << endl;

			{
				//Measure t("test_4_gl_triangles_prepare_vertices");
				for(int i = 0; i < (int)contours_gl.size(); ++i) {
					const Contour::ChunkList &chunks = contours_gl[i].contour.get_chunks();
					for(Contour::ChunkList::const_iterator j = chunks.begin(); j != chunks.end(); ++j)
						vertices.push_back(vec2f(j->p1));
				}
			}

			{
				Measure t("test_4_gl_triangles_send_data");
				glBufferSubData( GL_ARRAY_BUFFER,
								 4*sizeof(vertices.front()),
								 vertices.size()*sizeof(vertices.front()),
								 &vertices.front() );
				glBufferSubData( GL_ELEMENT_ARRAY_BUFFER,
								 0,
								 triangles.size()*sizeof(triangles.front()),
								 &triangles.front() );
			}

			{
				Measure t("test_4_gl_triangles_render");
				for(int i = 0; i < (int)contours_gl.size(); ++i) {
					e.shaders.color(contours_gl[i].color);
					glDrawElements(GL_TRIANGLES, triangle_counts[i], GL_UNSIGNED_INT, (char*)NULL + triangle_starts[i]*sizeof(int));
				}
			}
		}
		*/
	}

	{
		// software

		Surface surface(width, height);
		Measure t("test_4_sw.tga", surface, true);

		vector<ContourInfo> contours_sw = contours;
		for(vector<ContourInfo>::iterator i = contours_sw.begin(); i != contours_sw.end(); ++i)
			i->contour.transform(bounds_file, bounds_sw);

		vector<Polyspan> polyspans(contours_sw.size());
		{
			Measure t("test_4_sw_build_polyspans");
			for(int i = 0; i < (int)contours_sw.size(); ++i) {
				polyspans[i].init(0, 0, width, height);
				contours_sw[i].contour.to_polyspan(polyspans[i]);
				polyspans[i].sort_marks();
			}
		}

		{
			Measure t("test_4_sw_render_polyspans");
			for(int i = 0; i < (int)contours_sw.size(); ++i)
				SwRender::polyspan(surface, polyspans[i], contours_sw[i].color, contours_sw[i].evenodd, contours_sw[i].invert);
		}
	}

	{
		// cl

		Surface surface(width, height);
		vector<ContourInfo> contours_cl = contours;
		ClRender clr(e.cl);
		{
			Measure t("test_4_cl.tga", surface, true);
			clr.send_surface(&surface);
			for(vector<ContourInfo>::const_iterator i = contours_cl.begin(); i != contours_cl.end(); ++i)
				clr.contour(i->contour, bounds_file, i->color, i->invert, i->evenodd);
			clr.receive_surface();
		}
	}
}
