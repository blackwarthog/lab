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

#include <ctime>

#include <vector>
#include <string>

#include "rendersw.h"


class Measure {
private:
	static std::vector<Measure*> stack;
	std::string filename;
	Surface *surface;
	bool tga;
	clock_t sub_tasks;
	clock_t t;

	Measure(const Measure&): surface(), tga(), sub_tasks(), t() { }
	Measure& operator= (const Measure&) { return *this; }
public:
	Measure(const std::string &filename);
	Measure(const std::string &filename, Surface &surface);
	~Measure();
};

#endif
