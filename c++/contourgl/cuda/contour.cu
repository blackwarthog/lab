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

//extern "C" {

#define ONE       65536
#define TWO      131072               // (ONE)*2
#define HALF      32768               // (ONE)/2
#define ONE_F     65536.f             // (float)(ONE)
#define DIV_ONE_F 0.0000152587890625f // 1.f/(ONE_F)


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
	if (flipx) { p0.x = (float)width  - p0.x; p1.x = (float)width  - p1.x; }
	if (flipy) { p0.y = (float)height - p0.y; p1.y = (float)height - p1.y; }
	float2 d;
	d.x = p1.x - p0.x;
	d.y = p1.y - p0.y;
	int w1 = width - 1;
	int h1 = height - 1;
	float kx = d.x/d.y;
	float ky = d.y/d.x;
	
	while(p0.x != p1.x || p0.y != p1.y) {
		int ix = max((int)p0.x, 0);
		int iy = (int)p0.y;
		if (ix > w1) return;

		float2 px, py;
		px.x = (float)(ix + 1);
		py.y = (float)(iy + 1);
		iy = max(0, min(h1, iy));
		
		px.y = p0.y + ky*(px.x - p0.x);
		py.x = p0.x + kx*(py.y - p0.y);
		
		float2 pp1 = p1;
		if (pp1.x > px.x) pp1 = px;
		if (pp1.y > py.y) pp1 = py;
		
		float cover = (pp1.x - p0.x)*ONE_F;
		float area = py.y - 0.5f*(p0.y + pp1.y);
		if (flipx) { ix = w1 - ix; cover = -cover; }
		if (flipy) { iy = h1 - iy; area = 1.f - area; }
		p0 = pp1;
		
		atomicAdd(
			(unsigned long long*)(marks + 2*(iy*width + ix)),
			((unsigned long long)(unsigned int)(int)(cover) << 32)
			| (unsigned long long)(unsigned int)((int)(area*cover)) );
		//int *mark = marks + ((iy*width + ix) << 1);
		//atomicAdd(mark, (int)(area*cover));
		//atomicAdd(mark + 1, (int)(cover));
	}
}

__global__ void fill(
	int width,
	int2 *marks,
	float4 *image,
	float4 color,
	int4 bounds )
{
	int id = blockIdx.x*blockDim.x + threadIdx.x + bounds.x;
	if (id >= bounds.z) return;
	id += bounds.y*width;
	marks += id;
	image += id;

	int icover = 0;
	while(true) {
		int2 m = *marks;
		*marks = make_int2(0, 0);
		float alpha = (float)abs(m.x + icover)*color.w*DIV_ONE_F;
		marks += width;

		icover += m.y;
		float one_alpha = 1.f - alpha;
		
		float4 p = *image;
		p.x = p.x*one_alpha + color.x*alpha;
		p.y = p.y*one_alpha + color.y*alpha;
		p.z = p.z*one_alpha + color.z*alpha;
		p.w = p.w*one_alpha + color.w*alpha;
		*image = p;
		
		if (++bounds.y >= bounds.w) return;
		image += width;
	}
}

//}