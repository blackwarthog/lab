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

#ifndef _ENVIRONMENT_H_
#define _ENVIRONMENT_H_

#include "glcontext.h"
#include "clcontext.h"
#include "shaders.h"

#ifdef CUDA
#include "cudacontext.h"
#endif

class Environment {
public:
	GlContext gl;
	ClContext cl;
	Shaders shaders;

	#ifdef CUDA
	CudaContext cu;
	#endif

	Environment(int width, int height, bool hdr, bool multisample, int samples):
		gl(width, height, hdr, multisample, samples) { }
	void use() { gl.use(); }
	void unuse() { gl.unuse(); }
};

#endif
