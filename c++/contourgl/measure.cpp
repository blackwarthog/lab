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

#include <time.h>

#include "measure.h"
#include "utils.h"
#include "glcontext.h"


using namespace std;


std::vector<Measure*> Measure::stack;


void Measure::init() {
	hide = !stack.empty() && stack.back()->hide_subs;
	hide_subs |= hide;
	tga = filename.size() > 4 && filename.substr(filename.size()-4, 4) == ".tga";
	if (!hide)
		cout << string(stack.size()*2, ' ')
		     << "begin             "
			 << filename
			 << endl << flush;
	stack.push_back(this);

	timespec spec;
	clock_gettime(CLOCK_MONOTONIC , &spec);
	t = spec.tv_sec*1000000000 + spec.tv_nsec;
}

Measure::~Measure() {
	if (!surface && tga) glFinish();

	long long dt;
	if (has_subs) {
		dt = subs;
		if (!repeats.empty()) {
			// remove 25% of minimal values and 25% of maximum values
			for(int i = (int)repeats.size()/10; i; --i) {
				vector<long long>::iterator j, jj;
				for(jj = j = repeats.begin(); j != repeats.end(); ++j) if (*j < *jj) jj = j;
				repeats.erase(jj);
				for(jj = j = repeats.begin(); j != repeats.end(); ++j) if (*j > *jj) jj = j;
				repeats.erase(jj);
			}
			// get average
			long long sum = 0;
			for(vector<long long>::iterator j = repeats.begin(); j != repeats.end(); ++j) sum += *j;
			dt += sum/repeats.size();
		}
	} else {
		timespec spec;
		clock_gettime(CLOCK_MONOTONIC , &spec);
		dt = spec.tv_sec*1000000000 + spec.tv_nsec - t;
	}
	Real ms = 1000.0*1e-9*(Real)dt;

	if (!hide)
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
	} else
	if (tga) {
		glClear(GL_COLOR_BUFFER_BIT);
		glFinish();
	}

	stack.pop_back();
	if (!stack.empty()) {
		stack.back()->has_subs = true;
		if (repeat) stack.back()->repeats.push_back(dt);
		       else stack.back()->subs += dt;
	}
}

