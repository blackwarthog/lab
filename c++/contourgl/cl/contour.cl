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
	global int2 *mark_buffer )
{
	const int2 v = { 0, 0 };
	mark_buffer[get_global_id(0)] = v;
}

kernel void path(
	int width,
	int height,
	global int *mark_buffer,
	global float2 *path )
{
	const float e = 1e-6f;
	size_t id = get_global_id(0);
	
	float2 s = { (float)width, (float)height }; 
	int w1 = width - 1;
	int h1 = height - 1;

	float2 p0 = path[id];
	float2 p1 = path[id + 1];
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
			global int *mark = mark_buffer + 2*(iy*width + clamp(ix, 0, w1));
			atomic_add(mark, (int)round(area*cover*65536));
			atomic_add(mark + 1, (int)round(cover*65536));
		}
		
		p0 = pp1;
	}
}

kernel void fill(
	int width,
	global int2 *mark_buffer,
	read_only image2d_t surface_read_image,
	write_only image2d_t surface_write_image,
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
	global int2 *mark = mark_buffer + id*w;
	const int2 izero = { 0, 0 };
	for(int2 coord = { 0, id }; coord.x < w; ++coord.x, ++mark) {
		int2 im = *mark;
		//*mark = izero;
		float alpha = fabs((float)im.x/65536.f + cover);
		cover += (float)im.y/65536.f;
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
