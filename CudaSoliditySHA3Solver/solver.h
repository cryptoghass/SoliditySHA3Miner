#pragma once

#ifndef __SOLVER__
#define __SOLVER__

#include "cudaSolver.h"

#ifdef __linux__
#	define EXPORT
#	define __CDECL__
#else
#	define EXPORT _declspec(dllexport)
#	define __CDECL__ __cdecl
#endif

namespace CUDASolver
{
	extern "C"
	{
		EXPORT void __CDECL__ FoundNvAPI64(bool *hasNvAPI64);

		EXPORT void __CDECL__ GetDeviceCount(int *deviceCount, const char *errorMessage);

		EXPORT void __CDECL__ GetDeviceName(int deviceID, const char *deviceName, const char *errorMessage);

		EXPORT CudaSolver *__CDECL__ GetInstance() noexcept;

		EXPORT void __CDECL__ DisposeInstance(CudaSolver *instance) noexcept;

		EXPORT MessageCallback __CDECL__ SetOnMessageHandler(CudaSolver *instance, MessageCallback messageCallback);

		EXPORT void __CDECL__ GetDeviceProperties(CudaSolver *instance, int deviceID, uint32_t *pciBusID, const char *deviceName, int *computeMajor, int *computeMinor,
													const char *errorMessage);

		EXPORT void __CDECL__ InitializeDevice(CudaSolver *instance, int deviceID, uint32_t pciBusID, uint32_t maxSolutionCount,
												uint64_t **solutions, uint32_t **solutionCount, uint64_t **solutionsDevice, uint32_t **solutionCountDevice,
												const char *errorMessage);

		EXPORT void __CDECL__ SetDevice(CudaSolver *instance, int deviceID, const char *errorMessage);

		EXPORT void __CDECL__ ResetDevice(CudaSolver *instance, int deviceID, const char *errorMessage);

		EXPORT void __CDECL__ FreeObject(CudaSolver *instance, int deviceID, void *object, const char *errorMessage);

		EXPORT void __CDECL__ PushHigh64Target(CudaSolver *instance, uint64_t *high64Target, const char *errorMessage);

		EXPORT void __CDECL__ PushMidState(CudaSolver *instance, sponge_ut *midState, const char *errorMessage);

		EXPORT void __CDECL__ PushTarget(CudaSolver *instance, byte32_t *high64Target, const char *errorMessage);

		EXPORT void __CDECL__ PushMessage(CudaSolver *instance, message_ut *midState, const char *errorMessage);

		EXPORT void __CDECL__ HashMidState(CudaSolver *instance, dim3 *grid, dim3 *block,
											uint64_t *solutionsDevice, uint32_t *solutionCountDevice,
											uint32_t *maxSolutionCount, uint64_t *workPosition, const char *errorMessage);

		EXPORT void __CDECL__ HashMessage(CudaSolver *instance, dim3 *grid, dim3 *block,
											uint64_t *solutionsDevice, uint32_t *solutionCountDevice,
											uint32_t *maxSolutionCount, uint64_t *workPosition, const char *errorMessage);

		EXPORT void __CDECL__ GetDeviceSettingMaxCoreClock(CudaSolver *instance, int deviceID, int *coreClock);

		EXPORT void __CDECL__ GetDeviceSettingMaxMemoryClock(CudaSolver *instance, int deviceID, int *memoryClock);

		EXPORT void __CDECL__ GetDeviceSettingPowerLimit(CudaSolver *instance, int deviceID, int *powerLimit);

		EXPORT void __CDECL__ GetDeviceSettingThermalLimit(CudaSolver *instance, int deviceID, int *thermalLimit);

		EXPORT void __CDECL__ GetDeviceSettingFanLevelPercent(CudaSolver *instance, int deviceID, int *fanLevel);

		EXPORT void __CDECL__ GetDeviceCurrentFanTachometerRPM(CudaSolver *instance, int deviceID, int *tachometerRPM);

		EXPORT void __CDECL__ GetDeviceCurrentTemperature(CudaSolver *instance, int deviceID, int *temperature);

		EXPORT void __CDECL__ GetDeviceCurrentCoreClock(CudaSolver *instance, int deviceID, int *coreClock);

		EXPORT void __CDECL__ GetDeviceCurrentMemoryClock(CudaSolver *instance, int deviceID, int *memoryClock);

		EXPORT void __CDECL__ GetDeviceCurrentUtilizationPercent(CudaSolver *instance, int deviceID, int *utiliztion);

		EXPORT void __CDECL__ GetDeviceCurrentPstate(CudaSolver *instance, int deviceID, int *pState);

		EXPORT void __CDECL__ GetDeviceCurrentThrottleReasons(CudaSolver *instance, int deviceID,
																const char *throttleReasons, uint64_t *reasonSize);
	}
}

#endif // !__SOLVER__