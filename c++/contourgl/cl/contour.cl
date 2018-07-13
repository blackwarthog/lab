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

// paths format:
//   {
//     int count,
//     paths: [
//       {
//         int point_count,
//         int flags,
//         float4 color,
//         points: [ float2, ... ]
//       },
//       ...
//     ]
//   }


kernel void draw(
	const int width,
	const int height,
	global int *mark_buffer,
	global float4 *image,
	global const char *paths_buffer )
{
	const float e = 1e-6f;

	int id = (int)get_local_id(0);
	int count = (int)get_local_size(0);
	
	int paths_count = *(global int *)paths_buffer;
	global const char *paths = paths_buffer + sizeof(int);
	
	int pixels_count = width*height;
	float2 size = (float2)((float)width, (float)height); 
	int w1 = width - 1;
	int h1 = height - 1;
	
	local int bound_minx;
	local int bound_miny;
	local int bound_maxx;
	local int bound_maxy;
	
	// draw paths
	for(int p = 0; p < paths_count; ++p) {
		int points_count = *(global const int *)paths; paths += sizeof(int);
		int flags        = *(global const int *)paths; paths += sizeof(int);
		
		float4 color;
		color.x = *(global const float *)paths; paths += sizeof(float);
		color.y = *(global const float *)paths; paths += sizeof(float);
		color.z = *(global const float *)paths; paths += sizeof(float);
		color.w = *(global const float *)paths; paths += sizeof(float);
		
		global const float *points = (global const float *)paths;
		paths += 2*points_count*sizeof(float);
		
		int segments_count = points_count - 1;
		if (segments_count <= 0) continue;
		
		bool invert  = flags & 1;
		bool evenodd = flags & 2;
		
		if (id == 0) {
			bound_minx = invert ?  0 : (int)floor(points[0] + e);
			bound_miny = invert ?  0 : (int)floor(points[1] + e);
			bound_maxx = invert ? w1 : bound_minx;
			bound_maxy = invert ? h1 : bound_miny;
		}
		barrier(CLK_GLOBAL_MEM_FENCE | CLK_LOCAL_MEM_FENCE);

		// trace path
		for(int i = id; i < segments_count; i += count) {
			int ii = 2*i;
			float2 p0 = { points[ii + 0], points[ii + 1] };
			float2 p1 = { points[ii + 2], points[ii + 3] };
			
			int p1x = (int)floor(p1.x + e);
			int p1y = (int)floor(p1.y + e);
			atomic_min(&bound_minx, p1x);
			atomic_min(&bound_miny, p1y);
			atomic_max(&bound_maxx, p1x);
			atomic_max(&bound_maxy, p1y);
				
			bool flipx = p1.x < p0.x;
			bool flipy = p1.y < p0.y;
			if (flipx) { p0.x = size.x - p0.x; p1.x = size.x - p1.x; }
			if (flipy) { p0.y = size.y - p0.y; p1.y = size.y - p1.y; }
			float2 d = p1 - p0;
			float kx = fabs(d.y) < e ? 1e10 : d.x/d.y;
			float ky = fabs(d.x) < e ? 1e10 : d.y/d.x;
			
			while(p0.x != p1.x || p0.y != p1.y) {
				int ix = (int)floor(p0.x + e);
				int iy = (int)floor(p0.y + e);
				if (iy > h1) break;

				float px = (float)(ix + 1);
				float py = (float)(iy + 1);
				float2 pp1 = p1;
				if (pp1.x > px) { pp1.x = px; pp1.y = p0.y + ky*(px - p0.x); }
				if (pp1.y > py) { pp1.y = py; pp1.x = p0.x + kx*(py - p0.y); }
				
				if (iy >= 0) {
					// calc values
					float cover = pp1.y - p0.y;
					float area = px - 0.5f*(p0.x + pp1.x);
					if (flipx) { ix = w1 - ix; area = 1.f - area; }
					if (flipy) { iy = h1 - iy; cover = -cover; }
					ix = clamp(ix, 0, w1);
					
					// store in buffer
					global int *mark = mark_buffer + (iy*width + ix)*2;
					atomic_add(mark, (int)round(area*cover*65536.f));
					atomic_add(mark + 1, (int)round(cover*65536.f));
				}
				
				p0 = pp1;
			}
		}
		barrier(CLK_GLOBAL_MEM_FENCE | CLK_LOCAL_MEM_FENCE);

		// read bounds
		int minx = max(bound_minx, 0);
		int miny = max(bound_miny, 0);
		int maxx = min(bound_maxx, w1);
		int maxy = min(bound_maxy, h1);
		barrier(CLK_GLOBAL_MEM_FENCE | CLK_LOCAL_MEM_FENCE);

		// fill
		for(int row = miny + id; row <= maxy; row += count) {
			global int *mark = mark_buffer + (row*width + minx)*2;
			global float4 *pixel = image + row*width + minx;
			global float4 *pixel_end = pixel - minx + maxx + 1;
			int icover = 0;
			
			while(pixel < pixel_end) {
				// read mark (alpha, cover)
				int ialpha = abs(icover + *mark); *mark = 0; ++mark;
				icover += *mark;                  *mark = 0; ++mark;
				
				if (evenodd) ialpha = 65536 - abs(ialpha%131072 - 65536);
				if (invert)  ialpha = 65536 - ialpha;
				
				//if (!ialpha) continue;
				
				// write color
				float alpha = (float)ialpha/65536.f*color.w;
				*pixel = *pixel*(1.f - alpha) + color*alpha;
				++pixel;
			}
		}
	}
}

