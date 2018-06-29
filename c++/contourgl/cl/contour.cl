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
	mark_buffer[ get_global_id(0) ] = v;
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
			// calc values
			float cover = pp1.y - p0.y;
			float area = px.x - 0.5f*(p0.x + pp1.x);
			if (flipx) { ix = w1 - ix; area = 1.f - area; }
			if (flipy) { iy = h1 - iy; cover = -cover; }
			ix = clamp(ix, 0, w1);
			
			// store in buffer
			global int *mark = mark_buffer + (iy*width + ix)*2;
			atomic_add(mark,     (int)round(area*cover*65536.f));
			atomic_add(mark + 1, (int)round(cover*65536.f));
		}
		
		p0 = pp1;
	}
}

kernel void fill(
	int width,
	global int2 *mark_buffer,
	read_only image2d_t surface_read_image,
	write_only image2d_t surface_write_image,
	int minx,
	int maxx,
	float4 color,
	int invert,
	int evenodd )
{
	size_t id = get_global_id(0);
	global int2 *row = mark_buffer + id*width;
	const int2 empty_mark = { 0, 0 };

	float cover = 0.f;
	for(int2 c = {minx, id}; c.x < maxx; ++c.x) {
		// read mark (x: alpha, y: cover)
		global int2 *mark = row + c.x;
		float alpha = fabs(cover + mark->x/65536.f);
		//if (evenodd) alpha = 1.f - fabs(fmod(alpha, 2.f) - 1.f);
		cover += mark->y/65536.f;
		*mark = empty_mark;
		
		//if (invert) alpha = 1.f - alpha;
		alpha *= color.w;
		
		// write color
		float alpha_inv = 1.f - alpha;
		float4 cl = read_imagef(surface_read_image, c);
		cl.x = cl.x*alpha_inv + color.x*alpha;
		cl.y = cl.y*alpha_inv + color.y*alpha;
		cl.z = cl.z*alpha_inv + color.z*alpha;
		cl.w = min(cl.w + alpha, 1.f);
		write_imagef(surface_write_image, c, cl);
	}
}
