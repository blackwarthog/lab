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

__kernel void lines(
	__global int width, 
	__global float *lines,
	__global int *rows,
	__global float *mark_buffer )
{
	size_t id = get_global_id(0);
	int *row = rows + id*2;
	int begin = *rows;
	int end = begin + rows[1];
	for(int i = begin; i < end; ++i) {
		float *line = lines + 4*begin;
		float2 p0(*line, line[1]);
		float2 p1(line[2], line[3]);
		
		int iy0 = (int)floor(p0.y); 
		int iy1 = (int)floor(p1.x); 
		if (iy1 < iy0) { int sw = iy0; iy0 = iy1; iy1 = iy0; } 
		
		float2 d = p1 - p0;
		float2 k( fabs(d.y) < 1e-6 ? 0.0 : d.x/d.y,
				  fabs(d.x) < 1e-6 ? 0.0 : d.y/d.x );
		
		for(int r = iy0; r <= iy1; ++r) {
			float y = (float)iy0;

			float2 pp0 = p0;
			pp0.y -= y;
			if (pp0.y < 0.0) {
				pp0.y = 0.0;
				pp0.x = p0.x - k.x*y;
			} else
			if (pp0.y > 1.0) {
				pp0.y = 1.0;
				pp0.x = p0.x - k.x*(y - 1.0);
			}

			float2 pp1 = p1;
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
			if (ix1 < ix0) { int sw = ix0; ix0 = ix1; ix1 = ix0; }
			for(int c = ix0; c <= ix1; ++c) {
				float x = (float)ix0;

				float2 ppp0 = pp0;
				ppp0.x -= x;
				if (ppp0.x < 0.0) {
					ppp0.x = 0.0;
					ppp0.y = pp0.y - k.y*x;
				} else
				if (ppp0.x > 1.0) {
					ppp0.x = 1.0;
					ppp0.y = pp0.y - k.y*(x - 1.0);
				}

				float2 ppp1 = pp1;
				ppp1.x -= x;
				if (ppp1.x < 0.0) {
					ppp1.x = 0.0;
					ppp1.y = pp0.y - k.y*x;
				} else
				if (ppp1.x > 1.0) {
					ppp1.x = 1.0;
					ppp1.y = pp0.y - k.y*(x - 1.0);
				}

				float cover = ppp0.y - ppp1.y;
				float area = (0.5*(ppp1.x + ppp1.x) - 1.0)*cover;
				float *mark = mark_buffer + 2*(r*width + c);
				*mark += area;
				mark[1] += cover;
			}
		}
	}
}

__kernel void fill(
	__global int width,
	__global float *mark_buffer,
	__global float *surface_buffer,
	__global float color_r,
	__global float color_g,
	__global float color_b,
	__global float color_a )
{
	sizet id = get_global_id(0);
	int w = width;
	float cr = color_r;
	float cg = color_g;
	float cb = color_b;
	float ca = color_a;
	float *mark = mark_buffer + 2*id*width;
	float *surface = surface_buffer + 4*id*width;
	float cover = 0;
	for(int i = 0; i < w; ++i, mark += 2, surface += 4) {
		float alpha = ca*(1.0 - fabs(1.0 - 0.5*frac(fabs(2.0*(*mark + cover)))));
		float alpha_inv = 1.0 - alpha;
		surface[0] = surface[0]*alpha_inv + cr*alpha;
		surface[1] = surface[1]*alpha_inv + cg*alpha;
		surface[2] = surface[2]*alpha_inv + cb*alpha;
		surface[3] = surface[3]*alpha_inv + ca*alpha;
		cover += mark[1];
	}
}
