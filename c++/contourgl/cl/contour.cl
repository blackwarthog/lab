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
	int width, 
	__global float *lines,
	__global int *rows,
	__global float *mark_buffer )
{
	const float e = 1e-6f;
	size_t id = get_global_id(0);
	int begin = rows[id*2];
	int end = begin + rows[id*2 + 1];
	for(int i = begin; i < end; ++i) {
		float2 p0 = { lines[4*i + 0], lines[4*i + 1] };
		float2 p1 = { lines[4*i + 2], lines[4*i + 3] };
		
		int iy0 = (int)floor(fmin(p0.y, p1.y) + e);
		int iy1 = (int)floor(fmax(p0.y, p1.y) - e);
		
		float2 d = p1 - p0;
		float kx = fabs(d.y) < e ? 0.f : d.x/d.y;
		float ky = fabs(d.x) < e ? 0.f : d.y/d.x;
		
		for(int r = iy0; r <= iy1; ++r) {
			float y = (float)r;
			float2 pya = { p0.x + kx*(y       - p0.y), y };
			float2 pyb = { p0.x + kx*(y + 1.0 - p0.y), y + 1.f };
			float2 pp0 = p0.y - y < -e      ? pya
			           : (p0.y - y > 1.f + e ? pyb : p0);
			float2 pp1 = p1.y - y < -e      ? pya
			           : (p1.y - y > 1.f + e ? pyb : p1);

			int ix0 = (int)floor(fmin(pp0.x, pp1.x) + e);
			int ix1 = (int)floor(fmax(pp0.x, pp1.x) - e);
			for(int c = ix0; c <= ix1; ++c) {
				float x = (float)c;
				float2 pxa = { x,       p0.y + ky*(x       - p0.x) };
				float2 pxb = { x + 1.0, p0.y + ky*(x + 1.0 - p0.x) };
				float2 ppp0 = pp0.x - x < -e      ? pxa
				            : (pp0.x - x > 1.f + e ? pxb : pp0);
				float2 ppp1 = pp1.x - x < -e      ? pxa
				            : (pp1.x - x > 1.f + e ? pxb : pp1);

				float cover = ppp1.y - ppp0.y;
				float area = (x + 1.f - 0.5f*(ppp0.x + ppp1.x))*cover;
				__global float *mark = mark_buffer + 2*(r*width + c);
				mark[0] += area;
				mark[1] += cover;
			}
		}
	}
}

__kernel void fill(
	int width,
	__global float *mark_buffer,
	__global float *surface_buffer,
	float color_r,
	float color_g,
	float color_b,
	float color_a,
	int invert,
	int evenodd )
{
	size_t id = get_global_id(0);
	int w = width;
	float cr = color_r;
	float cg = color_g;
	float cb = color_b;
	float ca = color_a;
	__global float *mark = mark_buffer + 2*id*w;
	__global float *surface = surface_buffer + 4*id*w;
	float cover = 0.f;
	for(int i = 0; i < width; ++i, mark += 2, surface += 4) {
		float alpha = fabs(*mark + cover);
		alpha = evenodd ? ca*(1.f - fabs(1.f - alpha - 2.f*floor(0.5f*alpha)))
				        : fmin(alpha, 1.f);
		if (invert) alpha = 1.f - alpha;
		float alpha_inv = 1.f - alpha;
		surface[0] = surface[0]*alpha_inv + cr*alpha;
		surface[1] = surface[1]*alpha_inv + cg*alpha;
		surface[2] = surface[2]*alpha_inv + cb*alpha;
		surface[3] = fmin(surface[3] + ca*alpha, 1.f);
		cover += mark[1];
	}
}
