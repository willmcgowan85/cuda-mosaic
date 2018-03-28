#include "cuda_runtime.h"
#include "math.h"

__device__ int diff(int a, int b)
{
	return (((16711680 & a) - (16711680 & b)) >> 16) * (((16711680 & a) - (16711680 & b)) >> 16)
		+ (((65280 & a) - (65280 & b)) >> 8) * (((65280 & a) - (65280 & b)) >> 8)
		+ ((255 & a) - (255 & b)) * ((255 & a) - (255 & b));
}

__device__ int diff_advanced(int a, int b, const float* weights)
{
	return (int) ((float)(abs((16711680 & a) - (16711680 & b)) >> 16) * weights[0]
		+ (float)abs(((65280 & a) - (65280 & b)) >> 8) * weights[1]
		+ (float)abs((255 & a) - (255 & b)) * weights[2]);
}

//__device__ int diff_advanced(int a, int b, const int* weights)
//{
//	return (((16711680 & a) - (16711680 & b)) >> 16) * (((16711680 & a) - (16711680 & b)) >> 16) * weights[0]
//		+ (((65280 & a) - (65280 & b)) >> 8) * (((65280 & a) - (65280 & b)) >> 8) * weights[1]
//		+ ((255 & a) - (255 & b)) * ((255 & a) - (255 & b)) * weights[2];
//}

__global__ void kernel(const int* tiles, const int* grid, int checks, int tilewidth, int* scores, int* bests, int tileN, int gridN, int count, int top)
{
	//__shared__ int cutoff;
	__shared__ int* best;
	__shared__ int* topscores;

	int block = gridDim.x * blockIdx.y + blockIdx.x;

	if (threadIdx.x == 0) {
		//cutoff = INT_MAX;
		best = new int[top];
		topscores = new int[top];
		for (int i = 0; i < top; i++) {
			topscores[i] = INT_MAX;
		}
	}

	__syncthreads();

	if (block < count) {
		for (int c = 0; c < checks; c++) {
			int t = ((threadIdx.x * checks) + c) * tilewidth;
			if (t < tileN) {
				int g = block * tilewidth;
				if (g < gridN) {
					int score = 0;
					int i = 0;
					while (i < tilewidth && score < topscores[top - 1]) {
						score += diff(tiles[t + i], grid[g + i]);
						i++;
					}
					if (score < topscores[top - 1]) {
						int besttile = (threadIdx.x * checks) + c;;
						for (int i = 0; i < top; i++) {
							if (score < topscores[i]) {
								int temp = topscores[i];
								topscores[i] = score;
								score = temp;
								temp = best[i];
								best[i] = besttile;
								besttile = temp;
							}
						}
					}
				}
			}
		}
	}

	__syncthreads();

	if (threadIdx.x == 0) {
		if (block < count) {
			for (int i = 0; i < top; i++) {
				bests[block * top + i] = best[i];
				scores[block * top + i] = topscores[i];
			}
		}
	}
}

__global__ void kernel_advanced(const int* tiles, const int* grid, const int tilecount, const int gridcount, const int tilewidth, const float* weights, int* scores, int* bests)
{
	int block = gridDim.x * blockIdx.y + blockIdx.x;
	int index = blockDim.x * block + threadIdx.x;
	int gridindex = index / tilecount;
	int tileindex = index % tilecount;
	if (gridindex < gridcount) {
		int score = 0;
		int t = tileindex * tilewidth;
		int g = gridindex * tilewidth;
		for (int i = 0; i < tilewidth; i++) {
			score += diff_advanced(tiles[t + i], grid[g + i], weights);
		}
		scores[index] = score;
		bests[index] = tileindex;
	}
	__syncthreads();

	//if (block < blocks) {
	//	scores[block * threads + threadIdx.x] = INT_MAX;
	//	for (int c = 0; c < checks; c++) {
	//		int t = ((threadIdx.x * checks) + c) * tilewidth;
	//		if (t < tileN) {
	//			int g = block * tilewidth;
	//			if (g < gridN) {
	//				int score = 0;
	//				int i = 0;
	//				while (i < tilewidth && score < scores[block * threads + threadIdx.x]) {
	//					score += diff_advanced(tiles[t + i], grid[g + i], weights);
	//					i++;
	//				}
	//				if (score < scores[block * threads + threadIdx.x]) {
	//					scores[block * threads + threadIdx.x] = score;
	//					bests[block * threads + threadIdx.x] = (threadIdx.x * checks) + c;
	//				}
	//			}
	//		}
	//	}
	//}
}
//__global__ void kernel_advanced(const int* tiles, const int* grid, int checks, int tilewidth, int* scores, int* bests, int tileN, int gridN, int blocks, int dither, const int* weights, const int threads)
//{
//	int block = gridDim.x * blockIdx.y + blockIdx.x;
//	if (block < blocks) {
//		scores[block * threads + threadIdx.x] = INT_MAX;
//		for (int c = 0; c < checks; c++) {
//			int t = ((threadIdx.x * checks) + c) * tilewidth;
//			if (t < tileN) {
//				int g = block * tilewidth;
//				if (g < gridN) {
//					int score = 0;
//					int i = 0;
//					while (i < tilewidth && score < scores[block * threads + threadIdx.x]) {
//						score += diff_advanced(tiles[t + i], grid[g + i], weights);
//						i++;
//					}
//					if (score < scores[block * threads + threadIdx.x]) {
//						scores[block * threads + threadIdx.x] = score;
//						bests[block * threads + threadIdx.x] = (threadIdx.x * checks) + c;
//					}
//				}
//			}
//		}
//	}
//}

int main()
{
	return 0;
}