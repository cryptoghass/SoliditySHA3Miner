#pragma once

namespace CUDASolver
{
	namespace Device
	{
		class Instance
		{
		public:
			int DeviceID;
			uint32_t PciBusID;

			NV_API API;

			Instance(int deviceID, uint32_t pciBusID) :
				DeviceID{ deviceID },
				PciBusID{ pciBusID },
				API { NV_API(deviceID, pciBusID) }
			{ }
		};
	}
}