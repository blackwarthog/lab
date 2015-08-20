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

#include "contourbuilder.h"

using namespace std;

void ContourBuilder::build_simple(vector<Vector> &c) {
	const float min_segment_length = 0.001f;
	const float rounds = 10.f;
	const float rounds2 = 1.f;

	vector<Vector> back;

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

		c.push_back(Vector(fx, fy));
		back.push_back(Vector(bx, by));

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

		c.push_back(Vector(fx, fy));
		back.push_back(Vector(bx, by));

		angle += step;
	}


	// go back
	c.reserve(c.size() + back.size() + 1);
	for(vector<Vector>::reverse_iterator ri = back.rbegin(); ri != back.rend(); ++ri)
		c.push_back(*ri);

	// close
	c.push_back(c.front());
}

void ContourBuilder::build_car(Contour &c, const Vector &o, double s) {
	c.move_to(  Vector( 5, -1)*s + o);
	c.line_to(  Vector( 4, -1)*s + o);
	c.conic_to( Vector( 2, -1)*s + o, Vector( 0, -1)*s);
	c.line_to(  Vector(-2, -1)*s + o);
	c.conic_to( Vector(-4, -1)*s + o, Vector( 0, -1)*s);
	c.line_to(  Vector(-5, -1)*s + o);
	c.line_to(  Vector(-5,  1)*s + o);
	c.line_to(  Vector(-4,  1)*s + o);
	c.cubic_to( Vector(-1,  3)*s + o, Vector( 0,  2)*s, Vector( 4,  0)*s);
	c.cubic_to( Vector( 3,  1)*s + o, Vector( 4,  0)*s, Vector( 2, -2)*s);
	c.line_to(  Vector( 5,  1)*s + o);
	c.close();
}

void ContourBuilder::build(Contour &c) {
	double scale = 0.8/5.0;

	int count = 100;
	double size = (double)(count + 2)/(double)(count);
	double step = 2.0*size/(double)(count + 1);
	double origin = step - size;
	double s = 2*size*scale/(double)(count);
	for(int i = 0; i < count; ++i)
		for(int j = 0; j < count; ++j)
			build_car(c, Vector(origin + i*step, origin + j*step), s);

	count = 100;
	size = (double)(count + 2)/(double)(count);
	step = 2.0*size/(double)(count + 1);
	origin = step - size;
	s = size*scale/(double)(count);
	for(int i = 0; i < count; ++i)
		for(int j = 0; j < count; ++j)
			build_car(c, Vector(origin + i*step, origin + j*step), s);

	build_car(c, Vector::zero(), scale);
	build_car(c, Vector::zero(), 0.5*scale);
}

