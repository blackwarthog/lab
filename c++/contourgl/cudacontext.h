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

#ifndef _CUDACONTEXT_H_
#define _CUDACONTEXT_H_

#include <vector>

#include <cuda.h>


class CudaParams {
private:
	std::vector<char> params_buffer;
	std::vector<int> params_offsets;
	std::vector<void*> params_pointers;
	std::vector<void*> params_extra;
	size_t params_buffer_size;

public:
	CudaParams();

	void reset();

	CudaParams& add(const void* data, int size, int align);

	template<typename T>
	CudaParams& add(const T &data, int align)
		{ return add(&data, sizeof(data), align); }

	template<typename T>
	CudaParams& add(const T &data)
		{ return add(data, __alignof(T)); }

	void** get_params() const
		{ return params_pointers.empty() ? NULL : &(const_cast<CudaParams*>(this)->params_pointers.front()); }
	void** get_extra() const
		{ return params_extra.empty() ? NULL : &(const_cast<CudaParams*>(this)->params_extra.front()); }
};


class CudaContext {
public:
	CUdevice device;
	CUcontext context;
	CUresult err;

	CudaContext();
	~CudaContext();

	void hello();
};

#endif
