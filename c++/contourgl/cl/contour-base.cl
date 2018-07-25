/*
    ......... 2015-2018 Ivan Mahonin

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

#pragma OPENCL EXTENSION cl_khr_int64_base_atomics: enable

#define ONE       65536
#define TWO      131072               // (ONE)*2
#define HALF      32768               // (ONE)/2
#define ONE_F     65536.f             // (float)(ONE)
#define DIV_ONE_F 0.0000152587890625f // 1.f/(ONE_F)


kernel void clear(
	int width,
	int height,
	global int4 *marks )
{
	int id = get_global_id(0);
	int c = id % width;
	marks[id] = (int4)(0, 0, 0, 0);
}

kernel void path(
	int width,
	int height,
	global long *marks,
	global float2 *points,
	int end,
	int minx )
{
	int id = get_global_id(0);
	if (id >= end) return;
	float2 p0 = points[id];
	float2 p1 = points[id + 1];
	
	bool flipx = p1.x < p0.x;
	bool flipy = p1.y < p0.y;
	if (flipx) { p0.x = (float)width  - p0.x; p1.x = (float)width  - p1.x; }
	if (flipy) { p0.y = (float)height - p0.y; p1.y = (float)height - p1.y; }
	float2 d = p1 - p0;
	int w1 = width - 1;
	int h1 = height - 1;
	float kx = d.x/d.y;
	float ky = d.y/d.x;
	
	while(p0.x != p1.x || p0.y != p1.y) {
		int iy = max((int)p0.y, 0);
		int ix = (int)p0.x;
		if (iy > h1) return;

		float2 px, py;
		px.x = (float)(ix + 1);
		py.y = (float)(iy + 1);
		ix = clamp(ix, 0, w1);
		
		px.y = p0.y + ky*(px.x - p0.x);
		py.x = p0.x + kx*(py.y - p0.y);
		
		float2 pp1 = p1;
		if (pp1.x > px.x) pp1 = px;
		if (pp1.y > py.y) pp1 = py;
		
		float cover = (pp1.y - p0.y)*ONE_F;
		float area = px.x - 0.5f*(p0.x + pp1.x);
		if (flipy) { iy = h1 - iy; cover = -cover; }
		if (flipx) { ix = w1 - ix; area = 1.f - area; }
		p0 = pp1;
		
		atomic_add(marks + ix*height + iy, upsample((int)cover, (int)(area*cover)));
	}
}

// TODO:
// different implementations for:
//   antialiased, transparent, inverted, evenodd contours and combinations (total 16 implementations)
kernel void fill(
	int height,
	global int2 *marks,
	global float4 *image,
	float4 color,
	int4 bounds )
{
	if (get_global_id(0) >= bounds.s3) return;
	int id = (int)get_global_id(0) + bounds.s0*height;
	marks += id;
	image += id;

	int icover = 0;
	while(true) {
		int2 m = *marks;
		*marks = (int2)(0, 0);
		float alpha = (float)abs(m.x + icover)*color.w*DIV_ONE_F;
		marks += height;

		icover += m.y;
		*image = *image*(1.f - alpha) + color*alpha;
		
		if (++bounds.s0 >= bounds.s2) return;
		image += height;
	}
}
