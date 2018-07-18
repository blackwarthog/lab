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

#ifndef _MEASURE_H_
#define _MEASURE_H_

#include <vector>
#include <string>

#include "swrender.h"


class Measure {
private:
	static std::vector<Measure*> stack;
	std::string filename;
	Surface *surface;
	bool tga;
	bool hide;
	bool hide_subs;
	long long subs;
	long long t;

	Measure(const Measure&): surface(), tga(), hide(), hide_subs(), subs(), t() { }
	Measure& operator= (const Measure&) { return *this; }
	void init();
public:
	Measure(const std::string &filename, bool hide_subs = false):
		filename(filename), surface(), tga(), hide(), hide_subs(hide_subs), subs(), t()
	{ init(); }

	Measure(const std::string &filename, Surface &surface, bool hide_subs = false):
		filename(filename), surface(&surface), tga(), hide(), hide_subs(hide_subs), subs(), t()
	{ init(); }

	~Measure();
};

#endif
