/*
    ......... 2016 Ivan Mahonin

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

#include <cstdarg>
#include <cstdio>
#include <cstring>
#include <cmath>

#include <iostream>
#include <iomanip>
#include <fstream>
#include <vector>
#include <set>

#include "surface.h"


using namespace std;


#define COMPARE_HELPER(a) a < other.a ? true : other.a < a ? false :

#define COMPARE(a) a < other.a;
#define COMPARE2(a, b) COMPARE_HELPER(a) COMPARE(b)
#define COMPARE3(a, b, c) COMPARE_HELPER(a) COMPARE2(b, c)
#define COMPARE4(a, b, c, d) COMPARE_HELPER(a) COMPARE3(b, c, d)


const double pi = 3.14159265358979323846;
const char logfile[] = "results/coef.log";

string strprintf(const char *format, ...)
{
	va_list args;
	va_start(args,format);
	static char buffer[1024*1024];
	vsprintf(buffer, format, args);
	return buffer;
}

template<typename T>
inline bool less(const T &a, const T &b, bool def)
	{ return a < b ? true : b < a ? false : def; }


double gauss(double x, double radius) {
	static const double k = 1.0/sqrt(2.0*pi);
	return k/radius*exp(-0.5*x*x/(radius*radius));
}


class Params {
public:
	union {
		struct { double k0, k1, k2; };
		double k[3];
	};

	Params(): k0(), k1(), k2() { }
	Params(double k0, double k1, double k2): k0(k0), k1(k1), k2(k2) { }


	bool operator< (const Params &other) const
		{ return COMPARE3(k0, k1, k2); }
};


class Checker {
private:
	vector<double> blank;
	vector<double> tmp;

public:
	int offset;
	bool valid;
	double radius;

	Checker(double radius): offset(3), valid(), radius(radius) {
		blank.resize((int)ceil(radius*10));
		tmp.resize(blank.size() + 2*offset);
		for(int i = 0; i < (int)blank.size(); ++i)
			blank[i] = gauss(i, radius);
	}

	Params convert_params(const Params &params) {
		double a = 1.0/params.k0;
		double b = a*cos(pi*params.k1);
		double c = 1.0/params.k2;

		Params p;
		p.k0 = (a*a + 2.0*c*b)/(c*a*a);
		p.k1 = -(c + 2.0*b)/(c*a*a);
		p.k2 = 1.0/(c*a*a);

		return p;
	}

	double check2(const Params &params) {
		if ( abs(params.k0) > 0.99999999
		  //|| abs(params.k1) > 0.99999999
		  || abs(params.k2) > 0.99999999
		  || params.k0 < 1e-6
		  || abs(params.k2) < 1e-6 ) return INFINITY;
		return check(convert_params(params));
		//return check(params);
	}

	double check(const Params &params) {
		valid = false;

		//if (mode3a && fabs(params.k0 + (params.k1 + params.k2)/(params.k0 + params.k1 + params.k2)) >= 0.999999999)
		//	return INFINITY;
		if (fabs(params.k0) < 1e-10 && fabs(params.k1) < 1e-10 && fabs(params.k2) < 1e-10)
			return INFINITY;

		double k = 1.0 - params.k0 - params.k1 - params.k2;
		tmp[offset] = k;
		for(int i = offset+1, end = (int)tmp.size()-offset; i < end; ++i)
			tmp[i] = params.k0*tmp[i-1]
				   + params.k1*tmp[i-2]
				   + params.k2*tmp[i-3];
		for(int i = (int)tmp.size()-offset-1, end = offset-1; i > end; --i)
			tmp[i] = k*tmp[i]
				   + params.k0*tmp[i+1]
				   + params.k1*tmp[i+2]
				   + params.k2*tmp[i+3];

		double delta = 0.0;
		//double minv = tmp[offset];
		//double kblank = 1.0/blank[0];
		for(int i = 0; i < (int)blank.size(); ++i) {
			double t = tmp[i + offset];
			double d = blank[i] - t;
			delta += fabs(d*d);
			//if (t < minv) minv = t;
			//if (minv < 0) minv = 0;
			//delta += 2*fabs(t - minv);
			//delta += 0.1*fabs(d/(kblank*blank[i] + 0.1));
		}

		if (isinf(delta) || isnan(delta)) return INFINITY;

		valid = true;
		return sqrt(delta/blank.size())*blank.size();
	}


	void graph(const string &filename) {
		int base_size = 768;
		int count = blank.size();
		int scale = base_size/count + 1;
		int div   = count/768 + 1;

		int pad = max(16, count*scale/div/8);
		int gw = count*scale/div;
		int gh = gw;

		Surface::Color axis = Surface::convert_color(0, 0, 1, 1);
		Surface::Color ca = Surface::convert_color(1, 0, 0, 0.25);
		Surface::Color cb = Surface::convert_color(0, 0, 0, 0.75);

		Surface s(gw + 2*pad, gh + 2*pad);
		s.clear(1.0, 1.0, 1.0, 1.0);

		s.set_color(axis);
		s.move_to(pad, pad);
		s.line_by(0, gh);
		s.line_by(gw, 0);

		s.set_color(ca);
		s.move_to(pad, pad);
		for(int i = 0; i < count; ++i)
			s.line_to(pad + i*scale/div, pad + (int)round((1.0 - blank[i]/blank[0])*gh));

		if (valid) {
			s.set_color(cb);
			s.move_to(pad, pad);
			for(int i = 0; i < count; ++i)
				s.line_to(pad + i*scale/div, pad + (int)round((1.0 - tmp[i + offset]/blank[0])*gh));
		}

		s.save(filename);
	}
};


class Finder {
public:
	class Entry {
	public:
		double value;
		Params min;
		Params current;
		Params max;

		Finder *finder;
		Entry *parent;
		int level;
		int index;
		int max_variants;
		set<Entry> variants;

		Entry(): value(INFINITY), finder(), parent(), level(), index(), max_variants() { }

		bool operator< (const Entry &other) const
			{ return COMPARE4(value, min, current, max); }

		void add_variant(const Entry &entry) {
			if (isinf(entry.value) || isnan(entry.value) || max_variants == 0) return;
			if ((int)variants.size() < max_variants || entry.value < variants.rbegin()->value) {
				variants.insert(entry);
				if (max_variants < (int)variants.size()) {
					set<Entry>::iterator i = variants.end();
					variants.erase(--i);
				}
			}
		}

		void report_index() {
			if (parent) {
				parent->report_index();
				cout << "["  << index << "]";
			}
		}

		void report(bool open) {
			if (level < finder->max_report_level) {
				cout << "level " << level << " ";
				report_index();
				for(int i = level; i < finder->max_report_level; ++i)
					cout << "   ";
				cout << (open ? " begin" : " end  ")
					 << fixed
					 << setprecision(8)
					 << " " << current.k0
					 << " " << current.k1
					 << " " << current.k2
					 << " " << value
					 << " (" << finder->best << ")"
					 << endl;
			}
		}

		double find() {
			if (max_variants <= 0) return value;

			if (value < finder->best) finder->best = value;
			report(true);

			int i[3];
			for(i[0] = 0; i[0] < finder->sub_division; ++i[0])
			for(i[1] = 0; i[1] < finder->sub_division; ++i[1])
			for(i[2] = 0; i[2] < finder->sub_division; ++i[2]) {
				Params p;
				for(int j = 0; j < 3; ++j)
					p.k[j] = (max.k[j] - min.k[j])/finder->sub_division*(i[j] + 0.5) + min.k[j];
				finder->value(i[0], i[1], i[2]) = finder->checker.check2(p);
			}

			finder->clear_zones();
			for(i[0] = 1; i[0] < finder->sub_division-1; ++i[0])
			for(i[1] = 1; i[1] < finder->sub_division-1; ++i[1])
			for(i[2] = 1; i[2] < finder->sub_division-1; ++i[2])
				if (finder->is_local_minimum(i[0], i[1], i[2])) {
					Zone &z = finder->zone_for_value(i[0], i[1], i[2]);
					double &v = finder->value(i[0], i[1], i[2]);
					if (v < z.value) z.set(v, i);
				}

			for(i[0] = 0; i[0] < finder->zone_count; ++i[0])
			for(i[1] = 0; i[1] < finder->zone_count; ++i[1])
			for(i[2] = 0; i[2] < finder->zone_count; ++i[2]) {
				Zone &z = finder->zone(i[0], i[1], i[2]);
				if (!isinf(z.value)) {
					Entry sub;
					sub.finder = finder;
					sub.parent = this;
					sub.level = level + 1;
					sub.max_variants = max_variants/2;
					if (sub.max_variants < 1) sub.max_variants = 1;
					for(int j = 0; j < 3; ++j) {
						sub.min.k[j]     = (max.k[j] - min.k[j])/finder->sub_division*(z.index[j] + 0.5 - finder->zone_size) + min.k[j];
						sub.current.k[j] = (max.k[j] - min.k[j])/finder->sub_division*(z.index[j] + 0.5) + min.k[j];
						sub.max.k[j]     = (max.k[j] - min.k[j])/finder->sub_division*(z.index[j] + 0.5 + finder->zone_size) + min.k[j];
					}
					sub.value = z.value;
					add_variant(sub);
				}
			}

			if (level < finder->max_level) {
				vector<Entry> v(variants.begin(), variants.end());
				variants.clear();
				for(vector<Entry>::iterator i = v.begin(); i != v.end(); ++i) {
					i->index = i - v.begin();
					i->find();
					variants.insert(*i);
				}
			}

			if (!variants.empty() && variants.begin()->value < value) {
				value = variants.begin()->value;
				current = variants.begin()->current;
			}

			if (value < finder->best) finder->best = value;
			report(false);
			return value;
		}
	};

	struct Zone {
		double value;
		int index[3];

		Zone() { clear(); }
		void clear() { value = INFINITY; memset(index, 0, sizeof(index)); }
		void set(double value, int *i) { this->value = value; memcpy(index, i, sizeof(index)); }
	};

	Checker checker;

	int sub_division;
	int zone_count;
	int zone_size;
	int s0;
	int s1;
	int s2;
	int max_level;
	int max_report_level;
	vector<double> values;
	vector<Zone> zones;

	double best;
	Entry root;

	Finder(
		double radius,
		Params min,
		Params max,
		int zone_count,
		int zone_size,
		int max_variants,
		int max_level,
		int max_report_level
	):
		checker(radius),
		sub_division(zone_count*zone_size),
		zone_count(zone_count),
		zone_size(zone_size),
		s0(1),
		s1(sub_division),
		s2(sub_division*sub_division),
		max_level(max_level),
		max_report_level(max_report_level),
		values(sub_division*sub_division*sub_division),
		zones(zone_count*zone_count*zone_count),
		best(INFINITY)
	{
		root.finder = this;
		root.min = min;
		root.max = max;
		root.max_variants = max_variants;
	}

	double find() {
		for(int j = 0; j < 3; ++j)
			root.current.k[j] = 0.5*(root.max.k[j] - root.min.k[j]) + root.min.k[j];
		root.value = checker.check2(root.current);
		best = root.value;
		return root.find();
	}

	double& value(int i0, int i1, int i2)
		{ return values[i0*s0 + i1*s1 + i2*s2]; }

	bool is_local_minimum(int i0, int i1, int i2) {
		double *vp = &value(i0, i1, i2);
		double v = *vp;
		return v < *(vp-s0) && v <= *(vp+s0)
			&& v < *(vp-s1) && v <= *(vp+s1)
			&& v < *(vp-s2) && v <= *(vp+s2);
	}

	Zone& zone_for_value(int i0, int i1, int i2)
		{ return zones[(i2/zone_size*zone_count + i1/zone_size)*zone_count + i0/zone_size]; }
	Zone& zone(int i0, int i1, int i2)
		{ return zones[(i2*zone_count + i1)*zone_count + i0]; }

	void clear_zones() {
		for(vector<Zone>::iterator i = zones.begin(); i != zones.end(); ++i)
			i->clear();
	}
};

double find_near(Checker &checker, double step, Params &params) {
	double best_value = checker.check2(params);
	Params best_params = params;
	while(true) {
		bool found = false;
		for(int i = 0; i < 3; ++i) {
			for(int j = -1; j <= 1; j += 2) {
				Params p = params;
				p.k[i] += j*step;
				double v = checker.check2(p);
				if (v < best_value) { best_value = v; best_params = p; }
			}
		}
		if (found) params = best_params; else break;
	}
	return best_value;
}

double find_near2(Checker &checker, double step, Params &params) {
	Finder finder(
		checker.radius,
		Params(params.k0-step, params.k1-step, params.k2-step),
		Params(params.k0+step, params.k1+step, params.k2+step),
		4,
		4,
		4,
		16,
		1
	);
	finder.find();
	params = finder.root.current;
	return finder.root.value;
}

void log_begin() {
	ofstream f(logfile, ios_base::app);
	f << endl << "double coefs[][7] = {" << endl;
}

void log_params(double radius, const Params &params, double value) {
	ofstream f(logfile, ios_base::app);
	f << "    { " << radius
	  << setprecision(13)
	  << ", "     << params.k0
	  << ", "     << params.k1
	  << ", "     << params.k2
	  << ", "     << value
	  << " },"    << endl;
	cout << endl;
}

void log_params(const Checker &checker, const Params &params, double value) {
	log_params(checker.radius, params, value);
}

void log_params(const Finder &finder) {
	log_params(finder.checker, finder.root.current, finder.root.value);
}

void log_end() {
	ofstream f(logfile, ios_base::app);
	f << "};" << endl << endl;
}

void graph(Checker &checker, const Params &params) {
	int ri = roundf(checker.radius);
	if (fabs(checker.radius - ri) > 1e-5) return;

	bool is_power_of_two = false;
	for(int i = 1; i <= 4096; i *= 2)
		if (ri == i) is_power_of_two = true;

	if (!is_power_of_two && ri%16 != 0)
		return;

	checker.check2(params);
	checker.graph(strprintf("iir_%04d.tga", ri));
}

void test() {
	Checker checker(16);
	Params p(0.9304775714874, -0.02728819847107, 0.9257968425751);
	cout << fixed << setprecision(8) << checker.check2(p) << endl;
	checker.graph("test.tga");
}

void process() {
	const double initial_radius = 16.0;
	const double min_radius = 1.0;
	const double max_radius = 2048;
	const double step = 0.1;
	const double params_step = 0.05;

	cout << "find initial params" << endl;
	Finder finder(
		initial_radius,
		Params(0, -1, -1),
		Params(1,  1,  1),
		16,
		16,
		1,
		4,
		8
	);

	finder.find();
	cout << finder.checker.check2(finder.root.current) << endl;
	cout << find_near(finder.checker, params_step, finder.root.current) << endl;
	graph(finder.checker, finder.root.current);

	log_begin();
	log_params(finder);

	cout << "walk back" << endl;
	Params params = finder.root.current;
	for(double r = finder.checker.radius; r > min_radius - 0.5*step; r -= step) {
		Checker checker(r);
		cout << "   " << r << endl;

		double value = find_near2(checker, params_step, params);
		checker.check2(params);
		graph(checker, params);
		if (abs(round(r*10) - r*10) < 1e-10)
			log_params(checker, params, value);

		cout << " " << params.k0
			 << " " << params.k1
			 << " " << params.k2
			 << " " << value
			 << endl;
	}

	cout << "walk forward" << endl;
	params = finder.root.current;
	for(double r = finder.checker.radius; r < max_radius + 0.5*step; r += step) {
		Checker checker(r);
		cout << "   " << r << endl;

		double value = find_near2(checker, params_step, params);
		graph(checker, params);
		checker.check2(params);
		if (abs(round(r*10) - r*10) < 1e-10)
			log_params(checker, params, value);

		cout << " " << params.k0
			 << " " << params.k1
			 << " " << params.k2
			 << " " << value
			 << endl;
	}

	log_end();
}



int main() {
	test();
	process();
}
