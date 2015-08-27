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

#include <iostream>

#include "shaders.h"


using namespace std;


Shaders::Shaders():
	simple_vertex_id(),
	simpleProgramId(),
	color_fragment_id(),
	colorProgramId(),
	colorUniform()
{
	// simple
	const char *simpleVertexSource =
		"in vec2 position;\n"
		"void main() { gl_Position = vec4(position, 0.0, 1.0); }\n";

	simple_vertex_id = glCreateShader(GL_VERTEX_SHADER);
	glShaderSource(simple_vertex_id, 1, &simpleVertexSource, NULL);
	glCompileShader(simple_vertex_id);
	check_shader(simple_vertex_id, simpleVertexSource);

	simpleProgramId = glCreateProgram();
	glAttachShader(simpleProgramId, simple_vertex_id);
	glBindAttribLocation(simpleProgramId, 0, "position");
	glLinkProgram(simpleProgramId);
	check_program(simpleProgramId, "simple");

	// color
	const char *colorFragmentSource =
		"uniform vec4 color;\n"
		//"out vec4 colorOut;\n"
		"void main() { gl_FragColor = color; }\n";

	color_fragment_id = glCreateShader(GL_FRAGMENT_SHADER);
	glShaderSource(color_fragment_id, 1, &colorFragmentSource, NULL);
	glCompileShader(color_fragment_id);
	check_shader(color_fragment_id, colorFragmentSource);

	colorProgramId = glCreateProgram();
	glAttachShader(colorProgramId, simple_vertex_id);
	glAttachShader(colorProgramId, color_fragment_id);
	glBindAttribLocation(colorProgramId, 0, "position");
	//glBindFragDataLocation(color_program_id, 0, "colorOut");
	glLinkProgram(colorProgramId);
	check_program(colorProgramId, "color");
	colorUniform = glGetUniformLocation(colorProgramId, "color");
}

Shaders::~Shaders() {
	glUseProgram(0);
	glDeleteProgram(colorProgramId);
	glDeleteProgram(simpleProgramId);
	glDeleteShader(color_fragment_id);
	glDeleteShader(simple_vertex_id);
}

void Shaders::check_shader(GLuint id, const char *src) {
	GLint compileStatus = 0;
	glGetShaderiv(id, GL_COMPILE_STATUS, &compileStatus);
	if (!compileStatus) {
		GLint infoLogLength = 0;
		glGetShaderiv(id, GL_INFO_LOG_LENGTH, &infoLogLength);
		std::string infoLog;
		infoLog.resize(infoLogLength);
		glGetShaderInfoLog(id, infoLog.size(), &infoLogLength, &infoLog[0]);
		infoLog.resize(infoLogLength);
		cout << "~~~~~~~~~~~~~~~~~~~~~~~~~~~" << endl
			 << "cannot compile shader:" << endl
			 << "~~~~~~source~~~~~~~~~~~~~~~" << endl
			 << src << endl
			 << "~~~~~~log~~~~~~~~~~~~~~~~~~" << endl
			 << infoLog << endl
			 << "~~~~~~~~~~~~~~~~~~~~~~~~~~~" << endl;
	}
}

void Shaders::check_program(GLuint id, const char *name) {
 	GLint linkStatus = 0;
	glGetProgramiv(id, GL_LINK_STATUS, &linkStatus);
	if (!linkStatus) {
		GLint infoLogLength = 0;
		glGetProgramiv(id, GL_INFO_LOG_LENGTH, &infoLogLength);
		std::string infoLog;
		infoLog.resize(infoLogLength);
		glGetProgramInfoLog(id, infoLog.size(), &infoLogLength, &infoLog[0]);
		infoLog.resize(infoLogLength);
		cout << "~~~~~~~~~~~~~~~~~~~~~~~~~~~" << endl
			 << "cannot link program " << name <<  ":" << endl
			 << "~~~~~~name~~~~~~~~~~~~~~~~~" << endl
			 << name << endl
			 << "~~~~~~log~~~~~~~~~~~~~~~~~~" << endl
			 << infoLog << endl
			 << "~~~~~~~~~~~~~~~~~~~~~~~~~~~" << endl;
	}

	glValidateProgram(id);
	GLint validateStatus = 0;
	glGetProgramiv(id, GL_VALIDATE_STATUS, &validateStatus);
	if (!validateStatus) {
		GLint infoLogLength = 0;
		glGetProgramiv(id, GL_INFO_LOG_LENGTH, &infoLogLength);
		std::string infoLog;
		infoLog.resize(infoLogLength);
		glGetProgramInfoLog(id, infoLog.size(), &infoLogLength, &infoLog[0]);
		infoLog.resize(infoLogLength);
		cout << "~~~~~~~~~~~~~~~~~~~~~~~~~~~" << endl
			 << "program not validated " << name <<  ":" << endl
			 << "~~~~~~name~~~~~~~~~~~~~~~~~" << endl
			 << name << endl
			 << "~~~~~~log~~~~~~~~~~~~~~~~~~" << endl
			 << infoLog << endl
			 << "~~~~~~~~~~~~~~~~~~~~~~~~~~~" << endl;
	}
}

void Shaders::simple() {
	glUseProgram(simpleProgramId);
}

void Shaders::color(const Color &c) {
	glUseProgram(colorProgramId);
	glUniform4fv(colorUniform, 1, c.channels);
}


