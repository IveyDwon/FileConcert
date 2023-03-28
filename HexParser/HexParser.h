#pragma once


extern "C"
{
	#pragma pack(8)
	struct LinearSegment
	{
		unsigned int startAddr;
		unsigned char * data;
		size_t size;
	};


	// return count of segments in hex file
	__declspec(dllexport) size_t __stdcall HexParse(wchar_t * file);
}
