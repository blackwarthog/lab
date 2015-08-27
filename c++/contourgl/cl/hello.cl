//#pragma OPENCL EXTENSION cl_khr_byte_addressable_store : enable
__constant char s[] = "Hello!";
__kernel void hello(__global char *out) {
   size_t tid = get_global_id(0);
   out[tid] = s[tid];
}