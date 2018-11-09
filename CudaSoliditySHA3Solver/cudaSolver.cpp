#include "cudaErrorCheck.cu"
#include "cudaSolver.h"

namespace CUDASolver
{
	// --------------------------------------------------------------------
	// Static
	// --------------------------------------------------------------------

	bool CudaSolver::FoundNvAPI64()
	{
		return NV_API::FoundNvAPI64();
	}

	void CudaSolver::GetDeviceCount(int *deviceCount, const char *errorMessage)
	{
		if (!CudaCheckError(cudaGetDeviceCount(deviceCount), errorMessage))
			return;

		if (*deviceCount < 1)
		{
			int runtimeVersion = 0;
			if (!CudaCheckError(cudaRuntimeGetVersion(&runtimeVersion), errorMessage))
				return;

			auto errorMsg = std::string("There are no available device(s) that support CUDA (requires: 9.2, current: "
							+ std::to_string(runtimeVersion / 1000) + "." + std::to_string((runtimeVersion % 100) / 10) + ")");

			auto errorMsgChar = errorMsg.c_str();

			std::memcpy((void *)errorMessage, errorMsgChar, errorMsg.length());
			std::memset((void *)&errorMessage[errorMsg.length()], '\0', 1ull);
		}
	}

	void CudaSolver::GetDeviceName(int deviceID, const char *deviceName, const char *errorMessage)
	{
		cudaDeviceProp devProp;

		if (!CudaCheckError(cudaGetDeviceProperties(&devProp, deviceID), errorMessage))
			return;

		std::string devName{ devProp.name };
		std::memcpy((void *)deviceName, devProp.name, devName.length());
		std::memset((void *)&deviceName[devName.length()], '\0', 1ull);
	}

	// --------------------------------------------------------------------
	// Public
	// --------------------------------------------------------------------

	CudaSolver::CudaSolver() noexcept
	{
		try
		{
			if (NV_API::FoundNvAPI64()) NV_API::initialize();
		}
		catch (std::exception ex)
		{
			m_messageCallback(-1, "Error", ex.what());
		}
	}

	CudaSolver::~CudaSolver() noexcept
	{
		try
		{
			m_deviceInstances.clear();
			NV_API::unload();
		}
		catch (...) {}
	}

	void CudaSolver::GetDeviceProperties(int deviceID, uint32_t *pciBusID, const char *deviceName,
											int *computeMajor, int *computeMinor, const char *errorMessage)
	{
		struct cudaDeviceProp deviceProp;
		if (!CudaCheckError(cudaGetDeviceProperties(&deviceProp, deviceID), errorMessage))
			return;

		char pciBusID_s[13];
		if (!CudaCheckError(cudaDeviceGetPCIBusId(pciBusID_s, 13, deviceID), errorMessage))
			return;

		*pciBusID = strtoul(std::string{ pciBusID_s }.substr(5, 2).c_str(), NULL, 16);

		std::string devicePropName{ deviceProp.name };
		std::memcpy((void *)deviceName, deviceProp.name, devicePropName.length());
		std::memset((void *)&deviceName[devicePropName.length()], '\0', 1ull);

		*computeMajor = deviceProp.major;
		*computeMinor = deviceProp.minor;
	}

	void CudaSolver::InitializeDevice(int deviceID, uint32_t pciBusID, uint32_t maxSolutionCount,
										uint64_t **solutions, uint32_t **solutionCount,
										uint64_t **solutionsDevice, uint32_t **solutionCountDevice,
										const char *errorMessage)
	{
		if (!CudaCheckError(cudaSetDevice(deviceID), errorMessage))
			return;

		if (!CudaCheckError(cudaDeviceReset(), errorMessage))
			return;

		if (!CudaCheckError(cudaSetDeviceFlags(cudaDeviceScheduleBlockingSync | cudaDeviceMapHost), errorMessage))
			return;

		if (!CudaCheckError(cudaHostAlloc(reinterpret_cast<void **>(solutionCount), UINT32_LENGTH, cudaHostAllocMapped),
			errorMessage))
			return;

		if (!CudaCheckError(cudaHostAlloc(reinterpret_cast<void **>(solutions), maxSolutionCount * UINT64_LENGTH, cudaHostAllocMapped),
			errorMessage))
			return;

		if (!CudaCheckError(cudaHostGetDevicePointer(reinterpret_cast<void **>(solutionCountDevice), reinterpret_cast<void *>(*solutionCount), 0),
			errorMessage))
			return;

		if (!CudaCheckError(cudaHostGetDevicePointer(reinterpret_cast<void **>(solutionsDevice), reinterpret_cast<void *>(*solutions), 0),
			errorMessage))
			return;

		m_deviceInstances.push_back(std::unique_ptr<Device::Instance>(new Device::Instance(deviceID, pciBusID)));
	}

	void CudaSolver::SetDevice(int deviceID, const char *errorMessage)
	{
		if (!CudaCheckError(cudaSetDevice(deviceID), errorMessage))
		{
			m_messageCallback(deviceID, "Error", errorMessage);
			return;
		}
	}

	void CudaSolver::ResetDevice(int deviceID, const char *errorMessage)
	{
		if (!CudaCheckError(cudaDeviceReset(), errorMessage))
		{
			m_messageCallback(deviceID, "Error", errorMessage);
			return;
		}
	}

	void CudaSolver::FreeObject(int deviceID, void *object, const char *errorMessage)
	{
		if (!CudaCheckError(cudaFreeHost(object), errorMessage))
		{
			m_messageCallback(deviceID, "Error", errorMessage);
			return;
		}
	}

	int CudaSolver::GetDeviceSettingMaxCoreClock(int deviceID)
	{
		int maxCoreClock;

		for (auto& device : m_deviceInstances)
			if (device->DeviceID == deviceID)
				if (device->API.getSettingMaxCoreClock(&maxCoreClock))
					return maxCoreClock;

		return -1;
	}

	int CudaSolver::GetDeviceSettingMaxMemoryClock(int deviceID)
	{
		int maxMemoryClock;

		for (auto& device : m_deviceInstances)
			if (device->DeviceID == deviceID)
				if (device->API.getSettingMaxMemoryClock(&maxMemoryClock))
					return maxMemoryClock;

		return -1;
	}

	int CudaSolver::GetDeviceSettingPowerLimit(int deviceID)
	{
		int powerLimit;

		for (auto& device : m_deviceInstances)
			if (device->DeviceID == deviceID)
				if (device->API.getSettingPowerLimit(&powerLimit))
					return powerLimit;

		return -1;
	}

	int CudaSolver::GetDeviceSettingThermalLimit(int deviceID)
	{
		int thermalLimit;

		for (auto& device : m_deviceInstances)
			if (device->DeviceID == deviceID)
				if (device->API.getSettingThermalLimit(&thermalLimit))
					return thermalLimit;

		return -1;
	}

	int CudaSolver::GetDeviceSettingFanLevelPercent(int deviceID)
	{
		int fanLevel;

		for (auto& device : m_deviceInstances)
			if (device->DeviceID == deviceID)
				if (device->API.getSettingFanLevelPercent(&fanLevel))
					return fanLevel;

		return -1;
	}

	int CudaSolver::GetDeviceCurrentFanTachometerRPM(int deviceID)
	{
		int tachometerRPM;

		for (auto& device : m_deviceInstances)
			if (device->DeviceID == deviceID)
				if (device->API.getCurrentFanTachometerRPM(&tachometerRPM))
					return tachometerRPM;

		return -1;
	}

	int CudaSolver::GetDeviceCurrentTemperature(int deviceID)
	{
		int currentTemperature;

		for (auto& device : m_deviceInstances)
			if (device->DeviceID == deviceID)
				if (device->API.getCurrentTemperature(&currentTemperature))
					return currentTemperature;

		return INT32_MIN;
	}

	int CudaSolver::GetDeviceCurrentCoreClock(int deviceID)
	{
		int currentCoreClock;

		for (auto& device : m_deviceInstances)
			if (device->DeviceID == deviceID)
				if (device->API.getCurrentCoreClock(&currentCoreClock))
					return currentCoreClock;

		return -1;
	}

	int CudaSolver::GetDeviceCurrentMemoryClock(int deviceID)
	{
		int currentMemoryClock;

		for (auto& device : m_deviceInstances)
			if (device->DeviceID == deviceID)
				if (device->API.getCurrentMemoryClock(&currentMemoryClock))
					return currentMemoryClock;

		return -1;
	}

	int CudaSolver::GetDeviceCurrentUtilizationPercent(int deviceID)
	{
		int currentUtilization;

		for (auto& device : m_deviceInstances)
			if (device->DeviceID == deviceID)
				if (device->API.getCurrentUtilizationPercent(&currentUtilization))
					return currentUtilization;

		return -1;
	}

	int CudaSolver::GetDeviceCurrentPstate(int deviceID)
	{
		int currentPstate;

		for (auto& device : m_deviceInstances)
			if (device->DeviceID == deviceID)
				if (device->API.getCurrentPstate(&currentPstate))
					return currentPstate;

		return -1;
	}

	std::string CudaSolver::GetDeviceCurrentThrottleReasons(int deviceID)
	{
		std::string throttleReasons;

		for (auto& device : m_deviceInstances)
			if (device->DeviceID == deviceID)
				if (device->API.getCurrentThrottleReasons(&throttleReasons))
					return throttleReasons;

		return "";
	}
}