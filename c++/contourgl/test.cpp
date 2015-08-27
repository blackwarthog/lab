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

#include <fstream>
#include <iostream>
#include <iomanip>

#include <GL/gl.h>
#include <GL/glext.h>
#include <GL/glx.h>

#include "test.h"

#include "contour.h"
#include "rendersw.h"
#include "contourbuilder.h"
#include "shaders.h"
#include "triangulator.h"


using namespace std;


class Test::Helper {
public:
	static void save_rgba(
		const void *buffer,
		int width,
		int height,
		bool flip,
		const string &filename )
	{
		// create file
		ofstream f(("results/" + filename).c_str(), ofstream::out | ofstream::trunc | ofstream::binary);

		// write header
		unsigned char targa_header[] = {
			0,    // Length of the image ID field (0 - no ID field)
			0,    // Whether a color map is included (0 - no colormap)
			2,    // Compression and color types (2 - uncompressed true-color image)
			0, 0, 0, 0, 0, // Color map specification (not need for us)
			0, 0, // X-origin
			0, 0, // Y-origin
			(unsigned char)(width & 0xff), // Image width
			(unsigned char)(width >> 8),
			(unsigned char)(height & 0xff), // Image height
			(unsigned char)(height >> 8),
			32,   // Bits per pixel
			0     // Image descriptor (keep zero for capability)
		};
		f.write((char*)targa_header, sizeof(targa_header));

		// write data
		if (flip) {
			int line_size = 4*width;
			const char *end = (char*)buffer;
			const char *current = end + height*line_size;
			while(current > end) {
				current -= line_size;
				f.write(current, line_size);
			}
		} else {
			f.write((const char*)buffer, 4*width*height);
		}
	}

	static void save_viewport(const string &filename) {
		glFinish();

		GLint  vp[4] = {};
		glGetIntegerv(GL_VIEWPORT, vp);

		GLint draw_buffer = 0, read_buffer = 0;
		glGetIntegerv(GL_DRAW_FRAMEBUFFER_BINDING, &draw_buffer);
		glGetIntegerv(GL_READ_FRAMEBUFFER_BINDING, &read_buffer);
		if (draw_buffer != read_buffer) {
			glBindFramebuffer(GL_DRAW_FRAMEBUFFER, (GLuint)read_buffer);
			glBindFramebuffer(GL_READ_FRAMEBUFFER, (GLuint)draw_buffer);
			glBlitFramebuffer(vp[0], vp[1], vp[2], vp[3], vp[0], vp[1], vp[2], vp[3], GL_COLOR_BUFFER_BIT, GL_NEAREST);
			glFinish();
			glBindFramebuffer(GL_DRAW_FRAMEBUFFER, (GLuint)draw_buffer);
			glBindFramebuffer(GL_READ_FRAMEBUFFER, (GLuint)read_buffer);
		}

		char *buffer = new char[vp[2]*vp[3]*4];
		glReadPixels(vp[0], vp[1], vp[2], vp[3], GL_BGRA, GL_UNSIGNED_BYTE, buffer);


		save_rgba(buffer, vp[2], vp[3], false, filename);
		delete buffer;
	}

	static void save_surface(const Surface &surface, const string &filename) {
		unsigned char *buffer = new unsigned char[4*surface.count()];
		unsigned char *j = buffer;
		for(Color *i = surface.data, *end = i + surface.count(); i != end; ++i, j += 4) {
			j[0] = (unsigned char)roundf(max(0.f, min(1.f, i->b))*255.f);
			j[1] = (unsigned char)roundf(max(0.f, min(1.f, i->g))*255.f);
			j[2] = (unsigned char)roundf(max(0.f, min(1.f, i->r))*255.f);
			j[3] = (unsigned char)roundf(max(0.f, min(1.f, i->a))*255.f);
		}
		save_rgba(buffer, surface.width, surface.height, false, filename);
		delete buffer;
	}

	static void draw_contour_strip(const vector<Vector> &c) {
		glBegin(GL_TRIANGLE_STRIP);
		for(vector<Vector>::const_iterator i = c.begin(); i != c.end(); ++i) {
			glVertex2d(i->x, i->y);
			glVertex2d(-1.0, i->y);
		}
		glEnd();
	}

	static void draw_contour_strip(const Contour &c) {
		glBegin(GL_TRIANGLE_STRIP);
		const Contour::ChunkList &chunks = c.get_chunks();
		Vector prev;
		for(Contour::ChunkList::const_iterator i = chunks.begin(); i != chunks.end(); ++i) {
			if ( i->type == Contour::LINE
			  || i->type == Contour::CLOSE)
			{
				glVertex2d(i->p1.x, i->p1.y);
				glVertex2d(-1.0, i->p1.y);
				prev.x = -1.0;
				prev.y = i->p1.y;
			} else {
				glVertex2d(prev.x, prev.y);
				glVertex2d(prev.x, prev.y);
				glVertex2d(i->p1.x, i->p1.y);
				glVertex2d(i->p1.x, i->p1.y);
				prev = i->p1;
			}
		}
		glEnd();
	}

	static void draw_contour_strip(const int &count) {
		glDrawArrays(GL_TRIANGLE_STRIP, 4, count);
	}

	template<typename T>
	static void draw_contour(const T &c, bool even_odd, bool invert) {
		glEnable(GL_STENCIL_TEST);

		// render mask
		GLint draw_buffer;
		glGetIntegerv(GL_DRAW_BUFFER, &draw_buffer);
		glDrawBuffer(GL_NONE);
		glClear(GL_STENCIL_BUFFER_BIT);
		glStencilFunc(GL_ALWAYS, 0, 0);
		if (even_odd) {
			glStencilOp(GL_INCR_WRAP, GL_INCR_WRAP, GL_INCR_WRAP);
		} else {
			glStencilOpSeparate(GL_FRONT, GL_INCR_WRAP, GL_INCR_WRAP, GL_INCR_WRAP);
			glStencilOpSeparate(GL_BACK, GL_DECR_WRAP, GL_DECR_WRAP, GL_DECR_WRAP);
		}
		Shaders::simple();
		draw_contour_strip(c);
		glDrawBuffer((GLenum)draw_buffer);

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

		Shaders::color(Color(0.f, 0.f, 1.f, 1.f));
		glDrawArrays(GL_TRIANGLE_STRIP, 0, 4);

		glDisable(GL_STENCIL_TEST);
	}

	static void draw_contour(int start, int count, bool even_odd, bool invert, const Color &color) {
		glEnable(GL_STENCIL_TEST);

		// render mask
		GLint draw_buffer;
		glGetIntegerv(GL_DRAW_BUFFER, &draw_buffer);
		glDrawBuffer(GL_NONE);
		glClear(GL_STENCIL_BUFFER_BIT);
		glStencilFunc(GL_ALWAYS, 0, 0);
		if (even_odd) {
			glStencilOp(GL_INCR_WRAP, GL_INCR_WRAP, GL_INCR_WRAP);
		} else {
			glStencilOpSeparate(GL_FRONT, GL_INCR_WRAP, GL_INCR_WRAP, GL_INCR_WRAP);
			glStencilOpSeparate(GL_BACK, GL_DECR_WRAP, GL_DECR_WRAP, GL_DECR_WRAP);
		}
		Shaders::simple();
		glDrawArrays(GL_TRIANGLE_STRIP, start, count);
		glDrawBuffer((GLenum)draw_buffer);

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

		Shaders::color(color);
		glDrawArrays(GL_TRIANGLE_STRIP, 0, 4);

		glDisable(GL_STENCIL_TEST);
	}

	static Vector get_frame_size() {
		int vp[4] = {};
		glGetIntegerv(GL_VIEWPORT, vp);
		return Vector((Real)vp[2], (Real)vp[3]);
	}

};

Test::Wrapper::Wrapper(const std::string &filename):
	filename(filename),
	surface(),
	tga(filename.size() > 4 && filename.substr(filename.size()-4, 4) == ".tga"),
	t(get_clock())
{ }

Test::Wrapper::Wrapper(const std::string &filename, Surface &surface):
	filename(filename),
	surface(&surface),
	tga(filename.size() > 4 && filename.substr(filename.size()-4, 4) == ".tga"),
	t(get_clock())
{ }

Test::Wrapper::~Wrapper() {
	if (!surface && tga) glFinish();
	Real ms = 1000.0*(Real)(get_clock() - t)/(Real)(CLOCKS_PER_SEC);
	cout << setw(8) << fixed << setprecision(3)
	     << ms << " ms - " << filename << flush << endl;

	if (tga) {
		if (surface)
			Helper::save_surface(*surface, filename);
		else
			Helper::save_viewport(filename);
	}

	if (surface) {
		surface->clear();
	} else {
		glClear(GL_COLOR_BUFFER_BIT);
		glFinish();
	}
}

void Test::check_gl(const std::string &s) {
	GLenum error = glGetError();
	if (error) {
		cout << s << " GL error: 0x" << setbase(16) << error << setbase(10) << endl;
	}
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


void Test::test1() {
	// OpenGl 2 code

	vector<Vector> c;
	ContourBuilder::build_simple(c);
	cout << c.size() << " vertices" << endl;

	glPushAttrib(GL_ALL_ATTRIB_BITS);

	int random = (int)get_clock();
	{
		Wrapper t("test_1_control_timer_200000_simple_ops");
		int j = random;
		for(long long i = 0; i < 200000; ++i)
			if (j > i) ++j;
		glColor4f(j%2, j%2, j%2, j%2);
	}

	glColor4f(0.f, 0.f, 1.f, 1.f);

	{
		Wrapper t("test_1_contour.tga");
		glBegin(GL_LINE_STRIP);
		for(vector<Vector>::const_iterator i = c.begin(); i != c.end(); ++i)
			glVertex2d(i->x, i->y);
		glEnd();
	}

	{
		Wrapper t("test_1_contour_fill.tga");
		Helper::draw_contour(c, false, false);
	}

	{
		Wrapper t("test_1_contour_fill_invert.tga");
		Helper::draw_contour(c, false, true);
	}

	{
		Wrapper t("test_1_contour_evenodd.tga");
		Helper::draw_contour(c, true, false);
	}

	{
		Wrapper t("test_1_contour_evenodd_invert.tga");
		Helper::draw_contour(c, true, true);
	}

	glPopAttrib();
}

void Test::test2() {
	Contour c, cc;
	ContourBuilder::build(cc);
	cout << cc.get_chunks().size() << " commands" << endl;

	Vector frame_size = Helper::get_frame_size();

	Rect bounds;
	bounds.p0 = Vector(-1.0, -1.0);
	bounds.p1 = Vector( 1.0,  1.0);
	Vector min_size(1.75/frame_size.x, 1.75/frame_size.y);

	{
		Wrapper t("test_2_split");
		cc.split(c, bounds, min_size);
	}

	const Contour::ChunkList &chunks = c.get_chunks();
	cout << chunks.size() << " vertices" << endl;

	GLuint buffer_id = 0;
	GLuint array_id = 0;
	int count = 0;
	vector< vec2<float> > vertices;

	{
		Wrapper t("test_2_init_buffer");
		vertices.resize(4+4*chunks.size());
		glGenBuffers(1, &buffer_id);
		glBindBuffer(GL_ARRAY_BUFFER, buffer_id);
		glBufferData( GL_ARRAY_BUFFER,
				      vertices.size()*sizeof(vec2<float>),
					  &vertices.front(),
					  GL_DYNAMIC_DRAW );
		vertices.clear();
		vertices.reserve(4+4*chunks.size());

		glGenVertexArrays(1, &array_id);
		glBindVertexArray(array_id);

		glEnableVertexAttribArray(0);
		glVertexAttribPointer(0, 2, GL_FLOAT, GL_TRUE, 0, NULL);

		Shaders::color(Color(0.f, 0.f, 1.f, 1.f));
		glDrawArrays(GL_TRIANGLE_STRIP, 0, vertices.size());
		glFinish();
		glClear(GL_COLOR_BUFFER_BIT);
		glFinish();
	}

	{
		Wrapper t("test_2_prepare_data");
		vertices.push_back(vec2<float>(bounds.p0.x, bounds.p0.y));
		vertices.push_back(vec2<float>(bounds.p0.x, bounds.p1.y));
		vertices.push_back(vec2<float>(bounds.p1.x, bounds.p0.y));
		vertices.push_back(vec2<float>(bounds.p1.x, bounds.p1.y));
		vertices.push_back(vec2<float>());
		vertices.push_back(vec2<float>());
		for(Contour::ChunkList::const_iterator i = chunks.begin(); i != chunks.end(); ++i) {
			if ( i->type == Contour::LINE
			  || i->type == Contour::CLOSE)
			{
				vertices.push_back(vec2<float>(i->p1));
				vertices.push_back(vec2<float>(-1.f, (float)i->p1.y));
			} else {
				vertices.push_back(vertices.back());
				vertices.push_back(vertices.back());
				vertices.push_back(vec2<float>(i->p1));
				vertices.push_back(vertices.back());
			}
		}
		count = vertices.size() - 4;
	}

	{
		Wrapper t("test_2_send_data");
		glBufferSubData( GL_ARRAY_BUFFER,
						 0,
					     vertices.size()*sizeof(vertices.front()),
					     &vertices.front() );
	}

	{
		Wrapper t("test_2_simple_fill.tga");
		glDrawArrays(GL_TRIANGLE_STRIP, 0, 4);
	}

	{
		Wrapper t("test_2_array.tga");
		glColorMask(GL_FALSE, GL_FALSE, GL_FALSE, GL_FALSE);
		glDrawArrays(GL_TRIANGLE_STRIP, 4, count);
		glColorMask(GL_TRUE, GL_TRUE, GL_TRUE, GL_TRUE);
	}

	{
		Wrapper t("test_2_contour_fill.tga");
		Helper::draw_contour(count, false, false);
	}

	{
		Wrapper t("test_2_contour_fill_invert.tga");
		Helper::draw_contour(count, false, true);
	}

	{
		Wrapper t("test_2_contour_evenodd.tga");
		Helper::draw_contour(count, true, false);
	}

	{
		Wrapper t("test_2_contour_evenodd_invert.tga");
		Helper::draw_contour(count, true, true);
	}

	glBindBuffer(GL_ARRAY_BUFFER, 0);
	glDeleteBuffers(1, &buffer_id);
}

void Test::test3() {
	Contour c;
	ContourBuilder::build(c);
	cout << c.get_chunks().size() << " commands" << endl;

	Vector frame_size = Helper::get_frame_size();
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
		Wrapper t("test_3_build_polyspan");
		c.to_polyspan(polyspan);
	}

	cout << polyspan.get_covers().size() << " covers" << endl;

	glPushAttrib(GL_ALL_ATTRIB_BITS);
	glColor4d(0.0, 0.0, 1.0, 1.0);
	{
		Wrapper t("test_3_polyspan_gl_lines.tga");
		glBegin(GL_LINE_STRIP);
		for(Polyspan::cover_array::const_iterator i = polyspan.get_covers().begin(); i != polyspan.get_covers().end(); ++i)
			glVertex2d((double)i->x/1024.0*2.0 - 1.0, (double)i->y/1024.0*2.0 - 1.0);
		glEnd();
	}
	glPopAttrib();


	{
		Wrapper t("test_3_polyspan_sort");
		polyspan.sort_marks();
	}

	{
		Wrapper t("test_3_polyspan_fill.tga", surface);
		RenderSW::polyspan(surface, polyspan, color, false, false);
	}

	{
		Wrapper t("test_3_polyspan_fill_invert.tga", surface);
		RenderSW::polyspan(surface, polyspan, color, false, true);
	}

	{
		Wrapper t("test_3_polyspan_evenodd.tga", surface);
		RenderSW::polyspan(surface, polyspan, color, true, false);
	}

	{
		Wrapper t("test_3_polyspan_evenodd_invert.tga", surface);
		RenderSW::polyspan(surface, polyspan, color, true, true);
	}
}

void Test::test4() {
	Vector frame_size = Helper::get_frame_size();
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

		GLuint buffer_id = 0;
		GLuint array_id = 0;
		vector< vec2<float> > vertices;
		vector<int> starts(contours_gl.size());
		vector<int> counts(contours_gl.size());

		{
			Wrapper t("test_4_gl_init_buffer");
			vertices.resize(4 + 4*commands_count + 2*contours_gl.size());
			glGenBuffers(1, &buffer_id);
			glBindBuffer(GL_ARRAY_BUFFER, buffer_id);
			glBufferData( GL_ARRAY_BUFFER,
						  vertices.size()*sizeof(vec2<float>),
						  &vertices.front(),
						  GL_DYNAMIC_DRAW );
			vertices.clear();
			vertices.reserve(4 + 4*commands_count);

			glGenVertexArrays(1, &array_id);
			glBindVertexArray(array_id);

			glEnableVertexAttribArray(0);
			glVertexAttribPointer(0, 2, GL_FLOAT, GL_TRUE, 0, NULL);

			Shaders::color(Color(0.f, 0.f, 1.f, 1.f));
			glDrawArrays(GL_TRIANGLE_STRIP, 0, vertices.size());
			glFinish();
			glClear(GL_COLOR_BUFFER_BIT);
			glFinish();
		}

		{
			Wrapper t("test_4_gl_stencil_prepare_data");
			vertices.push_back(vec2<float>(bounds_gl.p0.x, bounds_gl.p0.y));
			vertices.push_back(vec2<float>(bounds_gl.p0.x, bounds_gl.p1.y));
			vertices.push_back(vec2<float>(bounds_gl.p1.x, bounds_gl.p0.y));
			vertices.push_back(vec2<float>(bounds_gl.p1.x, bounds_gl.p1.y));
			for(int i = 0; i < (int)contours_gl.size(); ++i) {
				starts[i] = (int)vertices.size();
				const Contour::ChunkList &chunks = contours_gl[i].contour.get_chunks();
				for(Contour::ChunkList::const_iterator j = chunks.begin(); j != chunks.end(); ++j) {
					if (j->type == Contour::LINE) {
						vertices.push_back(vec2<float>(j->p1));
						vertices.push_back(vec2<float>(-1.f, (float)j->p1.y));
					} else
					if (j->type == Contour::CLOSE) {
						vertices.push_back(vec2<float>(j->p1));
						vertices.push_back(vec2<float>(-1.f, (float)j->p1.y));
					} else {
						vertices.push_back(vertices.back());
						vertices.push_back(vec2<float>(j->p1));
						vertices.push_back(vertices.back());
						vertices.push_back(vec2<float>(-1.f, (float)j->p1.y));
					}
				}
				counts[i] = (int)vertices.size() - starts[i];
			}
		}

		{
			Wrapper t("test_4_gl_stencil_send_data");
			glBufferSubData( GL_ARRAY_BUFFER,
							 0,
						     vertices.size()*sizeof(vertices.front()),
						     &vertices.front() );
		}

		{
			Wrapper t("test_4_gl_stencil_points.tga");
			glDrawArrays(GL_POINTS, 0, vertices.size());
		}

		{
			Wrapper t("test_4_gl_stencil_render.tga");
			for(int i = 0; i < (int)contours_gl.size(); ++i) {
				Helper::draw_contour(
					starts[i],
					counts[i],
					contours_gl[i].invert,
					contours_gl[i].evenodd,
					contours_gl[i].color );
			}
			// glDrawArrays(GL_POINTS, 0, vertices.size());
		}

		// gl_triangles

		GLuint index_buffer_id = 0;
		vector<int> triangle_starts(contours_gl.size());
		vector<int> triangle_counts(contours_gl.size());
		vector<int> triangles;
		vertices.clear();
		vertices.reserve(commands_count);

		{
			Wrapper t("test_4_gl_init_index_buffer");
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
			Wrapper t("test_4_gl_triangulate");
			int index_offset = 4;
			for(int i = 0; i < (int)contours_gl.size(); ++i) {
				triangle_starts[i] = (int)triangles.size();
				Triangulator::triangulate(contours_gl[i].contour, triangles, index_offset);
				triangle_counts[i] = (int)triangles.size() - triangle_starts[i];
				index_offset += (int)contours_gl[i].contour.get_chunks().size();
			}
		}

		cout << triangles.size() << " triangles" << endl;

		{
			Wrapper t("test_4_gl_triangles_prepare_vertices");
			for(int i = 0; i < (int)contours_gl.size(); ++i) {
				const Contour::ChunkList &chunks = contours_gl[i].contour.get_chunks();
				for(Contour::ChunkList::const_iterator j = chunks.begin(); j != chunks.end(); ++j)
					vertices.push_back(vec2<float>(j->p1));
			}
		}

		{
			Wrapper t("test_4_gl_triangles_send_data");
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
			Wrapper t("test_4_gl_triangles_render.tga");
			for(int i = 0; i < (int)contours_gl.size(); ++i) {
				Shaders::color(contours_gl[i].color);
				glDrawElements(GL_TRIANGLES, triangle_counts[i], GL_UNSIGNED_INT, (char*)NULL + triangle_starts[i]*sizeof(int));
			}
		}
	}

	{
		// software

		vector<ContourInfo> contours_sw = contours;
		for(vector<ContourInfo>::iterator i = contours_sw.begin(); i != contours_sw.end(); ++i)
			i->contour.transform(bounds_file, bounds_sw);

		vector<Polyspan> polyspans(contours_sw.size());
		{
			Wrapper t("test_4_sw_build_polyspans");
			for(int i = 0; i < (int)contours_sw.size(); ++i) {
				polyspans[i].init(0, 0, width, height);
				contours_sw[i].contour.to_polyspan(polyspans[i]);
				polyspans[i].sort_marks();
			}
		}

		Surface surface(width, height);

		{
			Wrapper t("test_4_sw_render_polyspans.tga", surface);
			for(int i = 0; i < (int)contours_sw.size(); ++i)
				RenderSW::polyspan(surface, polyspans[i], contours_sw[i].color, contours_sw[i].evenodd, contours_sw[i].invert);
		}
	}
}
