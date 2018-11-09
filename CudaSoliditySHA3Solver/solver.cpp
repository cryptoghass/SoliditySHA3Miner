#include "solver.h"

namespace CUDASolver
{
	void FoundNvAPI64(bool *hasNvAPI64)
	{
		*hasNvAPI64 = CudaSolver::FoundNvAPI64();
	}

	void GetDeviceCount(int *deviceCount, const char *errorMessage)
	{
		CudaSolver::GetDeviceCount(deviceCount, errorMessage);
	}

	void GetDeviceName(int deviceID, const char *deviceName, const char *errorMessage)
	{
		CudaSolver::GetDeviceName(deviceID, deviceName, errorMessage);
	}

	CudaSolver *GetInstance() noexcept
	{
		try { return new CudaSolver(); }
		catch (...) { return nullptr; }
	}

	void DisposeInstance(CudaSolver *instance) noexcept
	{
		try
		{
			instance->~CudaSolver();
			free(instance);
		}
		catch (...) {}
	}

	MessageCallback SetOnMessageHandler(CudaSolver *instance, MessageCallback messageCallback)
	{
		instance->m_messageCallback = messageCallback;
		return messageCallback;
	}

	void GetDeviceProperties(CudaSolver *instance, int deviceID, uint32_t *pciBusID, const char *deviceName, int *computeMajor, int *computeMinor,
								const char *errorMessage)
	{
		instance->GetDeviceProperties(deviceID, pciBusID, deviceName, computeMajor, computeMinor, errorMessage);
	}

	void InitializeDevice(CudaSolver *instance, int deviceID, uint32_t pciBusID, uint32_t maxSolutionCount,
							uint64_t **solutions, uint32_t **solutionCount, uint64_t **solutionsDevice, uint32_t **solutionCountDevice,
							const char *errorMessage)
	{
		instance->InitializeDevice(deviceID, pciBusID, maxSolutionCount, solutions, solutionCount, solutionsDevice, solutionCountDevice, errorMessage);
	}

	void SetDevice(CudaSolver *instance, int deviceID, const char *errorMessage)
	{
		instance->SetDevice(deviceID, errorMessage);
	}

	void ResetDevice(CudaSolver *instance, int deviceID, const char *errorMessage)
	{
		instance->ResetDevice(deviceID, errorMessage);
	}

	void FreeObject(CudaSolver *instance, int deviceID, void *object, const char *errorMessage)
	{
		instance->FreeObject(deviceID, object, errorMessage);
	}

	void PushHigh64Target(CudaSolver *instance, uint64_t *high64Target, const char *errorMessage)
	{
		instance->PushHigh64Target(high64Target, errorMessage);
	}

	void PushMidState(CudaSolver *instance, sponge_ut *midState, const char *errorMessage)
	{
		instance->PushMidState(midState, errorMessage);
	}

	void PushTarget(CudaSolver *instance, byte32_t *target, const char *errorMessage)
	{
		instance->PushTarget(target, errorMessage);
	}

	void PushMessage(CudaSolver *instance, message_ut *message, const char *errorMessage)
	{
		instance->PushMessage(message, errorMessage);
	}

	void HashMidState(CudaSolver *instance, dim3 *grid, dim3 *block,
						uint64_t *solutionsDevice, uint32_t *solutionCountDevice,
						uint32_t *maxSolutionCount, uint64_t *workPosition, const char *errorMessage)
	{
		instance->HashMidState(grid, block,
								solutionsDevice, solutionCountDevice,
								maxSolutionCount, workPosition, errorMessage);
	}

	void HashMessage(CudaSolver *instance, dim3 *grid, dim3 *block,
		uint64_t *solutionsDevice, uint32_t *solutionCountDevice,
		uint32_t *maxSolutionCount, uint64_t *workPosition, const char *errorMessage)
	{
		instance->HashMessage(grid, block,
								solutionsDevice, solutionCountDevice,
								maxSolutionCount, workPosition, errorMessage);
	}

	void GetDeviceSettingMaxCoreClock(CudaSolver *instance, int deviceID, int *coreClock)
	{
		*coreClock = instance->GetDeviceSettingMaxCoreClock(deviceID);
	}

	void GetDeviceSettingMaxMemoryClock(CudaSolver *instance, int deviceID, int *memoryClock)
	{
		*memoryClock = instance->GetDeviceSettingMaxMemoryClock(deviceID);
	}

	void GetDeviceSettingPowerLimit(CudaSolver *instance, int deviceID, int *powerLimit)
	{
		*powerLimit = instance->GetDeviceSettingPowerLimit(deviceID);
	}

	void GetDeviceSettingThermalLimit(CudaSolver *instance, int deviceID, int *thermalLimit)
	{
		*thermalLimit = instance->GetDeviceSettingThermalLimit(deviceID);
	}

	void GetDeviceSettingFanLevelPercent(CudaSolver *instance, int deviceID, int *fanLevel)
	{
		*fanLevel = instance->GetDeviceSettingFanLevelPercent(deviceID);
	}

	void GetDeviceCurrentFanTachometerRPM(CudaSolver *instance, int deviceID, int *tachometerRPM)
	{
		*tachometerRPM = instance->GetDeviceCurrentFanTachometerRPM(deviceID);
	}

	void GetDeviceCurrentTemperature(CudaSolver *instance, int deviceID, int *temperature)
	{
		*temperature = instance->GetDeviceCurrentTemperature(deviceID);
	}

	void GetDeviceCurrentCoreClock(CudaSolver *instance, int deviceID, int *coreClock)
	{
		*coreClock = instance->GetDeviceCurrentCoreClock(deviceID);
	}

	void GetDeviceCurrentMemoryClock(CudaSolver *instance, int deviceID, int *memoryClock)
	{
		*memoryClock = instance->GetDeviceCurrentMemoryClock(deviceID);
	}

	void GetDeviceCurrentUtilizationPercent(CudaSolver *instance, int deviceID, int *utiliztion)
	{
		*utiliztion = instance->GetDeviceCurrentUtilizationPercent(deviceID);
	}

	void GetDeviceCurrentPstate(CudaSolver *instance, int deviceID, int *pState)
	{
		*pState = instance->GetDeviceCurrentPstate(deviceID);
	}

	void GetDeviceCurrentThrottleReasons(CudaSolver *instance, int deviceID, const char *throttleReasons, uint64_t *reasonSize)
	{
		auto reasons = instance->GetDeviceCurrentThrottleReasons(deviceID);
		const char *reasonStr = reasons.c_str();
		std::memcpy((void *)throttleReasons, reasonStr, reasons.length());
		std::memset((void *)&throttleReasons[reasons.length()], '\0', 1ull);
		*reasonSize = reasons.length();
	}
}