// HexParser.cpp : 定义 DLL 应用程序的导出函数。
//

#include "stdafx.h"
#include "HexParser.h"
#include "HexFile.h"

#pragma comment(linker, "/export:HexParse=_HexParse@4")

size_t __stdcall HexParse(wchar_t * file, std::vector<Segment> datas)
{
	HexFile hexFile;

	if (hexFile.Parse(file))
	{
		hexFile.Fill(0xff);

		std::vector<Segment> segs = hexFile.GetAllSegments();
		datas = segs;
		size_t segCount = segs.size();

	/*	LinearSegment * segments = new LinearSegment[segCount];
		for (size_t i = 0; i < segCount; ++i)
		{
			segments[i].startAddr = segs[i].baseAddr + segs[i].offsetAddr;
			segments[i].size = segs[i].data.size();

			segments[i].data = new unsigned char[segs[i].data.size()];
			std::memcpy(segments[i].data, segs[i].data.data(), segs[i].data.size());
		}
		hexFile.DumpToIntelFile(file);*/
		return segCount;
	}
	else
		return 0;
}

