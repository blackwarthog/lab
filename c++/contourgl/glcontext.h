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

#ifndef _GLCONTEXT_H_
#define _GLCONTEXT_H_

#include <string>

#include <GL/gl.h>
#include <GL/glext.h>
#include <GL/glx.h>


class GlContext {
public:
	Display *display;
	GLXPbuffer pbuffer;
	GLXContext context;

	GLuint texture_id;
	GLuint framebuffer_id;
	GLuint renderbuffer_id;

	GLuint multisample_texture_id;
	GLuint multisample_renderbuffer_id;
	GLuint multisample_framebuffer_id;

	GlContext(int width, int height, bool hdr, bool multisample, int samples);
	~GlContext();

	void use();
	void unuse();

	void check(const std::string &s = std::string());
};

#endif
