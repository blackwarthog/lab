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
	marks[id] = (int4)(0, 0, c | (c + 1), 0);
}

kernel void path(
	int width,
	int height,
	global int *marks,
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
	if (flipx) { p0.x = (float)width - p0.x; p1.x = (float)width - p1.x; }
	if (flipy) { p0.y = (float)height - p0.y; p1.y = (float)height - p1.y; }
	float2 d = p1 - p0;
	float kx = d.x/d.y;
	float ky = d.y/d.x;
	int w1 = width - 1;
	int h1 = height - 1;
	
	global int *row;
	float2 px, py, pp1;
	float cover, area;
	int ix, iy, iix;
	
	while(p0.x != p1.x || p0.y != p1.y) {
		ix = (int)p0.x;
		iy = max((int)p0.y, 0);
		if (iy > h1) return;

		px.x = (float)(ix + 1);
		px.y = p0.y + ky*(px.x - p0.x);
		py.y = (float)(iy + 1);
		py.x = p0.x + kx*(py.y - p0.y);
		pp1 = p1;
		if (pp1.x > px.x) pp1 = px;
		if (pp1.y > py.y) pp1 = py;
		
		cover = (pp1.y - p0.y)*ONE_F;
		area = px.x - 0.5f*(p0.x + pp1.x);
		if (flipx) { ix = w1 - ix; area = 1.f - area; }
		if (flipy) { iy = h1 - iy; cover = -cover; }
		ix = clamp(ix, 0, w1);
		
		row = marks + 4*iy*width;
		atomic_add((global long*)(row + 4*ix), upsample((int)cover, (int)(area*cover)));
		//atomic_add(row + 4*ix, (int)(area*cover));
		//atomic_add(row + 4*ix + 1, (int)cover);
		
		row += 2;
		iix = (ix & (ix + 1)) - 1;
		while(iix >= minx) {
			atomic_min(row + 4*iix, ix);
			iix = (iix & (iix + 1)) - 1;
		}
		
		p0 = pp1;
	}
}

// TODO:
// different implementations for:
//   antialiased, transparent, inverted, evenodd contours and combinations (total 16 implementations)
kernel void fill(
	int width,
	global int4 *marks,
	global float4 *image,
	float4 color,
	int4 bounds )
{
	if (get_global_id(0) >= bounds.s3) return;
	int id = width*(int)get_global_id(0);
	marks += id;
	image += id;
	global int4 *mark;
	global float4 *pixel;

	//prefetch(row       + bounds.s0, bounds.s2 - bounds.s0);
	//prefetch(image_row + bounds.s0, bounds.s2 - bounds.s0);

	int4 m;
	float alpha;
	//int ialpha;
	int icover = 0, c0 = bounds.s0, c1 = bounds.s0;
	while(c1 < bounds.s2) {
		//ialpha = abs(icover);
		//ialpha = evenodd ? ONE - abs((ialpha % TWO) - ONE)
		//				 : min(ialpha, ONE);
		//if (invert) ialpha = ONE - ialpha;
		if (abs(icover) > HALF)
			while(c0 < c1)
				image[c0++] = color;

		mark = &marks[c1];
		m = *mark;
		*mark = (int4)(0, 0, c1 | (c1 + 1), 0); 
		
		//ialpha = abs(mark.x + icover);
		//ialpha = evenodd ? ONE - abs((ialpha % TWO) - ONE)
		//				 : min(ialpha, ONE);
		//if (invert) ialpha = ONE - ialpha;  

		alpha = (float)abs(m.x + icover)*DIV_ONE_F;
		pixel = &image[c1];
		*pixel = *pixel*(1.f - alpha) + color*alpha;
		
		icover += m.y;
		c0 = c1 + 1;
		c1 = m.z;
	}
}
