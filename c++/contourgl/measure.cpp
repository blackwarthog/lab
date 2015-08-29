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

#include <iostream>
#include <iomanip>

#include "measure.h"
#include "utils.h"
#include "glcontext.h"


using namespace std;


std::vector<Measure*> Measure::stack;


Measure::Measure(const std::string &filename):
	filename(filename),
	surface(),
	tga(filename.size() > 4 && filename.substr(filename.size()-4, 4) == ".tga"),
	sub_tasks(),
	t()
{
	cout << string(stack.size()*2, ' ') << "begin             " << filename << endl << flush;
	stack.push_back(this);
	t = clock();
}

Measure::Measure(const std::string &filename, Surface &surface):
	filename(filename),
	surface(&surface),
	tga(filename.size() > 4 && filename.substr(filename.size()-4, 4) == ".tga"),
	sub_tasks(),
	t()
{
	cout << string(stack.size()*2, ' ') << "begin             " << filename << endl << flush;
	stack.push_back(this);
	t = clock();
}

Measure::~Measure() {
	if (!surface && tga) glFinish();

	clock_t dt = sub_tasks ? sub_tasks : clock() - t;
	Real ms = 1000.0*(Real)dt/(Real)(CLOCKS_PER_SEC);

	cout << string((stack.size()-1)*2, ' ') << "end "
		 << setw(8) << fixed << setprecision(3)
		 << ms << " ms - "
		 << filename
		 << endl << flush;

	if (tga) {
		if (surface)
			Utils::save_surface(*surface, filename);
		else
			Utils::save_viewport(filename);
	}

	if (surface) {
		surface->clear();
	} else {
		glClear(GL_COLOR_BUFFER_BIT);
		glFinish();
	}

	stack.pop_back();
	if (!stack.empty()) stack.back()->sub_tasks += dt;
}

