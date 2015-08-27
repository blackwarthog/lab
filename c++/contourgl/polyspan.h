/*
	polyspan.h
	Polyspan Header

	Copyright (c) 2002-2005 Robert B. Quattlebaum Jr., Adrian Bentley
	Copyright (c) 2007, 2008 Chris Moore
	Copyright (c) 2012-2013 Carlos López
	......... ... 2015 Ivan Mahonin

	This package is free software; you can redistribute it and/or
	modify it under the terms of the GNU General Public License as
	published by the Free Software Foundation; either version 2 of
	the License, or (at your option) any later version.

	This package is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
	General Public License for more details.
*/

#ifndef _POLYSPAN_H_
#define _POLYSPAN_H_

#include <vector>

#include "geometry.h"

class Polyspan {
public:
	struct PenMark {
		int y, x;
		Real cover, area;

		PenMark(): y(), x(), cover(), area() { }
		PenMark(int xin, int yin, Real c, Real a):
			y(yin), x(xin), cover(c), area(a) { }
		void set(int xin, int yin, Real c, Real a)
			{ y = yin; x = xin; cover = c; area = a; }
		void setcoord(int xin, int yin)
			{ y = yin; x = xin;	}
		void setcover(Real c, Real a)
			{ cover	= c; area = a; }
		void addcover(Real c, Real a)
			{ cover += c; area += a; }
		bool operator < (const PenMark &rhs) const
			{ return y == rhs.y ? x < rhs.x : y < rhs.y; }
	};

	typedef	std::vector<PenMark> cover_array;

	//for assignment to flags value
	enum PolySpanFlags {
		NotSorted = 0x8000,
		NotClosed =	0x4000
	};

	enum {
		MAX_SUBDIVISION_SIZE = 64,
		MIN_SUBDIVISION_DRAW_LEVELS = 4
	};

private:
	Vector			arc[3*MAX_SUBDIVISION_SIZE + 1];

	cover_array		covers;
	PenMark			current;

	int				open_index;

	//ending position of last primitive
	Real			cur_x;
	Real			cur_y;

	//starting position of current primitive list
	Real			close_x;
	Real			close_y;

	//flags for the current segment
	int				flags;

	//the window that will be drawn (used for clipping)
	ContextRect		window;

	//add the current cell, but only if there is information to add
	void addcurrent();

	//move to the next cell (cover values 0 initially), keeping the current if necessary
	void move_pen(int x, int y);

	static bool clip_conic(const Vector *p, const ContextRect &r);
	static Real max_edges_conic(const Vector *p);
	static void subd_conic_stack(Vector *arc);

	static bool clip_cubic(const Vector *p, const ContextRect &r);
	static Real max_edges_cubic(const Vector *p);
	static void subd_cubic_stack(Vector *arc);

public:
	Polyspan();

	const ContextRect& get_window() const { return window; }
	const cover_array& get_covers() const { return covers; }

	bool notclosed() const
		{ return (flags & NotClosed) || (cur_x != close_x) || (cur_y != close_y); }

	//0 out all the variables involved in processing
	void clear();
	void init(const ContextRect &window)
		{ clear(); this->window = window; }
	void init(int minx, int miny, int maxx, int maxy)
	{
		clear();
		window.minx = minx;
		window.miny = miny;
		window.maxx = maxx;
		window.maxy = maxy;
	}

	//close the primitives with a line (or rendering will not work as expected)
	void close();

	// Not recommended - destroys any separation of spans currently held
	void merge_all();

	//will sort the marks if they are not sorted
	void sort_marks();

	//encapsulate the current sublist of marks (used for drawing)
	void encapsulate_current();

	//move to start a new primitive list (enclose the last primitive if need be)
	void move_to(Real x, Real y);

	//primitive_to functions
	void line_to(Real x, Real y);
	void conic_to(Real x1, Real y1, Real x, Real y);
	void cubic_to(Real x1, Real y1, Real x2, Real y2, Real x, Real y);

	void draw_scanline(int y, Real x1, Real y1, Real x2, Real y2);
	void draw_line(Real x1, Real y1, Real x2, Real y2);

	Real extract_alpha(Real area, bool evenodd) const;
};

#endif
