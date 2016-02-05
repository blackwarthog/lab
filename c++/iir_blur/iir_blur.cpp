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
	int maxi;
	double maxv;
	bool valid;
	double radius;

	Checker(double radius): maxi(), maxv(), valid(), radius(radius) {
		blank.resize((int)ceil(radius*10));
		tmp.resize(blank.size());
		for(int i = 0; i < (int)blank.size(); ++i)
			blank[i] = gauss(i, radius);
	}

	Params convert_params(const Params &params) {
		double a = 1.0/params.k0;
		double b = a*cos(pi*params.k1);
		double c = 1.0/params.k2;

		Params p;
		p.k0 = -(a*a + 2.0*c*b)/(c*a*a);
		p.k1 = (c + 2.0*b)/(c*a*a);
		p.k2 = -1.0/(c*a*a);

		return p;
	}

	double check2(const Params &params) {
		if ( abs(params.k0) > 0.99999999
		  || abs(params.k1) > 0.99999999
		  || abs(params.k2) > 0.99999999
		  || params.k0 < 1e-6
		  || abs(params.k2) < 1e-6 ) return INFINITY;
		return check(convert_params(params), true);
	}

	double check(const Params &params, bool mode3a = true) {
		valid = false;

		//if (mode3a && fabs(params.k0 + (params.k1 + params.k2)/(params.k0 + params.k1 + params.k2)) >= 0.999999999)
		//	return INFINITY;
		if (!mode3a && fabs(params.k1 + params.k2/(params.k1 + params.k2)) >= 0.999999999)
			return INFINITY;
		if (fabs(params.k0) < 1e-10 && fabs(params.k1) < 1e-10 && fabs(params.k2) < 1e-10)
			return INFINITY;

		maxv = 1.0 - params.k0 - params.k1 - params.k2;
		int offset = 3;
		maxi = offset;
		tmp[offset] = 1.0;
		for(int i = offset; i < (int)tmp.size(); ++i) {
			if (mode3a)
				tmp[i] = (i == offset ? maxv : 0.0)
					   + params.k0*tmp[i-1]
					   + params.k1*tmp[i-2]
					   + params.k2*tmp[i-3];
			else
				tmp[i] = (i == offset ? 1.0 : 0.0)
					   + (i-1 == offset ? params.k0 : 0.0)
					   + params.k1*tmp[i-1]
					   + params.k2*tmp[i-2];
			if (i < (int)tmp.size()/2 && maxv < tmp[i]) { maxv = tmp[i]; maxi = i; }
		}

		//if (maxv < 1e-20 || isnan(maxv)) return INFINITY;
		//maxi = (int)round(radius) + offset;

		//double k = blank[0]/maxv;
		double delta = 0.0;
		for(int i = maxi; i < (int)tmp.size(); ++i) {
			double t = tmp[i];
			//double d = blank[i - maxi] - t;
			//delta += d*d;
			//if (t < 0) delta += 5*d*d;
			delta += fabs(blank[i - maxi] - t);
			if (t < 0) delta += fabs(t);
		}

		if (isinf(delta) || isnan(delta)) return INFINITY;

		valid = true;
		return delta;
	}


	void graph(const string &filename) {
		int count = blank.size();
		int scale = 256/count + 1;

		int pad = max(16, count/8);
		int gw = count*scale;
		int gh = gw;

		Surface::Color axis = Surface::convert_color(0, 0, 0, 1);
		Surface::Color ca = Surface::convert_color(0, 0, 1, 0.75);
		Surface::Color cb = Surface::convert_color(1, 0, 0, 0.75);

		Surface s(gw + 2*pad, gh + 2*pad);
		s.clear(1.0, 1.0, 1.0, 1.0);

		s.set_color(axis);
		s.move_to(pad, pad);
		s.line_by(0, gh);
		s.line_by(gw, 0);

		s.set_color(ca);
		s.move_to(pad, pad);
		for(int i = 0; i < count; ++i)
			s.line_to(pad + i*scale, pad + (int)round((1.0 - blank[i]/blank[0])*gh));

		if (valid) {
			s.set_color(cb);
			s.move_to(pad, pad);
			for(int i = 0; i < count - maxi; ++i)
				s.line_to(pad + i*scale, pad + (int)round((1.0 - tmp[i + maxi]/maxv)*gh));
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
				cout << (open ? " begin" : " end")
					 << " " << current.k0
					 << " " << current.k1
					 << " " << current.k2
					 << " " << value
					 << endl;
			}
		}

		double find() {
			if (max_variants <= 0) return value;

			report(true);

			int i[3];
			for(i[0] = 0; i[0] < finder->sub_division; ++i[0])
			for(i[1] = 0; i[1] < finder->sub_division; ++i[1])
			for(i[2] = 0; i[2] < finder->sub_division; ++i[2]) {
				Entry sub;
				sub.finder = finder;
				sub.parent = this;
				sub.level = level + 1;
				sub.max_variants = max_variants/4;
				if (sub.max_variants < 1) sub.max_variants = 1;
				for(int j = 0; j < 3; ++j) {
					sub.min.k[j]     = (max.k[j] - min.k[j])/finder->sub_division*(i[j] + 0.5 - 4.0) + min.k[j];
					sub.current.k[j] = (max.k[j] - min.k[j])/finder->sub_division*(i[j] + 0.5 + 0.0) + min.k[j];
					sub.max.k[j]     = (max.k[j] - min.k[j])/finder->sub_division*(i[j] + 0.5 + 4.0) + min.k[j];
				}
				sub.value = finder->checker.check2(sub.current);
				add_variant(sub);
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

			report(false);
			return value;
		}
	};

	Checker checker;

	int sub_division;
	int max_level;
	int max_report_level;

	Entry root;

	Finder(
		double radius,
		Params min,
		Params max,
		int sub_division,
		int max_variants,
		int max_level,
		int max_report_level
	):
		checker(radius),
		sub_division(sub_division),
		max_level(max_level),
		max_report_level(max_report_level)
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
		return root.find();
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

void log_begin() {
	ofstream f(logfile, ios_base::app);
	f << endl << "double coefs[][7] = {" << endl;
}

void log_params(double radius, int maxi, double maxv, const Params &params, double value) {
	ofstream f(logfile, ios_base::app);
	f << "    { " << radius
	  << ", "     << maxi
	  << setprecision(13)
	  << ", "     << maxv
	  << ", "     << params.k0
	  << ", "     << params.k1
	  << ", "     << params.k2
	  << ", "     << value
	  << " },"    << endl;
	cout << endl;
}

void log_params(const Checker &checker, const Params &params, double value) {
	log_params(checker.radius, checker.maxi, checker.maxv, params, value);
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

void process() {
	const double initial_radius = 8.0;
	//const double min_radius = 1.0;
	//const double max_radius = 2048;
	//const double step = 0.1;
	//const double params_step = 1e-7;

	cout << "find initial params" << endl;
	Finder finder(
		initial_radius,
		Params( 0.25, -1, -1),
		Params( 1,  1,  1),
		256,
		1,
		3,
		5
	);

	finder.find();
	finder.checker.check2(finder.root.current);
	graph(finder.checker, finder.root.current);

	log_begin();
	log_params(finder);

	/*
	cout << "walk back" << endl;
	Params params = finder.root.current;
	for(double r = finder.checker.radius; r > min_radius - 0.5*step; r -= step) {
		Checker checker(r);
		cout << "   " << r << flush;

		double value = find_near(checker, params_step, params);
		graph(checker, params);

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
		cout << "   " << r << flush;

		double value = find_near(checker, params_step, params);
		graph(checker, params);

		cout << " " << params.k0
			 << " " << params.k1
			 << " " << params.k2
			 << " " << value
			 << endl;
	}
	*/

	log_end();
}



int main() {
	process();

	/*
	string logfile = "results/coef.log";

	{ ofstream f(logfile.c_str(), ios_base::app); f << endl << "double coefs[][7] = {" << endl; }

	Params prev(0.914, 0.129, -0.216);
	for(double r = 4.0; r < 2048.1; r *= pow(2.0, 0.125)) {
		cout << endl;
		cout << "find coefficients for radius " << r << endl;

		double x = 0.1*log2(r)/2;

		Finder finder(
			r,
			Params(prev.k0 - x, prev.k1 - x, prev.k2 - x),
			Params(prev.k0 + x, prev.k1 + x, prev.k2 + x),
			128,
			1,
			3,
			5
		);

		finder.find();
		finder.checker.check(finder.root.current);
		finder.checker.graph(strprintf("iir_%08.3f.tga", finder.checker.radius));

		ofstream f(logfile.c_str(), ios_base::app);
		f << "    { " << finder.checker.radius
		  << ", "     << finder.checker.maxi
		  << setprecision(20)
		  << ", "     << finder.checker.maxv
		  << ", "     <<  finder.root.current.k0
		  << ", "     << finder.root.current.k1
		  << ", "     << finder.root.current.k2
		  << ", "     << finder.root.value
		  << " },"    << endl;
		cout << endl;

		prev = finder.root.current;
	}

	{ ofstream f(logfile.c_str(), ios_base::app); f << "};" << endl << endl; }
	*/
}
