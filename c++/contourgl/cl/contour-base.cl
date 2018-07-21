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

#pragma OPENCL EXTENSION cl_khr_int64_base_atomics: enable

#define ONE       65536
#define TWO      131072               // (ONE)*2
#define HALF      32768               // (ONE)/2
#define ONE_F     65536.f             // (float)(ONE)
#define DIV_ONE_F 0.0000152587890625f // 1.f/(ONE_F)


kernel void clear(
	int width,
	int height,
	global int4 *mark_buffer )
{
	int id = get_global_id(0);
	if (id >= width*height) return;
	int c = id % width;
	mark_buffer[id] = (int4)(0, 0, c | (c + 1), 0);
}

kernel void path(
	int width,
	int height,
	global int *mark_buffer,
	global float2 *points,
	int begin,
	int end,
	int4 bounds )
{
	int id = get_global_id(0);
	if (id >= end) return;
	
	float2 s = { (float)width, (float)height }; 
	int w1 = width - 1;
	int h1 = height - 1;

	float2 p0 = points[id];
	float2 p1 = points[id + 1];
	bool flipx = p1.x < p0.x;
	bool flipy = p1.y < p0.y;
	if (flipx) { p0.x = s.x - p0.x; p1.x = s.x - p1.x; }
	if (flipy) { p0.y = s.y - p0.y; p1.y = s.y - p1.y; }
	float2 d = p1 - p0;
	float kx = d.x/d.y;
	float ky = d.y/d.x;
	
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
		
		row = mark_buffer + 4*iy*width;
		atomic_add((global long*)(row + 4*ix), upsample((int)cover, (int)(area*cover)));
		
		row += 2;
		iix = (ix & (ix + 1)) - 1;
		while(iix >= bounds.s0) {
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
	int height,
	global int4 *mark_buffer,
	global float4 *image,
	float4 color,
	int4 bounds,
	int invert,
	int evenodd )
{
	int id = get_global_id(0);
	if (id >= height) return;
	global int4 *row = mark_buffer + id*width;
	global int4 *mark;
	global float4 *image_row = image + id*width;
	global float4 *pixel;

	int4 m;
	float alpha;
	//int ialpha;
	int icover = 0, c0 = bounds.s0, c1 = bounds.s0;
	while(c0 < bounds.s2) {
		c1 = min(c1, bounds.s2);
		mark = &row[c1];
		m = *mark;
		*mark = (int4)(0, 0, c1 | (c1 + 1), 0); 
		
		//ialpha = abs(icover);
		//ialpha = evenodd ? ONE - abs((ialpha % TWO) - ONE)
		//				 : min(ialpha, ONE);
		//if (invert) ialpha = ONE - ialpha;
		if (abs(icover) > HALF)
			while(c0 < c1)
				image_row[c0++] = color;
	
		if (c1 >= bounds.s2) return;
		
		//ialpha = abs(mark.x + icover);
		//ialpha = evenodd ? ONE - abs((ialpha % TWO) - ONE)
		//				 : min(ialpha, ONE);
		//if (invert) ialpha = ONE - ialpha;  

		alpha = (float)abs(m.x + icover)*DIV_ONE_F;
		pixel = &image_row[c1];
		*pixel = (float4)( pixel->xyz*(1.f - alpha) + color.xyz*alpha,
				           min(pixel->w + alpha, 1.f) );
		
		c0 = c1 + 1;
		c1 = m.z;
		icover += m.y;
	}
}
