#pragma once

#include <algorithm>
#include <chrono>
#include <memory>
#include <random>
#include <thread>
#include <device_launch_parameters.h>
#include "device/nv_api.h"
#include "device/instance.h"

// --------------------------------------------------------------------
// CUDA common constants
// --------------------------------------------------------------------

__constant__ static uint64_t const Keccak_f1600_RC[24] =
{
	0x0000000000000001, 0x0000000000008082, 0x800000000000808a,
	0x8000000080008000, 0x000000000000808b, 0x0000000080000001,
	0x8000000080008081, 0x8000000000008009, 0x000000000000008a,
	0x0000000000000088, 0x0000000080008009, 0x000000008000000a,
	0x000000008000808b, 0x800000000000008b, 0x8000000000008089,
	0x8000000000008003, 0x8000000000008002, 0x8000000000000080,
	0x000000000000800a, 0x800000008000000a, 0x8000000080008081,
	0x8000000000008080, 0x0000000080000001, 0x8000000080008008
};

namespace CUDASolver
{
	typedef void(*MessageCallback)(int deviceID, const char *type, const char *message);

	class CudaSolver
	{
	public:
		MessageCallback m_messageCallback;

		bool isSubmitStale;

	private:
		std::vector<std::unique_ptr<Device::Instance>> m_deviceInstances;

	public:
		static bool FoundNvAPI64();

		static void GetDeviceCount(int *deviceCount, const char *errorMessage);

		static void GetDeviceName(int deviceID, const char *deviceName, const char *errorMessage);

		CudaSolver() noexcept;
		~CudaSolver() noexcept;

		void GetDeviceProperties(int deviceID, uint32_t *pciBusID, const char *deviceName,
									int *computeMajor, int *computeMinor, const char *errorMessage);

		void InitializeDevice(int deviceID, uint32_t pciBusID, uint32_t maxSolutionCount,
								uint64_t **solutions, uint32_t **solutionCount,
								uint64_t **solutionsDevice, uint32_t **solutionCountDevice,
								const char *errorMessage);

		void SetDevice(int deviceID, const char *errorMessage);
		void ResetDevice(int deviceID, const char *errorMessage);
		void FreeObject(int deviceID, void *object, const char *errorMessage);

		void PushHigh64Target(uint64_t *high64Target, const char *errorMessage);
		void PushMidState(sponge_ut *midState, const char *errorMessage);
		void PushTarget(byte32_t *target, const char *errorMessage);
		void PushMessage(message_ut *message, const char *errorMessage);

		void HashMidState(dim3 *grid, dim3 *block,
							uint64_t *solutionsDevice, uint32_t *solutionCountDevice,
							uint32_t *maxSolutionCount, uint64_t *workPosition, const char *errorMessage);

		void HashMessage(dim3 *grid, dim3 *block,
							uint64_t *solutionsDevice, uint32_t *solutionCountDevice,
							uint32_t *maxSolutionCount, uint64_t *workPosition, const char *errorMessage);

		int GetDeviceSettingMaxCoreClock(int deviceID);
		int GetDeviceSettingMaxMemoryClock(int deviceID);
		int GetDeviceSettingPowerLimit(int deviceID);
		int GetDeviceSettingThermalLimit(int deviceID);
		int GetDeviceSettingFanLevelPercent(int deviceID);

		int GetDeviceCurrentFanTachometerRPM(int deviceID);
		int GetDeviceCurrentTemperature(int deviceID);
		int GetDeviceCurrentCoreClock(int deviceID);
		int GetDeviceCurrentMemoryClock(int deviceID);
		int GetDeviceCurrentUtilizationPercent(int deviceID);
		int GetDeviceCurrentPstate(int deviceID);
		std::string GetDeviceCurrentThrottleReasons(int deviceID);
	};
}