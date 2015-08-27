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

#include "clrender.h"


using namespace std;


ClRender::ClRender(ClContext &cl): cl(cl) {
	// TODO: init programs
}

ClRender::~ClRender() {
	// TODO: release programs
}

void ClRender::contour(const Contour &contour, Surface &target) {
	// TODO:
	// split contour
	// resterize lines
	// fill
}



void SwRenderAlt::line(const Vector &p0, const Vector &p1) {
	int iy0 = min(max((int)floor(p0.y), 0), height);
	int iy1 = min(max((int)floor(p1.y), 0), height);
	if (iy1 < iy0) swap(iy0, iy1);

	Vector d = p1 - p0;
	Vector k( fabs(d.y) < 1e-6 ? 0.0 : d.x/d.y,
		      fabs(d.x) < 1e-6 ? 0.0 : d.y/d.x );

	for(int r = iy0; r <= iy1; ++r) {
		Real y = (Real)iy0;

		Vector pp0 = p0;
		pp0.y -= y;
		if (pp0.y < 0.0) {
			pp0.y = 0.0;
			pp0.x = p0.x - k.x*y;
		} else
		if (pp0.y > 1.0) {
			pp0.y = 1.0;
			pp0.x = p0.x - k.x*(y - 1.0);
		}

		Vector pp1 = p1;
		pp1.y -= y;
		if (pp1.y < 0.0) {
			pp1.y = 0.0;
			pp1.x = p0.x - k.x*y;
		} else
		if (pp1.y > 1.0) {
			pp1.y = 1.0;
			pp1.x = p0.x - k.x*(y - 1.0);
		}

		int ix0 = min(max((int)floor(pp0.x), 0), width);
		int ix1 = min(max((int)floor(pp1.x), 0), width);
		if (ix1 < ix0) swap(ix0, ix1);
		for(int c = ix0; c <= ix1; ++c) {
			Real x = (Real)ix0;

			Vector ppp0 = pp0;
			ppp0.x -= x;
			if (ppp0.x < 0.0) {
				ppp0.x = 0.0;
				ppp0.y = pp0.y - k.y*x;
			} else
			if (ppp0.x > 1.0) {
				ppp0.x = 1.0;
				ppp0.y = pp0.y - k.y*(x - 1.0);
			}

			Vector ppp1 = pp1;
			ppp1.x -= x;
			if (ppp1.x < 0.0) {
				ppp1.x = 0.0;
				ppp1.y = pp0.y - k.y*x;
			} else
			if (ppp1.x > 1.0) {
				ppp1.x = 1.0;
				ppp1.y = pp0.y - k.y*(x - 1.0);
			}

			Real cover = ppp0.y - ppp1.y;
			Real area = (0.5*(ppp1.x + ppp1.x) - 1.0)*cover;
			(*this)[r][c].add(area, cover);
		}
	}
}
