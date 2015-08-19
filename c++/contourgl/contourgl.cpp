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

#include <cmath>

#include <fstream>
#include <iostream>
#include <vector>
#include <map>

#include <assert.h>

#include <GL/gl.h>
#include <GL/glext.h>
#include <GL/glx.h>


#define GLX_CONTEXT_MAJOR_VERSION_ARB 0x2091
#define GLX_CONTEXT_MINOR_VERSION_ARB 0x2092
typedef GLXContext (*GLXCREATECONTEXTATTRIBSARBPROC)(Display*, GLXFBConfig, GLXContext, Bool, const int*);


using namespace std;


typedef pair<float, float> vec2;
typedef vector<vec2> contour;

void save_rgba(const void *buffer, int width, int height, const string &filename) {
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
	int line_size = 4*width;
	const char *end = (char*)buffer;
	const char *current = end + height*line_size;
	while(current > end) {
		current -= line_size;
		f.write(current, line_size);
	}
}

void save_viewport(const string &filename) {
	cout << filename << endl;
	glFinish();
	int vp[4] = {};
	glGetIntegerv(GL_VIEWPORT, vp);
	char *buffer = new char[vp[2]*vp[3]*4];
	glReadPixels(vp[0], vp[1], vp[2], vp[3], GL_BGRA, GL_UNSIGNED_BYTE, buffer);
	save_rgba(buffer, vp[2], vp[3], filename);
}

void build_contour(contour &c) {
	const float min_segment_length = 0.001f;
	const float rounds = 10.f;
	const float rounds2 = 1.f;

	contour back;

	float angle = 360.f;
	float offset = 0.25f/(rounds + 1.f);

	// go front
	while(true) {
		float radius = angle/360.f/(rounds + 1.f);
		float step = min_segment_length*180.f/M_PI/radius;
		if (radius > 1.f - 2.f*offset) break;

		float fr = radius + offset;
		float fx = fr*sinf(angle/180.f*M_PI);
		float fy = fr*cosf(angle/180.f*M_PI);

		float br = radius - offset;
		float bx = br*sinf(angle/180.f*M_PI);
		float by = br*cosf(angle/180.f*M_PI);

		c.push_back(vec2(fx, fy));
		back.push_back(vec2(bx, by));

		angle += step;
	}

	float max_angle = angle;

	while(true) {
		float radius = max_angle/360.f/(rounds + 1.f)
				     + (max_angle-angle)/360.f/rounds2;
		float step = min_segment_length*180.f/M_PI/radius;
		if (radius < 1.f/(rounds + 1.f))
			break;

		float fr = radius + offset;
		float fx = fr*sinf(angle/180.f*M_PI);
		float fy = fr*cosf(angle/180.f*M_PI);

		float br = radius - offset;
		float bx = br*sinf(angle/180.f*M_PI);
		float by = br*cosf(angle/180.f*M_PI);

		c.push_back(vec2(fx, fy));
		back.push_back(vec2(bx, by));

		angle += step;
	}


	// go back
	c.reserve(c.size() + back.size() + 1);
	for(contour::reverse_iterator ri = back.rbegin(); ri != back.rend(); ++ri)
		c.push_back(*ri);

	// close
	c.push_back(c.front());

	cout << c.size() << " vertices" << endl;
}

void draw_contour_strip(const contour &c) {
	glBegin(GL_TRIANGLE_STRIP);
	for(contour::const_iterator i = c.begin(); i != c.end(); ++i) {
		glVertex2f(i->first, i->second);
		glVertex2f(-1.f, i->second);
	}
	glEnd();
}


void draw_contour(const contour &c, bool even_odd, bool invert) {
	glPushAttrib(GL_ALL_ATTRIB_BITS);
	glEnable(GL_STENCIL_TEST);

	// render mask
	glClear(GL_STENCIL_BUFFER_BIT);
	glColorMask(GL_FALSE, GL_FALSE, GL_FALSE, GL_FALSE);
	glStencilFunc(GL_ALWAYS, 0, 0);
	if (even_odd) {
		glStencilOp(GL_KEEP, GL_KEEP, GL_INCR_WRAP);
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
	glVertex2d(-1.f, -1.f);
	glVertex2d( 1.f, -1.f);
	glVertex2d(-1.f,  1.f);
	glVertex2d( 1.f,  1.f);
	glEnd();

	glPopAttrib();
}

class test_wrapper {
private:
	string filename;
	clock_t t;
	test_wrapper(const test_wrapper&): t() { }
	test_wrapper& operator= (const test_wrapper&) { return *this; }
public:
	test_wrapper(const string &filename): filename(filename), t(clock()) { }
	~test_wrapper() {
		glFinish();
		cout << 1000.0*(double)(clock() - t)/(double)(CLOCKS_PER_SEC) << " ms" << endl;
		cout << filename << endl;
		save_viewport(filename);
		glClear(GL_COLOR_BUFFER_BIT);
	}
};

void test() {
	contour c;
	build_contour(c);

	glColor4f(0.f, 0.f, 1.f, 1.f);

	{
		test_wrapper t("test_contour.tga");
		glBegin(GL_LINE_STRIP);
		for(contour::const_iterator i = c.begin(); i != c.end(); ++i)
			glVertex2f(i->first, i->second);
		glEnd();
	}

	{
		test_wrapper t("test_contour_fill.tga");
		draw_contour(c, false, false);
	}

	{
		test_wrapper t("test_contour_fill_invert.tga");
		draw_contour(c, false, true);
	}

	{
		test_wrapper t("test_contour_evenodd.tga");
		draw_contour(c, true, false);
	}

	{
		test_wrapper t("test_contour_evenodd_invert.tga");
		draw_contour(c, true, true);
	}
}

int main() {
	// open display (we will use default display and screen 0)
	Display *display = XOpenDisplay(NULL);
	assert(display);

	// choose config
	int config_attribs[] = {
		GLX_DOUBLEBUFFER,      False,
		GLX_RED_SIZE,          8,
		GLX_GREEN_SIZE,        8,
		GLX_BLUE_SIZE,         8,
		GLX_ALPHA_SIZE,        8,
		GLX_DEPTH_SIZE,        24,
		GLX_STENCIL_SIZE,      8,
		GLX_ACCUM_RED_SIZE,    8,
		GLX_ACCUM_GREEN_SIZE,  8,
		GLX_ACCUM_BLUE_SIZE,   8,
		GLX_ACCUM_ALPHA_SIZE,  8,
		GLX_DRAWABLE_TYPE,     GLX_PBUFFER_BIT,
		None };
	int nelements = 0;
	GLXFBConfig *configs = glXChooseFBConfig(display, 0, config_attribs, &nelements);
	assert(configs != NULL && nelements > 0);
	GLXFBConfig config = configs[0];
	assert(config);

	// create pbuffer
	int pbuffer_width = 1024;
	int pbuffer_height = 1024;
	int pbuffer_attribs[] = {
		GLX_PBUFFER_WIDTH, pbuffer_width,
		GLX_PBUFFER_HEIGHT, pbuffer_height,
		None };
	GLXPbuffer pbuffer = glXCreatePbuffer(display, config, pbuffer_attribs);
	assert(pbuffer);

	// create context
	int context_attribs[] = {
		GLX_CONTEXT_MAJOR_VERSION_ARB, 2,
		GLX_CONTEXT_MINOR_VERSION_ARB, 1,
		None };
	GLXCREATECONTEXTATTRIBSARBPROC glXCreateContextAttribsARB = (GLXCREATECONTEXTATTRIBSARBPROC) glXGetProcAddress((const GLubyte*)"glXCreateContextAttribsARB");
	GLXContext context = glXCreateContextAttribsARB(display, config, NULL, True, context_attribs);
	assert(context);

	// make context current
	glXMakeContextCurrent(display, pbuffer, pbuffer, context);

	// set view port
	glViewport(0, 0, pbuffer_width, pbuffer_height);

	// do something
	test();

	// deinitialization
	glXMakeContextCurrent(display, None, None, NULL);
	glXDestroyContext(display, context);
	glXDestroyPbuffer(display, pbuffer);
	XCloseDisplay(display);

	cout << "done" << endl;
	return 0;
}
