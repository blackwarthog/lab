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

#include <cassert>
#include <cstring>

#include <iostream>

#include "cudacontext.h"


using namespace std;


CudaParams::CudaParams(): params_buffer_size() {
	params_extra.push_back(CU_LAUNCH_PARAM_BUFFER_POINTER);
	params_extra.push_back(NULL);
	params_extra.push_back(CU_LAUNCH_PARAM_BUFFER_SIZE);
	params_extra.push_back(&params_buffer_size);
	params_extra.push_back(CU_LAUNCH_PARAM_END);
}

void CudaParams::reset() {
	params_buffer.clear();
	params_offsets.clear();
	params_pointers.clear();
	params_extra.clear();
	params_buffer_size = 0;
}

CudaParams& CudaParams::add(const void* data, int size, int align) {
	assert(align > 0);

	int index = params_buffer.empty() ? 0 : ((params_buffer.size() - 1)/align + 1)*align;
	params_buffer.resize(index + size);
	memcpy(&params_buffer[index], data, size);
	params_buffer_size = params_buffer.size();

	params_offsets.push_back(index);

	char *root = &params_buffer.front();
	params_pointers.push_back(root + index);
	if (params_pointers.front() != root) {
		params_pointers.clear();
		for(std::vector<int>::iterator i = params_offsets.begin(); i != params_offsets.end(); ++i)
			params_pointers.push_back(root + *i);
	}
	params_extra[1] = root;

	return *this;
}


CudaContext::CudaContext():
	device(),
	context(),
	err()
{
	const int device_index = 0;

	err = cuInit(0);
    assert(!err);

    err = cuDeviceGet(&device, device_index);
    assert(!err);

    char device_name[1024] = {};
    err = cuDeviceGetName(device_name, sizeof(device_name), device);
    assert(!err);
    //cout << "CUDA device " << device_index << ": " << device_name << endl;

    err = cuCtxCreate(&context, CU_CTX_SCHED_AUTO, device);
    assert(!err);

    //hello();
}

CudaContext::~CudaContext() {
	cuCtxDestroy(context);
}

void CudaContext::hello() {
	CUmodule module;
	err = cuModuleLoad(&module, "cuda/hello.ptx");
	assert(!err);

	CUfunction kernel;
	err = cuModuleGetFunction(&kernel, module, "hello");
	assert(!err);

	char data[] = "......";

	CUdeviceptr buffer;
	err = cuMemAlloc(&buffer, sizeof(data));

	CudaParams params;
	params.add(buffer);

	err = cuLaunchKernel(
		kernel,
		1, 1, 1,
		sizeof(data), 1, 1,
		0, 0, 0,
		params.get_extra() );
	assert(!err);

	err = cuStreamSynchronize(0);
	assert(!err);

	err = cuMemcpyDtoH(data, buffer, sizeof(data));
	assert(!err);

	err = cuMemFree(buffer);
	assert(!err);

	err = cuModuleUnload(module);
	assert(!err);

	cout << data << endl;
}
