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

#ifndef _CLCONTEXT_H_
#define _CLCONTEXT_H_

#include <vector>
#include <string>

#include <CL/opencl.h>


class ClContext {
public:
	cl_int err;
	cl_context context;
	std::vector<cl_device_id> devices;
	cl_command_queue queue;

	unsigned int max_compute_units;
	size_t max_group_size;

	ClContext();
	~ClContext();

	void hello();
	cl_program load_program(const std::string &filename);
	static void callback(const char *, const void *, size_t, void *);
};

#endif
