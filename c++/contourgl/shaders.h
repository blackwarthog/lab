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

#ifndef _SHADERS_H_
#define _SHADERS_H_

#include <GL/gl.h>
#include <GL/glext.h>
#include <GL/glx.h>

#include "swrender.h"

class Shaders {
public:
	GLuint simple_vertex_id;
	GLuint simpleProgramId;

	GLuint color_fragment_id;
	GLuint colorProgramId;
	GLint colorUniform;

	void check_shader(GLuint id, const char *src);
	void check_program(GLuint id, const char *name);

	Shaders();
	~Shaders();

	void simple();
	void color(const Color &c);
};

#endif
