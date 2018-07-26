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

extern "C" {

#define ONE       65536
#define TWO      131072               // (ONE)*2
#define HALF      32768               // (ONE)/2
#define ONE_F     65536.f             // (float)(ONE)
#define DIV_ONE_F 0.0000152587890625f // 1.f/(ONE_F)


__global__ void clear(
	int width,
	int height,
	int4 *marks )
{
	int id = blockIdx.x*blockDim.x + threadIdx.x;
	int c = id % width;
	marks[id] = make_int4(0, 0, c | (c + 1), 0);
}

__global__ void path(
	int width,
	int height,
	int *marks,
	const float2 *points,
	int begin,
	int end,
	int minx )
{
	int id = blockIdx.x*blockDim.x + threadIdx.x + begin;
	if (id >= end) return;
	float2 p0 = points[id];
	float2 p1 = points[id + 1];
	
	bool flipx = p1.x < p0.x;
	bool flipy = p1.y < p0.y;
	if (flipx) { p0.x = (float)width - p0.x; p1.x = (float)width - p1.x; }
	if (flipy) { p0.y = (float)height - p0.y; p1.y = (float)height - p1.y; }
	
	float2 d;
	d.x = p1.x - p0.x;
	d.y = p1.y - p0.y;
	float kx = d.x/d.y;
	float ky = d.y/d.x;
	int w1 = width - 1;
	int h1 = height - 1;
	
	while(p0.x != p1.x || p0.y != p1.y) {
		int ix = (int)p0.x;
		int iy = max((int)p0.y, 0);
		if (iy > h1) return;

		float2 px, py;
		px.x = (float)(ix + 1);
		py.y = (float)(iy + 1);
		ix = max(0, min(w1, ix));
		
		px.y = p0.y + ky*(px.x - p0.x);
		py.x = p0.x + kx*(py.y - p0.y);

		float2 pp1 = p1;
		if (pp1.x > px.x) pp1 = px;
		if (pp1.y > py.y) pp1 = py;
		
		float cover = (pp1.y - p0.y)*ONE_F;
		float area = px.x - 0.5f*(p0.x + pp1.x);
		if (flipx) { ix = w1 - ix; area = 1.f - area; }
		if (flipy) { iy = h1 - iy; cover = -cover; }
		
		int *row = marks + 4*iy*width;
		atomicAdd(
			(unsigned long long*)(row + 4*ix),
			((unsigned long long)(unsigned int)(int)(cover) << 32)
			| (unsigned long long)(unsigned int)((int)(area*cover)) );
		//row[4*ix] += (int)(area*cover);
		//row[4*ix + 1] += (int)(cover);
		//atomicAdd(row + 4*ix, (int)(area*cover));
		//atomicAdd(row + 4*ix + 1, (int)(cover));
		
		row += 2;
		int iix = (ix & (ix + 1)) - 1;
		while(iix >= minx) {
			atomicMin(row + 4*iix, ix);
			iix = (iix & (iix + 1)) - 1;
		}
		
		p0 = pp1;
	}
}

__global__ void fill(
	int width,
	int4 *marks,
	float4 *image,
	float4 color,
	int4 bounds )
{
	int id = blockIdx.x*blockDim.x + threadIdx.x + bounds.y;
	if (id >= bounds.w) return;
	id *= width;
	marks += id;
	image += id;
	
	int4 *mark;
	float4 *pixel;

	int4 m;
	int icover = 0, c0 = bounds.x, c1 = bounds.x;
	while(c1 < bounds.z) {
		if (abs(icover) > HALF)
			while(c0 < c1)
				image[c0++] = color;

		mark = &marks[c1];
		m = *mark;
		*mark = make_int4(0, 0, c1 | (c1 + 1), 0); 
		
		float alpha = (float)abs(m.x + icover)*DIV_ONE_F;
		float one_alpha = 1.f - alpha;
		
		pixel = &image[c1];
		float4 p = *pixel;
		p.x = p.x*one_alpha + color.x*alpha;
		p.y = p.y*one_alpha + color.y*alpha;
		p.z = p.z*one_alpha + color.z*alpha;
		p.w = p.w*one_alpha + color.w*alpha;
		*pixel = p;
		
		icover += m.y;
		c0 = c1 + 1;
		c1 = m.z;
	}
}

}