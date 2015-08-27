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

#include "clcontext.h"
#include "test.h"
#include "shaders.h"


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
	int pbuffer_width = 256;
	int pbuffer_height = 256;
	int pbuffer_attribs[] = {
		GLX_PBUFFER_WIDTH, pbuffer_width,
		GLX_PBUFFER_HEIGHT, pbuffer_height,
		None };
	GLXPbuffer pbuffer = glXCreatePbuffer(display, config, pbuffer_attribs);
	assert(pbuffer);

	// create context
	int context_attribs[] = {
		GLX_CONTEXT_MAJOR_VERSION_ARB, 3,
		GLX_CONTEXT_MINOR_VERSION_ARB, 3,
		None };
	GLXCREATECONTEXTATTRIBSARBPROC glXCreateContextAttribsARB = (GLXCREATECONTEXTATTRIBSARBPROC) glXGetProcAddress((const GLubyte*)"glXCreateContextAttribsARB");
	GLXContext context = glXCreateContextAttribsARB(display, config, NULL, True, context_attribs);
	assert(context);

	// make context current
	glXMakeContextCurrent(display, pbuffer, pbuffer, context);

	// frame buffer
	int framebuffer_width = 512;
	int framebuffer_height = 512;
	int framebuffer_samples = 16;
	bool antialising = false;
	bool hdr = false;

	GLenum internal_format = hdr ? GL_RGBA16F : GL_RGBA;
	GLenum color_type = hdr ? GL_FLOAT : GL_UNSIGNED_BYTE;

	GLuint multisample_texture_id = 0;
	glGenTextures(1, &multisample_texture_id);
	glBindTexture(GL_TEXTURE_2D_MULTISAMPLE, multisample_texture_id);
	glTexImage2DMultisample(GL_TEXTURE_2D_MULTISAMPLE, framebuffer_samples, internal_format, framebuffer_width, framebuffer_height, GL_TRUE);

	GLuint multisample_renderbuffer_id = 0;
	glGenRenderbuffers(1, &multisample_renderbuffer_id);
	glBindRenderbuffer(GL_RENDERBUFFER, multisample_renderbuffer_id);
	glRenderbufferStorageMultisample(GL_RENDERBUFFER, framebuffer_samples, GL_STENCIL_INDEX8, framebuffer_width, framebuffer_height);

	GLuint multisample_framebuffer_id = 0;
	glGenFramebuffers(1, &multisample_framebuffer_id);
	glBindFramebuffer(GL_DRAW_FRAMEBUFFER, multisample_framebuffer_id);
	glFramebufferRenderbuffer(GL_DRAW_FRAMEBUFFER, GL_STENCIL_ATTACHMENT, GL_RENDERBUFFER, multisample_renderbuffer_id);
	glFramebufferTexture2D(GL_DRAW_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D_MULTISAMPLE, multisample_texture_id, 0);

	GLuint texture_id = 0;
	glGenTextures(1, &texture_id);
	glBindTexture(GL_TEXTURE_2D, texture_id);
	glTexImage2D(GL_TEXTURE_2D, 0, internal_format, framebuffer_width, framebuffer_height, 0, GL_RGBA, color_type, NULL);

	GLuint renderbuffer_id = 0;
	glGenRenderbuffers(1, &renderbuffer_id);
	glBindRenderbuffer(GL_RENDERBUFFER, renderbuffer_id);
	glRenderbufferStorage(GL_RENDERBUFFER, GL_STENCIL_INDEX8, framebuffer_width, framebuffer_height);

	GLuint framebuffer_id = 0;
	glGenFramebuffers(1, &framebuffer_id);
	glBindFramebuffer(GL_READ_FRAMEBUFFER, framebuffer_id);
	glFramebufferRenderbuffer(GL_READ_FRAMEBUFFER, GL_STENCIL_ATTACHMENT, GL_RENDERBUFFER, renderbuffer_id);
	glFramebufferTexture2D(GL_READ_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, texture_id, 0);

	cout << "Framebuffer status:" << setbase(16)
		 << " 0x" << glCheckFramebufferStatus(GL_DRAW_FRAMEBUFFER)
		 << " 0x" << glCheckFramebufferStatus(GL_READ_FRAMEBUFFER)
		 << setbase(10) << endl;

	// set view port
	glViewport(0, 0, framebuffer_width, framebuffer_height);

	if (antialising)
		glEnable(GL_MULTISAMPLE);
	else
		glBindFramebuffer(GL_DRAW_FRAMEBUFFER, framebuffer_id);

	Shaders::initialize();

	// do something
	//Test::test1();
	//Test::test2();
	//Test::test3();
	//Test::test4();

	ClContext().hello();

	Shaders::deinitialize();

	// deinitialization
	glBindFramebuffer(GL_READ_FRAMEBUFFER, 0);
	glDeleteFramebuffers(1, &framebuffer_id);

	glBindTexture(GL_TEXTURE_2D, 0);
	glDeleteTextures(1, &texture_id);

	glBindFramebuffer(GL_DRAW_FRAMEBUFFER, 0);
	glDeleteFramebuffers(1, &multisample_framebuffer_id);

	glBindRenderbuffer(GL_RENDERBUFFER, 0);
	glDeleteRenderbuffers(1, &multisample_renderbuffer_id);

	glBindTexture(GL_TEXTURE_2D_MULTISAMPLE, 0);
	glDeleteTextures(1, &multisample_texture_id);

	glXMakeContextCurrent(display, None, None, NULL);
	glXDestroyContext(display, context);
	glXDestroyPbuffer(display, pbuffer);
	XCloseDisplay(display);

	cout << "done" << endl;
	return 0;
}
