#include "cuda_runtime.h"

__device__ int pixel_diff(int a, int b)
{
	return (((16711680 & a) - (16711680 & b)) >> 16) * (((16711680 & a) - (16711680 & b)) >> 16)
		+ (((65280 & a) - (65280 & b)) >> 8) * (((65280 & a) - (65280 & b)) >> 8)
		+ ((255 & a) - (255 & b)) * ((255 & a) - (255 & b));
}

__global__ void kernel(const int* tiles, const int* grid, const int tilecount, const int gridcount, const int tilewidth, int* scores)
{
	int index = blockDim.x * (gridDim.x * blockIdx.y + blockIdx.x) + threadIdx.x;
	int gridindex = index / tilecount;
	int tileindex = index % tilecount;
	if (gridindex < gridcount) {
		int score = 0;
		for (int tilepixel = 0; tilepixel < tilewidth; tilepixel++) {
			score += pixel_diff(
				tiles[tileindex * tilewidth + tilepixel], 
				grid[gridindex * tilewidth + tilepixel]
			);
		}
		scores[index] = score;
	}
	//__syncthreads();
}

int main()
{
	return 0;
}