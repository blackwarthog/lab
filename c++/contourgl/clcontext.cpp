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
	context(),
	queue(),
	max_compute_units(),
	max_group_size()
{

	// platform

	cl_uint platform_count = 0;
	clGetPlatformIDs(0, NULL, &platform_count);
	assert(platform_count);
	//cout << platform_count << " platforms" << endl;
	vector<cl_platform_id> platforms(platform_count);
	clGetPlatformIDs(platforms.size(), &platforms.front(), NULL);
	cl_platform_id platform = platforms[0];

	char vendor[256] = { };
	err = clGetPlatformInfo(platform, CL_PLATFORM_VENDOR, sizeof(vendor), vendor, NULL);
	assert(!err);
	//cout << "Use CL platform 0 by " << vendor << endl;

    char platform_version[256];
    err = clGetPlatformInfo(platform, CL_PLATFORM_VERSION, sizeof(platform_version), platform_version, NULL);
	assert(!err);
    //cout << "Platform 0 OpenCL version " << platform_version << endl;

	// devices

	cl_uint device_count = 0;
    err = clGetDeviceIDs(platform, CL_DEVICE_TYPE_GPU, 0, NULL, &device_count);
    assert(!err);
    //cout << device_count << " devices" << endl;

    devices.resize(device_count);
    err = clGetDeviceIDs(platform, CL_DEVICE_TYPE_GPU, devices.size(), &devices.front(), NULL);
    assert(!err);

    char device_name[256];
    clGetDeviceInfo(devices.front(), CL_DEVICE_NAME, sizeof(device_name), device_name, NULL);
    //cout << "Device 0 name " << device_name << endl;

    char device_version[256];
    clGetDeviceInfo(devices.front(), CL_DEVICE_VERSION, sizeof(device_version), device_version, NULL);
    //cout << "Device 0 OpenCL version " << device_version << endl;

    clGetDeviceInfo(devices.front(), CL_DEVICE_MAX_COMPUTE_UNITS, sizeof(max_compute_units), &max_compute_units, NULL);
    //cout << "Device 0 max compute units " << max_compute_units << endl;

    clGetDeviceInfo(devices.front(), CL_DEVICE_MAX_WORK_GROUP_SIZE, sizeof(max_group_size), &max_group_size, NULL);
    //cout << "Device 0 max group size " << max_group_size << endl;

    // context

    cl_context_properties context_props[] = {
    	CL_CONTEXT_PLATFORM, (cl_context_properties)platform,
		CL_NONE };
    context = clCreateContext(context_props, 1, &devices.front(), callback, NULL, &err);
    assert(context);

	// command queue

	queue = clCreateCommandQueue(context, devices.front(), CL_QUEUE_OUT_OF_ORDER_EXEC_MODE_ENABLE, NULL);
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

	err = clBuildProgram(program, 1, &devices.front(), "", NULL, NULL);
	if (err) {
		size_t size;
		clGetProgramBuildInfo(program, devices.front(), CL_PROGRAM_BUILD_LOG, 0, NULL, &size);
		char *log = new char[size];
		clGetProgramBuildInfo(program, devices.front(), CL_PROGRAM_BUILD_LOG, size, log, NULL);
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
