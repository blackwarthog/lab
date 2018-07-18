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

kernel void clear(
	int width,
	int height,
	global int4 *mark_buffer )
{
	int id = get_global_id(0);
	if (id >= width*height) return;
	int c = id % width;
	int4 v = { 0, 0, c | (c + 1), 0 };
	mark_buffer[id] = v;
}

kernel void path(
	int width,
	int height,
	global int *mark_buffer,
	global float2 *points,
	int begin,
	int end )
{
	const float e = 1e-6f;
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
	float kx = fabs(d.y) < e ? 1e10 : d.x/d.y;
	float ky = fabs(d.x) < e ? 1e10 : d.y/d.x;
	
	while(p0.x != p1.x || p0.y != p1.y) {
		int ix = (int)floor(p0.x + e);
		int iy = (int)floor(p0.y + e);
		if (iy > h1) break;

		float2 px, py;
		px.x = (float)(ix + 1);
		px.y = p0.y + ky*(px.x - p0.x);
		py.y = max((float)(iy + 1), 0.f);
		py.x = p0.x + kx*(py.y - p0.y);
		float2 pp1 = p1;
		if (pp1.x > px.x) pp1 = px;
		if (pp1.y > py.y) pp1 = py;
		
		if (iy >= 0) {
			float cover = pp1.y - p0.y;
			float area = px.x - 0.5f*(p0.x + pp1.x);
			if (flipx) { ix = w1 - ix; area = 1.f - area; }
			if (flipy) { iy = h1 - iy; cover = -cover; }
			ix = clamp(ix, 0, w1);
			global int *row = mark_buffer + 4*iy*width;
			global int *mark = row + 4*ix;
			atomic_add(mark, (int)round(area*cover*65536.f));
			atomic_add(mark + 1, (int)round(cover*65536.f));
			int iix = (ix & (ix + 1)) - 1;
			while(iix > 0) {
				atomic_min(row + 4*iix + 2, ix);
				iix = (iix & (iix + 1)) - 1;
			}
		}
		
		p0 = pp1;
	}
}

kernel void fill(
	int width,
	int height,
	global int4 *mark_buffer,
	global float4 *image,
	float4 color,
	int invert,
	int evenodd )
{
	const int scale = 65536;
	const int scale2 = 2*scale;
	const int scale05 = scale/2;

	int id = get_global_id(0);
	if (id >= height) return;
	int w1 = width - 1;
	global int4 *row = mark_buffer + id*width;
	global float4 *image_row = image + id*width;

	int cover = 0;
	int ialpha;
	int2 c0 = { 0, id };
	int2 c1 = c0;
	int4 empty_mark = { 0, 0, 0, 0 };
	while(c0.x < w1) {
		int4 mark;
		while(c1.x < width) {
			mark = row[c1.x];
			empty_mark.z = c1.x | (c1.x + 1);
			row[c1.x] = empty_mark; 
			if (mark.x || mark.y) break;
			c1.x = min(mark.z, width);
		}
		
		ialpha = abs(cover);
		ialpha = evenodd ? scale - abs((ialpha % scale2) - scale)
						 : min(ialpha, scale);
		if (invert) ialpha = scale - ialpha;  
		if (ialpha > scale05) {
			while(c0.x < c1.x) {
				image_row[c0.x] = color;
				++c0.x;
			}
		}
	
		if (c1.x >= width) return;
		
		ialpha = abs(mark.x + cover);
		ialpha = evenodd ? scale - abs((ialpha % scale2) - scale)
						 : min(ialpha, scale);
		if (invert) ialpha = scale - ialpha;  
		if (ialpha > 4) {
			float alpha = (float)ialpha/(float)scale;
			float alpha_inv = 1.f - alpha;
			global float4 *pixel = &image_row[c1.x];
			float4 cl = *pixel;
			cl.x = cl.x*alpha_inv + color.x*alpha;
			cl.y = cl.y*alpha_inv + color.y*alpha;
			cl.z = cl.z*alpha_inv + color.z*alpha;
			cl.w = min(cl.w + alpha, 1.f);
			*pixel = cl;
		}
		
		c0.x = c1.x + 1;
		c1.x = min(mark.z, width);
		cover += mark.y;
	}
}
