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

#include <GL/gl.h>
#include <GL/glext.h>
#include <GL/glx.h>

#include "test.h"

#include "contour.h"
#include "rendersw.h"
#include "contourbuilder.h"


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
		ofstream f(filename.c_str(), ofstream::out | ofstream::trunc | ofstream::binary);

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
		int vp[4] = {};
		glGetIntegerv(GL_VIEWPORT, vp);
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
		glDrawArrays(GL_TRIANGLE_STRIP, 0, count);
	}

	template<typename T>
	static void draw_contour(const T &c, bool even_odd, bool invert) {
		glPushAttrib(GL_ALL_ATTRIB_BITS);
		glEnable(GL_STENCIL_TEST);

		// render mask
		glClear(GL_STENCIL_BUFFER_BIT);
		glColorMask(GL_FALSE, GL_FALSE, GL_FALSE, GL_FALSE);
		glStencilFunc(GL_ALWAYS, 0, 0);
		if (even_odd) {
			glStencilOp(GL_INCR_WRAP, GL_INCR_WRAP, GL_INCR_WRAP);
		} else {
			glStencilOpSeparate(GL_FRONT, GL_INCR_WRAP, GL_INCR_WRAP, GL_INCR_WRAP);
			glStencilOpSeparate(GL_BACK, GL_DECR_WRAP, GL_DECR_WRAP, GL_DECR_WRAP);
		}
		draw_contour_strip(c);

		// fill mask
		glColorMask(GL_TRUE, GL_TRUE, GL_TRUE, GL_TRUE);
		glStencilOp(GL_KEEP, GL_KEEP, GL_KEEP);
		if (!even_odd && !invert)
			glStencilFunc(GL_NOTEQUAL, 0, -1);
		if (!even_odd &&  invert)
			glStencilFunc(GL_EQUAL, 0, -1);
		if ( even_odd && !invert)
			glStencilFunc(GL_EQUAL, 1, 1);
		if ( even_odd &&  invert)
			glStencilFunc(GL_EQUAL, 0, 1);

		glBegin(GL_TRIANGLE_STRIP);
		glVertex2d(-1.0, -1.0);
		glVertex2d( 1.0, -1.0);
		glVertex2d(-1.0,  1.0);
		glVertex2d( 1.0,  1.0);
		glEnd();

		glPopAttrib();
	}
};

Test::Wrapper::Wrapper(const std::string &filename):
	filename(filename), surface(), t(get_clock())
{ }

Test::Wrapper::~Wrapper() {
	if (!surface) glFinish();
	Real ms = 1000.0*(Real)(get_clock() - t)/(Real)(CLOCKS_PER_SEC);
	cout << setw(8) << fixed << setprecision(3)
	     << ms << " ms - " << filename << endl;

	if (filename.size() > 4 && filename.substr(filename.size()-4, 4) == ".tga") {
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

void Test::test1() {
	vector<Vector> c;
	ContourBuilder::build_simple(c);
	cout << c.size() << " vertices" << endl;

	glPushAttrib(GL_ALL_ATTRIB_BITS);
	glColor4d(0.0, 0.0, 1.0, 1.0);

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

	Rect bounds;
	bounds.p0 = Vector(-1.0, -1.0);
	bounds.p1 = Vector( 1.0,  1.0);
	Vector min_size(1.75/1024.0, 1.75/1024.0);

	{
		Wrapper("test_2_split");
		cc.split(c, bounds, min_size);
	}

	const Contour::ChunkList &chunks = c.get_chunks();
	cout << chunks.size() << " vertices" << endl;

	glPushAttrib(GL_ALL_ATTRIB_BITS);
	glColor4d(0.0, 0.0, 1.0, 1.0);

	GLuint buf_id = 0;
	int count = 0;
	vector< vec2<float> > vertices;

	{
		Wrapper t("test_2_init_buffer");
		vertices.resize(4*chunks.size());
		glGenBuffers(1, &buf_id);
		glBindBuffer(GL_ARRAY_BUFFER, buf_id);
		glBufferData( GL_ARRAY_BUFFER,
				      vertices.size()*sizeof(vec2<float>),
					  &vertices.front(),
					  GL_DYNAMIC_DRAW );
		vertices.clear();
		vertices.reserve(4*chunks.size());

		glEnableClientState(GL_VERTEX_ARRAY);
		glDrawArrays(GL_TRIANGLES, 0, vertices.size());
		glClear(GL_COLOR_BUFFER_BIT);
		glFinish();
	}

	{
		Wrapper t("test_2_prepare_data");
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
		count = vertices.size();
	}

	{
		Wrapper t("test_2_send_data");
		glBufferSubData( GL_ARRAY_BUFFER,
						 0,
					     vertices.size()*sizeof(vec2<float>),
					     &vertices.front() );
		glVertexPointer(2, GL_FLOAT, sizeof(vec2<float>), 0);
	}

	{
		Wrapper t("test_2_contour.tga");
		glBegin(GL_LINE_STRIP);
		for(Contour::ChunkList::const_iterator i = chunks.begin(); i != chunks.end(); ++i)
			glVertex2d(i->p1.x, i->p1.y);
		glEnd();
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

	glDisableClientState(GL_VERTEX_ARRAY);
	glBindBuffer(GL_ARRAY_BUFFER, 0);
	glDeleteBuffers(1, &buf_id);
	glPopAttrib();
}

void Test::test3() {
	Contour c;
	ContourBuilder::build(c);
	cout << c.get_chunks().size() << " commands" << endl;

	Rect bounds;
	bounds.p0 = Vector(-1.0, -1.0);
	bounds.p1 = Vector( 1.0,  1.0);
	Rect pixel_bounds;
	pixel_bounds.p0 = Vector(   0.0,    0.0);
	pixel_bounds.p1 = Vector(1024.0, 1024.0);

	c.transform(bounds, pixel_bounds);

	Polyspan polyspan;
	polyspan.init(0, 0, 1024, 1024);

	Surface surface(1024, 1024);
	Color color(0.f, 0.f, 1.f, 1.f);

	{
		Wrapper("test_3_build_polyspan");
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
		Wrapper("test_3_polyspan_sort");
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

