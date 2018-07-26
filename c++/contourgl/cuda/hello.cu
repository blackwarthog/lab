extern "C" {

__constant__ char s[] = "Hello!";


__global__ void hello(char *out) {
	int i = threadIdx.x;
	out[i] = s[i];
}

}