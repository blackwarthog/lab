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

#include "test.h"
#include "measure.h"


using namespace std;


int main() {
	int width = 512;
	int height = 512;

	Rect bounds_file;
	bounds_file.p0 = Vector(0.0, 450.0);
	bounds_file.p1 = Vector(500.0, -50.0);

	Rect bounds_frame;
	bounds_frame.p0 = Vector();
	bounds_frame.p1 = Vector(width, height);

	Rect bounds_gl;
	bounds_gl.p0 = Vector(-1.0, -1.0);
	bounds_gl.p1 = Vector( 1.0,  1.0);

	{
		// lines

		Test::Data data, gldata, datalow;
		Test::load(data, "lines.txt");
		Test::transform(data, bounds_file, bounds_frame);

		gldata = data;
		Test::transform(gldata, bounds_frame, bounds_gl);

		/*
		{ Environment e(width, height, false, false, 8);
		  Measure t("test_lines_gl_stencil.tga", true);
		  Test::test_gl_stencil(e, gldata); }
		{ Environment e(width, height, false, true, 8);
		  Measure t("test_lines_gl_stencil_aa.tga", true);
		  Test::test_gl_stencil(e, gldata); }
		{
			Environment e(width, height, false, false, 8);
			{ Surface surface(width, height);
			  Measure t("test_lines_sw.tga", surface, true);
			  Test::test_sw(e, data, surface); }
			{ Surface surface(width, height);
			  Measure t("test_lines_cl.tga", surface, true);
			  Test::test_cl(e, data, surface); }
			{ Surface surface(width, height);
			  Measure t("test_lines_cl2.tga", surface, true);
			  Test::test_cl2(e, data, surface); }
			{ Surface surface(width, height);
			  Measure t("test_lines_cl3.tga", surface, true);
			  Test::test_cl3(e, data, surface); }
		}
		*/

		{ Measure t("test_lines_downgrade", true); Test::downgrade(data, datalow); }

		/*
		gldata = datalow;
		Test::transform(gldata, bounds_frame, bounds_gl);
		{ Environment e(width, height, false, false, 8);
		  Measure t("test_lineslow_gl_stencil.tga", true);
		  Test::test_gl_stencil(e, gldata); }
		{ Environment e(width, height, false, true, 8);
		  Measure t("test_lineslow_gl_stencil_aa.tga", true);
		  Test::test_gl_stencil(e, gldata); }
		*/
		{
			Environment e(width, height, false, false, 8);
			//{ Surface surface(width, height);
			//  Measure t("test_lineslow_sw.tga", surface, true);
			//  Test::test_sw(e, datalow, surface); }
			/*
			{ Surface surface(width, height);
			  Measure t("test_lineslow_cl.tga", surface, true);
			  Test::test_cl(e, datalow, surface); }
			{ Surface surface(width, height);
			  Measure t("test_lineslow_cl2.tga", surface, true);
			  Test::test_cl2(e, datalow, surface); }
			*/
			{ Surface surface(width, height);
			  Measure t("test_lineslow_cl3.tga", surface, true);
			  Test::test_cl3(e, datalow, surface); }
		}
	}

	if (false ){
		// splines

		Test::Data data, ldata, gldata;
		Test::load(data, "splines.txt");
		Test::transform(data, bounds_file, bounds_frame);
		Environment e(width, height, false, false, 8);

		{ Environment e(width, height, false, true, 8);
		  Measure t("test_splines_gl_stencil.tga", true);
		  { Measure t("split"); Test::split(data, gldata); }
		  Test::transform(gldata, bounds_frame, bounds_gl);
		  Test::test_gl_stencil(e, gldata); }
		{ Environment e(width, height, false, true, 8);
		  Measure t("test_splines_gl_stencil_aa.tga", true);
		  { Measure t("split"); Test::split(data, gldata); }
		  Test::transform(gldata, bounds_frame, bounds_gl);
		  Test::test_gl_stencil(e, gldata); }
		e.use();
		{ Surface surface(width, height);
		  Measure t("test_splines_sw.tga", surface, true);
		  Test::test_sw(e, data, surface); }
		{ Surface surface(width, height);
		  Measure t("test_splines_cl.tga", surface, true);
		  { Measure t("split"); Test::split(data, ldata); }
		  Test::test_cl(e, ldata, surface); }
	}

	cout << "done" << endl;
	return 0;
}
