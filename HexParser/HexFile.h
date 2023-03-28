#pragma once

#include <string>
#include <fstream>
#include <vector>
#include <algorithm>
#include <chrono>
#include <thread>
#include <atomic>
#include <mutex>
#include <array>

struct Segment
{
	enum RecordType 
	{
		H0,		// for intel format
		H1,
		H2,
		H3,
		H4,
		H5,

		S0,		// for moto format
		S1,
		S2,
		S3,
		S5,
		S7,
		S8,
		S9,
	};

	RecordType recordType;
	unsigned int baseAddr;
	unsigned int offsetAddr;
	size_t recordSize;

	std::vector<unsigned char> data;

	Segment(RecordType recordType, unsigned int baseAddr) : recordType(recordType), baseAddr(baseAddr) {offsetAddr = 0; recordSize = 0;}
};

class HexFile
{
public:
	HexFile(bool filterDirtyData = false)
		: parseProgress(0.0f), errorLineNumber(-1), readToParsingRadio(0.5f), filterDirtyData(filterDirtyData)
	{
	}

	HexFile(const std::wstring & file, bool filterDirtyData = false) 
		: parseProgress(0.0f), errorLineNumber(-1), readToParsingRadio(0.5f), filterDirtyData(filterDirtyData)
	{ 
		Parse(file);
	}

	HexFile(const std::wstring & file, unsigned char fillByte, bool filterDirtyData = false) 
		: parseProgress(0.0f), errorLineNumber(-1), readToParsingRadio(0.5f), filterDirtyData(filterDirtyData)
	{
		if(Parse(file))
		{
			Fill(fillByte);
		}	
	}
	
	~HexFile(void)
	{
	}

	bool Parse(const std::wstring & file);
	
	bool Parse(const std::wstring & file, unsigned char byte)
	{
		if (Parse(file))
		{	
			Fill(byte);	

			return true;
		}
		else
			return false;
	}
	
	void Fill(unsigned char fillByte)
	{
		segments.erase(std::remove_if(segments.begin(), segments.end(), [&](Segment &seg)->bool {return seg.recordType == Segment::RecordType::H1;}));

		if (segments.size() > 0)
		{
			std::qsort(segments.data(), segments.size(), sizeof(Segment), 
				[] (const void * a, const void * b) -> int
				{
					unsigned long long aAddr = ((Segment*)a)->baseAddr + ((Segment*)a)->offsetAddr;
					unsigned long long bAddr = ((Segment*)b)->baseAddr + ((Segment*)b)->offsetAddr;
					return (int)(aAddr - bAddr);
				}
			);						
			
			CheckAllAddress(true);

			const size_t maxSegSize = 0x00010000;

			unsigned int linearStartAddr = segments[0].baseAddr;
			unsigned int lastSegEndAddr = segments.back().baseAddr + segments.back().offsetAddr + segments.back().data.size();
			unsigned int lastSegTailGap = maxSegSize - (lastSegEndAddr - linearStartAddr + maxSegSize) % maxSegSize;
			unsigned int linearSpaceSize =  lastSegEndAddr + lastSegTailGap % maxSegSize - linearStartAddr;

			std::vector<unsigned char> fullBuf(linearSpaceSize, fillByte);

			for (size_t i = 0; i < segments.size(); ++i)
			{
				unsigned int curAddr = segments[i].baseAddr + segments[i].offsetAddr;
				unsigned int curSize = segments[i].data.size();

				int targetAddr = curAddr - linearStartAddr;
				std::copy(segments[i].data.begin(), segments[i].data.end(), fullBuf.begin() + targetAddr);
			}

			size_t recSize = segments[0].recordSize;
			Segment::RecordType recType = segments[0].recordType;
			
			segments.clear();

			size_t segCount = (size_t)std::ceil(1.0 * linearSpaceSize / maxSegSize);

			for(size_t i = 0; i < segCount; ++i)
			{
				segments.push_back(Segment(recType, linearStartAddr + i * maxSegSize));
				
				segments.back().recordSize = recSize;

				unsigned int offset = i * maxSegSize;
				segments.back().data.assign(fullBuf.begin() + offset, fullBuf.begin() + offset + maxSegSize);
			}
		}

		segments.push_back(Segment(Segment::RecordType::H1, 0));
	}

	void EnableFilterDirtyData(bool enable) { filterDirtyData = enable; }

	void SetReadToParsingRadio(float ratio) { if (ratio > 0.00001 && ratio < 0.99) readToParsingRadio = ratio;}

	std::vector<Segment> GetAllSegments() { return segments;}

	size_t GetSegmentCount() { return segments.size();}

	float GetParseProgress() { return parseProgress;}
	int GetErrorLine(std::wstring & line) { line = errorLineString; return errorLineNumber; }

	int DumpToIntelFile(const std::wstring & file);

	int GetCleanRawData(const std::wstring & inFile, const std::wstring & dataFile);

private:
	bool ParseIntelRecord(const std::wstring & record, unsigned char & len, unsigned char & type, unsigned int & offset, std::array<unsigned char, 0xFF> & data);
	bool ParseMotoRecord(const std::wstring & record, unsigned char & len, unsigned char & type, unsigned int & offset, std::array<unsigned char, 0xFF> & data);

	bool ParseIntelFile(const std::vector<std::wstring> & lines, size_t fileSize);
	bool ParseMotoFile(const std::vector<std::wstring> & lines, size_t fileSize);

	void SetErrorLine(int lineNubmer, const std::wstring & lineString) { errorLineNumber = lineNubmer; errorLineString = lineString; }

	// for limit of count of input parameters on std::async(), 2013-06-04
	//static int GetRegData(const std::wstring & fileName, const std::wstring & regExp, std::vector<std::wstring> & matchedLines, size_t & matchedChars, std::atomic<float> & progress, int groupIndex = 0, bool caseSensitive = false);
	//static int GetRegData(const std::wstring & fileName, const std::wstring & regExp, std::vector<std::wstring> & matchedLines, size_t & matchedChars,float & progress, int groupIndex);
	static int GetRegData(const std::wstring & fileName, const std::wstring & regExp, std::vector<std::wstring> & matchedLines, size_t & matchedChars,float & progress);

	void RemoveNullSegment()
	{
		for(size_t i = 1; i < segments.size(); ++i)
		{
			if (segments[i - 1].data.size() == 0 && segments[i - 1].recordType != Segment::RecordType::H1)
			{
				segments.erase(segments.begin() + (i - 1));				
				--i;
			}
		}
	}

	bool CheckAllAddress(bool throwException = false)
	{		
		if (segments.size() < 1)
			return true;

		const size_t maxSegSize = 0x00010000;

		for (size_t i = 1; i < segments.size(); ++i)
		{
			unsigned int preSegAddr =  segments[i - 1].baseAddr + segments[i - 1].offsetAddr;
			unsigned int preSegSize = segments[i - 1].data.size();
			unsigned int curSegAddr = segments[i].baseAddr + segments[i].offsetAddr;
			unsigned int curSegSize = segments[i].data.size();
				
			if (preSegSize > maxSegSize || curSegSize > maxSegSize)
				if (throwException)
					throw std::exception("too large size of some segment");
				else
					return false;

			if (preSegAddr + preSegSize > curSegAddr)
				if (throwException)
					throw std::exception("cross over to address of next segment");
				else
					return false;
		}

		if (segments.back().data.size() > maxSegSize)
			if (throwException)
				throw std::exception("too large size of some segment");
			else
				return false;

		return true;
	}

private:	
	std::vector<Segment> segments;

	float readToParsingRadio;
	std::atomic<float> parseProgress;

	std::atomic<bool> filterDirtyData;

	std::atomic<int> errorLineNumber;
	std::wstring errorLineString;
};


