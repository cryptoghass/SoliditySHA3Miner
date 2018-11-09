#include <cuda_runtime.h>
#include <iostream>
#include <string>

// Define this to turn on error checking
#define CUDA_ERROR_CHECK

#define CudaSafeCall(err)					__cudaSafeCall(err, __FILE__, __LINE__)
#define CudaCheckError(err, errMessage)		__cudaCheckError(err, errMessage)
#define CudaSyncAndCheckError(errMessage)	__cudaSyncAndCheckError(errMessage)

__host__ inline std::string __cudaSafeCall(cudaError err, const char *file, const int line)
{
#ifdef CUDA_ERROR_CHECK
	if (cudaSuccess != err)
		return cudaGetErrorString(err);
	else
#endif //CUDA_ERROR_CHECK
		return "";
}

__host__ inline bool __cudaCheckError(cudaError err, const char *errorMessage)
{
#ifdef CUDA_ERROR_CHECK
	if (err != cudaSuccess)
	{
		auto errorMsgChar = cudaGetErrorString(err);
		std::string errorMsg{ errorMsgChar };

		std::memcpy((void *)errorMessage, errorMsgChar, errorMsg.length());
		std::memset((void *)&errorMessage[errorMsg.length()], '\0', 1ull);

		return false;
	}
#endif //CUDA_ERROR_CHECK

	return true;
}

__host__ inline bool __cudaSyncAndCheckError(const char *errorMessage)
{
	cudaError_t response{ cudaSuccess };
	std::string cudaErrors{ "" };

#ifdef CUDA_ERROR_CHECK
	response = cudaGetLastError();
	if (response != cudaSuccess)
	{
		while (response != cudaSuccess)
		{
			if (!cudaErrors.empty()) cudaErrors += " <- ";
			cudaErrors += cudaGetErrorString(response);
			response = cudaGetLastError();
		}
		auto errorChar = cudaErrors.c_str();

		std::memcpy((void *)errorMessage, errorChar, cudaErrors.length());
		std::memset((void *)&errorMessage[cudaErrors.length()], '\0', 1ull);

		return false;
	}
#endif //CUDA_ERROR_CHECK

	response = cudaDeviceSynchronize();

	if (response != cudaSuccess)
	{
		response = cudaGetLastError();

		while (response != cudaSuccess)
		{
			if (!cudaErrors.empty()) cudaErrors += " <- ";
			cudaErrors += cudaGetErrorString(response);
			response = cudaGetLastError();
		}
		auto errorChar = cudaErrors.c_str();

		std::memcpy((void *)errorMessage, errorChar, cudaErrors.length());
		std::memset((void *)&errorMessage[cudaErrors.length()], '\0', 1ull);

		return false;
	}
	return true;
}
