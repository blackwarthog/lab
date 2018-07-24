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

#include <cassert>

#include <iostream>
#include <fstream>

#include "clcontext.h"


using namespace std;


ClContext::ClContext():
	err(),
	device(),
	context(),
	queue(),
	max_compute_units(),
	max_group_size()
{
	const int platform_index = 0;
	const int device_index = 0;

	// platform
	cl_uint platform_count = 0;
	err |= clGetPlatformIDs(0, NULL, &platform_count);
	assert(!err);
	//cout << platform_count << " platforms" << endl;

	vector<cl_platform_id> platforms(platform_count);
	err |= clGetPlatformIDs(platforms.size(), &platforms.front(), NULL);
	assert(!err);

	assert(platform_index < (int)platform_count);
	cl_platform_id platform = platforms[platform_index];

	char vendor[256] = { };
	err |= clGetPlatformInfo(platform, CL_PLATFORM_VENDOR, sizeof(vendor), vendor, NULL);
	assert(!err);
	//cout << "Use CL platform " << platform_index << " by " << vendor << endl;

    char platform_version[256];
    err |= clGetPlatformInfo(platform, CL_PLATFORM_VERSION, sizeof(platform_version), platform_version, NULL);
	assert(!err);
	//cout << "Platform " << platform_index << " OpenCL version " << platform_version << endl;

	// devices

	cl_uint device_count = 0;
    err |= clGetDeviceIDs(platform, CL_DEVICE_TYPE_ALL, 0, NULL, &device_count);
    assert(!err);
    //cout << device_count << " devices" << endl;

	vector<cl_device_id> devices(device_count);
    err |= clGetDeviceIDs(platform, CL_DEVICE_TYPE_ALL, devices.size(), &devices.front(), NULL);
    assert(!err);

	assert(device_index < (int)device_count);
    device = devices[device_index];

    char device_name[256];
    err |= clGetDeviceInfo(device, CL_DEVICE_NAME, sizeof(device_name), device_name, NULL);
    assert(!err);
    //cout << "Device " << device_index << " name " << device_name << endl;

    char device_version[256];
    err |= clGetDeviceInfo(device, CL_DEVICE_VERSION, sizeof(device_version), device_version, NULL);
    assert(!err);
    //cout << "Device " << device_index << " OpenCL version " << device_version << endl;

    err |= clGetDeviceInfo(device, CL_DEVICE_MAX_COMPUTE_UNITS, sizeof(max_compute_units), &max_compute_units, NULL);
    assert(!err);
    //cout << "Device " << device_index << " max compute units " << max_compute_units << endl;

    unsigned int max_dimensions;
    err |= clGetDeviceInfo(device, CL_DEVICE_MAX_WORK_ITEM_DIMENSIONS, sizeof(max_dimensions), &max_dimensions, NULL);
    assert(!err);
    assert(max_dimensions);
    //cout << "Device " << device_index << " max work dimensions " << max_dimensions << endl;

    vector<size_t> max_group_sizes(max_dimensions);
    err |= clGetDeviceInfo(device, CL_DEVICE_MAX_WORK_GROUP_SIZE, max_group_sizes.size()*sizeof(size_t), &max_group_sizes.front(), NULL);
    assert(!err);
    max_group_size = max_group_sizes.front();
    //cout << "Device " << device_index << " max group size " << max_group_size << endl;

	size_t timer_resolution;
    err |= clGetDeviceInfo(device, CL_DEVICE_PROFILING_TIMER_RESOLUTION, sizeof(timer_resolution), &timer_resolution, NULL);
    assert(!err);
    //cout << "Device " << device_index << " timer resolution " << timer_resolution << " ns" << endl;

    unsigned long long global_mem_size;
    err |= clGetDeviceInfo(device, CL_DEVICE_GLOBAL_MEM_SIZE, sizeof(global_mem_size), &global_mem_size, NULL);
    assert(!err);
    //cout << "Device " << device_index << " global mem size " << global_mem_size << endl;

    unsigned long long local_mem_size;
    err |= clGetDeviceInfo(device, CL_DEVICE_LOCAL_MEM_SIZE, sizeof(local_mem_size), &local_mem_size, NULL);
    assert(!err);
    //cout << "Device " << device_index << " local mem size " << local_mem_size << endl;

    unsigned long long max_constant_buffer_size;
    err |= clGetDeviceInfo(device, CL_DEVICE_MAX_CONSTANT_BUFFER_SIZE, sizeof(max_constant_buffer_size), &max_constant_buffer_size, NULL);
    assert(!err);
    //cout << "Device " << device_index << " max constant buffer size " << max_constant_buffer_size << endl;

	// context

    cl_context_properties context_props[] = {
    	CL_CONTEXT_PLATFORM,       (cl_context_properties)platform,
		CL_NONE };
    context = clCreateContext(context_props, 1, &device, callback, NULL, &err);
    assert(context);

	// command queue

    cl_command_queue_properties props = 0
    	| CL_QUEUE_OUT_OF_ORDER_EXEC_MODE_ENABLE
    	//| CL_QUEUE_PROFILING_ENABLE
    	| 0;
	queue = clCreateCommandQueue(
		context, device, props, NULL);
	assert(queue);

	//hello();
}

ClContext::~ClContext() {
	clReleaseCommandQueue(queue);
	clReleaseContext(context);
}

void ClContext::callback(const char *, const void *, size_t, void *) { }

cl_program ClContext::load_program(const std::string &filename) {
	ifstream f(("cl/" + filename).c_str());
	string text((istreambuf_iterator<char>(f)), istreambuf_iterator<char>());
	const char *text_pointer = text.c_str();
	cl_program program = clCreateProgramWithSource(context, 1, &text_pointer, NULL, NULL);
	assert(program);

	const char options[] = " -cl-fast-relaxed-math -Werror ";

	err = clBuildProgram(program, 1, &device, options, NULL, NULL);
	if (err) {
		size_t size;
		clGetProgramBuildInfo(program, device, CL_PROGRAM_BUILD_LOG, 0, NULL, &size);
		char *log = new char[size];
		clGetProgramBuildInfo(program, device, CL_PROGRAM_BUILD_LOG, size, log, NULL);
		cout << log << endl;
		delete[] log;
	}
	assert(!err);

	return program;
}

void ClContext::hello() {

	// data

	char data[] = "......";

	// buffer

	cl_mem buffer = clCreateBuffer(context, CL_MEM_WRITE_ONLY | CL_MEM_USE_HOST_PTR, sizeof(data), data, NULL);
	assert(buffer);

	// program

	cl_program program = load_program("hello.cl");

	// kernel

	cl_kernel kernel = clCreateKernel(program, "hello", NULL);
	assert(kernel);
	err = clSetKernelArg(kernel, 0, sizeof(buffer), &buffer);
	assert(!err);

	size_t work_group_size = sizeof(data);
	cl_event event1 = NULL, event2 = NULL;
	err = clEnqueueNDRangeKernel(
		queue,
		kernel,
		1,
		NULL,
		&work_group_size,
		NULL,
		0,
		NULL,
		&event1 );
	assert(!err);

	// read

	clEnqueueReadBuffer(queue, buffer, CL_TRUE, 0, sizeof(data), data, 1, &event1, &event2);

	// wait

	clWaitForEvents(1, &event2);
	cout << data << endl;

	// deinitialize

	clReleaseKernel(kernel);
	clReleaseProgram(program);
	clReleaseMemObject(buffer);
}
