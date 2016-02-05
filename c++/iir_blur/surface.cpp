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

#include <cstring>
#include <cmath>

#include <fstream>

#include "surface.h"


using namespace std;


Surface::Surface(int width, int height):
	buffer(new Color[width*height]),
	width(width),
	height(height),
	color(),
	x(),
	y(),
	blending(true)
{
	memset(buffer, 0, sizeof(Color)*width*height);
}

Surface::~Surface()
	{ delete[] buffer; }

unsigned int Surface::convert_channel(double c) {
	int i = (int)round(c*255.0);
	return i < 0 ? 0 : i > 255 ? 255 : (unsigned char)i;
}

unsigned int Surface::blend_channel(unsigned int a, unsigned int dest, unsigned int src)
{
	return (dest*(255-a) + a*src) >> 8;
}

Surface::Color Surface::convert_color(double r, double g, double b, double a) {
	return  convert_channel(r)
		 | (convert_channel(g) << 8)
		 | (convert_channel(b) << 16)
		 | (convert_channel(a) << 24);
}

Surface::Color Surface::blend(Color dest, Color src) {
	unsigned int sa = src >> 24;
	if (sa >= 255) return src; else if (sa == 0) return dest;
	unsigned int da = (dest >> 24) + sa;
	if (da > 255) da = 255;
	return  blend_channel(sa, dest | 0xff, src | 0xff)
		 | (blend_channel(sa, (dest >> 8) | 0xff, (src >> 8) | 0xff) << 8)
		 | (blend_channel(sa, (dest >> 16) | 0xff, (src >> 16) | 0xff) << 16)
		 | (da << 24);
}

void Surface::line(Color color, int x0, int y0, int x1, int y1, bool blending) {
	if (x1 < x0) swap(x1, x0);
	if (y1 < y0) swap(y1, y0);
	int pdx = y1 - y0;
	int pdy = x0 - x1;
	int d = 0;
	for(int x = x0, y = y0; x <= x1 && y <= y1;) {
		if (blending) blend_point(x, y, color); else set_point(x, y, color);
		if (abs(d+pdx) < abs(d+pdy))
			{ d += pdx; ++x; } else { d += pdy; ++y; }
	}
}

void Surface::rect(Color color, int x0, int y0, int x1, int y1, bool blending) {
	if (x1 < x0) swap(x1, x0);
	if (y1 < y0) swap(y1, y0);
	for(int x = x0; x < x1; ++x)
		for(int y = y0; y < y1; ++y)
			if (blending) blend_point(x, y, color); else set_point(x, y, color);
}

void Surface::save(const string &filename) const
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
	if (true) {
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
