/*
    ......... 2018 Ivan Mahonin

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

/*
  samples_buffer format:
    Sample count,         // only field 'next_index' is in use and store index of next sample to allocate
    Sample rows[height],  // only fields 'next_index' are in use and store index first sample in the row 
    Sample real_samples[]
*/


typedef struct {
	float4 color;
	int invert;
	int evenodd;
	int align0;
	int align1;
} Path __attribute__((aligned (32)));

typedef struct {
	float2 coord;
	int path_index;
	int align0;
} Point __attribute__((aligned (16)));

typedef struct {
	int path_index;
	int x;
	float area;
	float cover;
	int next_index;
	int align0;
	int align1;
	int align2;
} Sample __attribute__((aligned (32))); 


kernel void reset(global Sample *samples)
{
	int id = get_global_id(0);
	samples[1+id].path_index = -1;
	samples[1+id].next_index = 0;
	if (id == 0) {
		samples->path_index = -1;
		samples->next_index = get_global_size(0) + 1;
	}
}


kernel void paths(
	int width,
	int height,
	global Sample *samples,
	global const Point *points )
{
	const float e = 1e-6f;
	
	// flip order, because we will insert samples into front of linked list 
	int id = get_global_size(0) - get_global_id(0) - 1;
	
	float2 size = (float2)((float)width, (float)height); 
	int w1 = width - 1;
	int h1 = height - 1;

	global int *next_sample = &samples->next_index;
	global Sample *rows = &samples[1];

	Point point0 = points[id];
	Point point1 = points[id+1];
	if (point0.path_index != point1.path_index) return;
	
	int path_index = point0.path_index;
	float2 p0 = point0.coord;
	float2 p1 = point1.coord;
	
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
			Sample sample;
			sample.path_index = path_index;
			sample.cover = pp1.y - p0.y;
			sample.area = px - 0.5f*(p0.x + pp1.x);
			if (flipx) { ix = w1 - ix; sample.area = 1.f - sample.area; }
			if (flipy) { iy = h1 - iy; sample.cover = -sample.cover; }
			sample.area *= sample.cover;
			sample.x = clamp(ix, 0, w1);
			
			// store in buffer
			int sample_index = atomic_inc(next_sample);
			sample.next_index = atomic_xchg(&rows[iy].next_index, sample_index);
			samples[sample_index] = sample;
		}
		
		p0 = pp1;
	}
}


kernel void draw(
	const int width,
	global float4 *image,
	global Sample *samples,
	global Path *paths )
{
	int id = get_global_id(0);

	global float4 *image_row = image + id*width;
	global Sample *first = &samples[1+id];

	int current_index;
	global Sample *prev, *current, *next;
	
	// sort
	bool repeat = true;
	while(repeat) {
		repeat        = false;
		prev          = first;
		current       = &samples[ prev->next_index ];
		while(current->next_index) {
			next = &samples[ current->next_index ];
			if ( current->path_index > next->path_index
			  || (current->path_index == next->path_index && current->x > next->x) )
			{
				// swap
				current_index = prev->next_index;
				prev->next_index = current->next_index;
				current->next_index = next->next_index;
				next->next_index = current_index;
				prev = next;
				repeat = true;
			} else {
				prev = current;
				current = next;
			}
		}
	}
	
	// merge
	current = &samples[ first->next_index ];
	float c = 0.f;
	while(current->next_index) {
		c += current->cover;
		next = &samples[ current->next_index ];
		if (current->path_index == next->path_index && current->x == next->x) {
			current->area  += next->area;
			current->cover += next->cover;
			current->next_index = next->next_index;
		} else {
			current = next;
		}
	}
	
	// draw
	global float4 *pixel, *next_pixel;
	float cover = 0.f;
	float alpha;
	int next_index = first->next_index;
	while(next_index) {
		current = &samples[ next_index ];
		next_index = current->next_index;
		
		// draw current
		float4 color = paths[ current->path_index ].color;
		float alpha = min(1.f, fabs(cover + current->area))*color.w;
		cover += current->cover;

		pixel = &image_row[current->x];
		*pixel = *pixel*(1.f - alpha) + color*alpha; // TODO: valid composite blending
		++pixel;
		
		// draw span: current <--> next
		next_pixel = fabs(cover) > 0.5f && current->path_index == samples[next_index].path_index
				   ? &image_row[samples[next_index].x] : pixel;
		while(pixel < next_pixel) {
			*pixel = *pixel*(1.f - color.w) + color*color.w; // TODO: valid composite blending
			++pixel;
		}

		if (current->path_index != samples[next_index].path_index) cover = 0.f;
	}
}

