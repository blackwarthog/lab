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

#include <string>
#include <iostream>
#include <fstream>

#include "clcontext.h"


using namespace std;


ClContext::ClContext(): err(), context() {

	// platform

	cl_uint platform_count = 0;
	clGetPlatformIDs(0, NULL, &platform_count);
	assert(platform_count);
	cout << platform_count << " platforms" << endl;
	vector<cl_platform_id> platforms(platform_count);
	clGetPlatformIDs(platforms.size(), &platforms.front(), NULL);
	cl_platform_id platform = platforms[0];

	char vendor[256] = { };
	err = clGetPlatformInfo(platform, CL_PLATFORM_VENDOR, sizeof(vendor), vendor, NULL);
	assert(!err);
	cout << "Use CL platform 0 by " << vendor << endl;

	// devices

	cl_uint device_count = 0;
    err = clGetDeviceIDs(platform, CL_DEVICE_TYPE_GPU, 0, NULL, &device_count);
    assert(!err);
    cout << device_count << " devices" << endl;

    devices.resize(device_count);
    err = clGetDeviceIDs(platform, CL_DEVICE_TYPE_GPU, devices.size(), &devices.front(), NULL);
    assert(!err);

    // context

    context = clCreateContext(0, 1, &devices.front(), NULL, NULL, &err);
    assert(context);
}

ClContext::~ClContext() {
	clReleaseContext(context);
}

void ClContext::hello() {

	// data

	char data[] = "......";

	// buffer

	cl_mem buffer = clCreateBuffer(context, CL_MEM_WRITE_ONLY | CL_MEM_USE_HOST_PTR, sizeof(data), data, NULL);
	assert(buffer);

	// program

	ifstream f("cl/hello.cl");
	string text((istreambuf_iterator<char>(f)), istreambuf_iterator<char>());
	const char *text_pointer = text.c_str();
	cl_program program = clCreateProgramWithSource(context, 1, &text_pointer, NULL, NULL);
	assert(program);

	err = clBuildProgram(program, devices.size(), &devices.front(), "", NULL, NULL);
	assert(!err);

	// kernel

	cl_kernel kernel = clCreateKernel(program, "hello", NULL);
	assert(kernel);
	err = clSetKernelArg(kernel, 0, sizeof(buffer), &buffer);
	assert(!err);

	// command queue

	cl_command_queue queue = clCreateCommandQueue(context, devices[0], 0, NULL);
	assert(queue);

	size_t work_group_size = sizeof(data);
	cl_event event = NULL;
	err = clEnqueueNDRangeKernel(
		queue,
		kernel,
		1,
		NULL,
		&work_group_size,
		NULL,
		0,
		NULL,
		&event );
	assert(!err);

	clWaitForEvents(1, &event);

	// read

	clEnqueueReadBuffer(queue, buffer, CL_TRUE, 0, sizeof(data), data, 0, NULL, &event);
	clWaitForEvents(1, &event);
	cout << data << endl;
}