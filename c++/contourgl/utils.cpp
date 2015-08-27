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

#include "utils.h"
#include "glcontext.h"


using namespace std;


Vector Utils::get_frame_size() {
	int vp[4] = {};
	glGetIntegerv(GL_VIEWPORT, vp);
	return Vector((Real)vp[2], (Real)vp[3]);
}

void Utils::save_rgba(
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

void Utils::save_viewport(const string &filename) {
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

void Utils::save_surface(const Surface &surface, const string &filename) {
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
