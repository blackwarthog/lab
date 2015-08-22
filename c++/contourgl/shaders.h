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

#include <GL/gl.h>
#include <GL/glext.h>
#include <GL/glx.h>

#include "rendersw.h"

class Shaders {
private:
	GLuint simple_vertex_id;
	GLuint simpleProgramId;

	GLuint color_fragment_id;
	GLuint colorProgramId;
	GLint colorUniform;

	Shaders();
	~Shaders();

	static Shaders *instance;

	void check_shader(GLuint id, const char *src);
	void check_program(GLuint id, const char *name);

public:
	static void initialize();
	static void deinitialize();

	static void simple();
	static void color(const Color &c);
};
