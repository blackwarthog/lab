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

__kernel void clear2f(
	__global float2 *buffer )
{
	const float2 v = { 0.f, 0.f };
	buffer[get_global_id(0)] = v;
}

__kernel void lines(
	int width, 
	__global float4 *lines,
	__global int2 *rows,
	__global float2 *mark_buffer )
{
	const float e = 1e-6f;
	int2 row = rows[get_global_id(0)];
	sampler_t sampler = CLK_NORMALIZED_COORDS_FALSE
					  | CLK_ADDRESS_NONE
			          | CLK_FILTER_NEAREST;
	int w = width;
	for(__global float4 *i = lines + row.x, *end = i + row.y; i < end; ++i) {
		float4 line = *i; 
		float2 p0 = { line.x, line.y };
		float2 p1 = { line.z, line.w };
		
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

				float2 m;
				m.y = ppp1.y - ppp0.y;
				m.x = (x + 1.f - 0.5f*(ppp0.x + ppp1.x))*m.y;
				mark_buffer[r*w + c] += m;  
			}
		}
	}
}

__kernel void fill(
	int width,
	__global float2 *mark_buffer,
	__read_only image2d_t surface_read_image,
	__write_only image2d_t surface_write_image,
	float4 color,
	int invert,
	int evenodd )
{
	size_t id = get_global_id(0);
	int w = width;
	const sampler_t sampler = CLK_NORMALIZED_COORDS_FALSE
							| CLK_ADDRESS_NONE
			                | CLK_FILTER_NEAREST;
	float4 cl = color;
	float cover = 0.f;
	__global float2 *mark = mark_buffer + id*w;
	for(int2 coord = { 0, id }; coord.x < w; ++coord.x, ++mark) {
		float2 m = *mark;

		float alpha = fabs(m.x + cover);
		cover += m.y;
		alpha = evenodd ? (1.f - fabs(1.f - alpha - 2.f*floor(0.5f*alpha)))
				        : fmin(alpha, 1.f);
		alpha *= cl.w;
		if (invert) alpha = 1.f - alpha;
		float alpha_inv = 1.f - alpha;
		
		float4 c = read_imagef(surface_read_image, sampler, coord);
		c.x = c.x*alpha_inv + cl.x*alpha;
		c.y = c.y*alpha_inv + cl.y*alpha;
		c.z = c.z*alpha_inv + cl.z*alpha;
		c.w = min(c.w + alpha, 1.f);
		write_imagef(surface_write_image, coord, c);
	}
}
