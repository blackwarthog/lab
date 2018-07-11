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
	global char *paths_buffer,
	global int *mark_buffer,
	read_only image2d_t read_image,
	write_only image2d_t write_image ) // assumed that read and write image is the same object
{
	const float e = 1e-6f;

	int id = (int)get_global_id(0);
	int count = (int)get_global_size(0);
	
	int paths_count = *(global int *)paths_buffer;
	global char *paths = paths_buffer + sizeof(int);
	
	int width = get_image_width(write_image);
	int height = get_image_height(write_image);
	int pixels_count = width*height;
	float2 size = (float2)((float)width, (float)height); 
	int w1 = width - 1;
	int h1 = height - 1;
	
	global int *bound_minx = (global int *)(mark_buffer + 2*pixels_count);
	global int *bound_miny = bound_minx + 1;
	global int *bound_maxx = bound_minx + 2;
	global int *bound_maxy = bound_minx + 3;
	
	// clear marks
	for(int i = id; i < 2*pixels_count; i += count)
		mark_buffer[i] = 0;
	barrier(CLK_LOCAL_MEM_FENCE);
	
	// draw paths
	for(int p = 0; p < paths_count; ++p) {
		int points_count = *(global int *)paths; paths += sizeof(int);
		int flags        = *(global int *)paths; paths += sizeof(int);
		
		float4 color;
		color.x = *(global float *)paths; paths += sizeof(float);
		color.y = *(global float *)paths; paths += sizeof(float);
		color.z = *(global float *)paths; paths += sizeof(float);
		color.w = *(global float *)paths; paths += sizeof(float);
		
		global float *points = (global float *)paths;
		paths += 2*points_count*sizeof(float);
		
		int segments_count = points_count - 1;
		if (segments_count <= 0) continue;
		
		int invert  = flags & 1;
		int evenodd = flags & 2;
		
		*bound_minx = invert ?  0 : (int)floor(clamp(points[0] + e, 0.f, size.x - 1.f + e));
		*bound_miny = invert ?  0 : (int)floor(clamp(points[1] + e, 0.f, size.y - 1.f + e));
		*bound_maxx = invert ? w1 : *bound_minx;
		*bound_maxy = invert ? h1 : *bound_miny;

		// trace path
		for(int i = id; i < segments_count; i += count) {
			int ii = 2*i;
			float2 p0 = { points[ii + 0], points[ii + 1] };
			float2 p1 = { points[ii + 2], points[ii + 3] };
			
			int p1x = (int)floor(clamp(p1.x + e, 0.f, size.x - 1.f + e));
			int p1y = (int)floor(clamp(p1.y + e, 0.f, size.y - 1.f + e));
			atomic_min(bound_minx, p1x - 1);
			atomic_min(bound_miny, p1y - 1);
			atomic_max(bound_maxx, p1x + 1);
			atomic_max(bound_maxy, p1y + 1);
				
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
					atomic_add(mark, (int)round(area*cover*65536.f));
					atomic_add(mark + 1, (int)round(cover*65536.f));
				}
				
				p0 = pp1;
			}
		}
		barrier(CLK_LOCAL_MEM_FENCE);
		
		// fill
		int2 coord;
		int minx = max(*bound_minx, 0);
		int miny = max(*bound_miny, 0);
		int maxx = min(*bound_maxx, w1);
		int maxy = min(*bound_maxy, h1);
		for(coord.y = miny + id; coord.y <= maxy; coord.y += count) {
			global int *mark = mark_buffer + (coord.y*width + minx)*2;
	
			float cover = 0.f;
			for(coord.x = minx; coord.x <= maxx; ++coord.x) {
				// read mark (alpha, cover)
				float alpha = fabs(cover + *mark/65536.f); *mark = 0; ++mark;
				cover += *mark/65536.f;                    *mark = 0; ++mark;
				
				//if (evenodd) alpha = 1.f - fabs(fmod(alpha, 2.f) - 1.f);
				//if (invert) alpha = 1.f - alpha;
				alpha *= color.w;
				
				// write color
				float alpha_inv = 1.f - alpha;
				float4 cl = read_imagef(read_image, coord);
				cl.x = cl.x*alpha_inv + color.x*alpha;
				cl.y = cl.y*alpha_inv + color.y*alpha;
				cl.z = cl.z*alpha_inv + color.z*alpha;
				cl.w = min(cl.w + alpha, 1.f);
				write_imagef(write_image, coord, cl);
			}
		}
		barrier(CLK_LOCAL_MEM_FENCE);
	}
}

