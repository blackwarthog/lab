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

#include <ctime>
#include <cassert>

#include <iostream>
#include <iomanip>

#include <GL/gl.h>
#include <GL/glext.h>
#include <GL/glx.h>

#include "test.h"


#define GLX_CONTEXT_MAJOR_VERSION_ARB 0x2091
#define GLX_CONTEXT_MINOR_VERSION_ARB 0x2092
typedef GLXContext (*GLXCREATECONTEXTATTRIBSARBPROC)(Display*, GLXFBConfig, GLXContext, Bool, const int*);


using namespace std;


clock_t get_clock() {
	return clock();
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
	Test::test1();
	Test::test2();
	Test::test3();

	// deinitialization
	glXMakeContextCurrent(display, None, None, NULL);
	glXDestroyContext(display, context);
	glXDestroyPbuffer(display, pbuffer);
	XCloseDisplay(display);

	cout << "done" << endl;
	return 0;
}
