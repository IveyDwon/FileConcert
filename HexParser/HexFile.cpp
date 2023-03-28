#include "stdafx.h"

#include <iostream>
#include <regex>
#include <sstream>
#include <future>

#include "HexFile.h"


bool HexFile::Parse(const std::wstring & file)
{
	std::wifstream someFile(file.c_str(), std::ios::in);

	if (!someFile.is_open()) 
		return false;
	
    someFile.seekg(0, std::ifstream::end);
    std::wstreampos curPos = 0, fileSize = someFile.tellg(); 

	someFile.seekg(0);

	std::wstring line;
	std::vector<std::wstring> lines;
	
	bool parseOK = false;
	errorLineNumber = -1;
	errorLineString.clear();

	for(int i = 0; i < 100 && !someFile.eof(); ++i)
	{
		line.clear();
		std::getline(someFile, line);
		
		curPos = someFile.tellg();
		parseProgress = (float)(readToParsingRadio * curPos / fileSize);

		if (line.size() == 0 && !this->filterDirtyData)
			break;

		if (line[0] == L':')
		{
			someFile.seekg(0);

			while(!someFile.eof())
			{
				std::getline(someFile, line);

				curPos = someFile.tellg();
				parseProgress = (float)(readToParsingRadio * curPos / fileSize);

				lines.push_back(line);
			}

			someFile.close();

			parseOK = ParseIntelFile(lines, (size_t)fileSize);

			break;
		}

		if (line[0] == L'S' || line[0] == L's')
		{
			someFile.seekg(0);

			while(!someFile.eof())
			{
				std::getline(someFile, line);

				curPos = someFile.tellg();
				parseProgress = (float)(readToParsingRadio * curPos / fileSize);

				lines.push_back(line);
			}
			
			someFile.close();

			parseOK = ParseMotoFile(lines, (size_t)fileSize);

			break;
		}		

		if (!this->filterDirtyData)
			break;
	}

	return parseOK;
}

bool HexFile::ParseIntelFile(const std::vector<std::wstring> & lines, size_t fileSize)
{	
	if (lines.size() == 0 || fileSize == 0)
		return false;

	segments.clear();

	std::array<unsigned char, 0xFF> dataBuf = {0};

	size_t lineIndex = 0, curSize = 0;
	for(; lineIndex < lines.size(); ++lineIndex)
	{	  
		// skip null line
		if(lines[lineIndex].size() == 0)
			continue;

		// update progress
		curSize += lines[lineIndex].size();
		parseProgress = readToParsingRadio + (float)((1 - readToParsingRadio) * curSize / fileSize);

		unsigned char len, type;
		unsigned int base, offset;
		if (!ParseIntelRecord(lines[lineIndex], len, type, offset, dataBuf))
		{
			if (this->filterDirtyData)
				continue;
			else
			{
				SetErrorLine(lineIndex, lines[lineIndex]);
				break;
			}
		}
				
		switch (type)
		{
		case 0:	// Data Record
			if (segments.size() == 0)	
				segments.push_back(Segment(Segment::RecordType::H0, 0));

			if(segments.back().data.size() == 0)
			{
				segments.back().offsetAddr = offset;
				segments.back().recordSize = len;
			}

			segments.back().data.insert(segments.back().data.end(), dataBuf.begin(), dataBuf.begin() + len);
			break;

		case 1:	// The End of File Record specifies the end of the hexadecimal object file.
			segments.push_back(Segment(Segment::RecordType::H1, 0));
			break;

		case 2:	// Extended Segment Address Record, The 16-bit Extended Segment Address Record is used to specify bits 4-19 of the Segment Base Address (SBA), where bits 0-3 of the SBA are zero. Bits 4-19 of the SBA are referred to as the Upper Segment Base Address(USBA).
			base = ((dataBuf[0] << 8) | dataBuf[1]) << 4;
			segments.push_back(Segment(Segment::RecordType::H2, base));
			break;

		case 3:	// Start Segment Address Record, The Start Segment Address Record can appear anywhere in a 16-bit hexadecimal object file. If such a record is not present in a hexadecimal object file, a loader is free to assign a default start address.
			base = (dataBuf[0] << 24) | (dataBuf[1] << 16) | (dataBuf[2] << 8) | dataBuf[3];
			segments.push_back(Segment(Segment::RecordType::H3, base));
			break;

		case 4:	// Extended Linear Address Record, The 32-bit Extended Linear Address Record is used to specify bits 16-31 of the Linear Base Address (LBA), where bits 0-15 of the LBA are zero. Bits 16-31 of the LBA are referred to as the Upper Linear Base Address (ULBA).
			base = (dataBuf[0] << 24) | (dataBuf[1] << 16);
			segments.push_back(Segment(Segment::RecordType::H4, base));
			break;	

		case 5:	// Start Linear Address Record, The Start Linear Address Record can appear anywhere in a 32-bit hexadecimal object file. If such a record is not present in a hexadecimal object file, a loader is free to assign a default start address.
			base =  (dataBuf[0] << 24) | (dataBuf[1] << 16) | (dataBuf[2] << 8) | dataBuf[3];
			segments.push_back(Segment(Segment::RecordType::H5, base));
			break;
		}		
	}

	RemoveNullSegment();

	return lineIndex == lines.size();
}

bool HexFile::ParseIntelRecord(const std::wstring & record, unsigned char & len, unsigned char & type, unsigned int & offset, std::array<unsigned char, 0xFF> & data)
{
	// check step 0: no any data
	if (record.size() == 0)
		return false;

	// check step 1: length of whole line
	if ((record.size() % 2) != 1)
		return false;

	static std::wsmatch matched;
	static std::wregex regExp(L"^:([A-Fa-f0-9]{2})([A-Fa-f0-9]{4})(0[0-5])([A-Fa-f0-9]{2,})");	

	// check step 2: line format
	if (!std::regex_search(record, matched, regExp))
		return false;

	len = (unsigned char)wcstoul(matched[1].str().c_str(), nullptr, 16);
	offset =  wcstoul(matched[2].str().c_str(), nullptr, 16);
	type =  (unsigned char)wcstoul(matched[3].str().c_str(), nullptr, 16);

	// check step 3: length of data
	if (len != ((matched[4].str().size() / 2) - 1))
		return false;

	// check step 4: CRC and put data in temp buffer sametime
	bool crcOK = false;
	unsigned char crc = 0;
	unsigned char wholeLineLen = 1 + 2 + 4 + 2 + (len * 2) + 2;
	for (size_t j = 1, k = 0; j < wholeLineLen; j += 2)
	{
		unsigned char d = (unsigned char)wcstoul(record.substr(j, 2).c_str(), nullptr, 16);
		if ((j+2) < wholeLineLen)
		{
			crc += d;
			if (j >= (1 + 2 + 4 + 2))
				data[k++] = d;
		}
		else
		{
			crc =  (unsigned char)((~crc) + 1);
			crcOK = (d == crc);
		}
	}
	if (!crcOK)
		return false;
		
	// check step 5: offset must be zero in segment record
	if (type != 0 && offset != 0)
		return false;

	return true;
}

bool HexFile::ParseMotoRecord(const std::wstring & record, unsigned char & len, unsigned char & type, unsigned int & offset, std::array<unsigned char, 0xFF> & data)
{
	return true;
}

bool HexFile::ParseMotoFile(const std::vector<std::wstring> & lines, size_t fileSize)
{
	
	return true;
}


 int HexFile::DumpToIntelFile(const std::wstring & file)
{
	std::wofstream outFile(file.c_str(), std::ios::out | std::ios::binary);
	if (!outFile.is_open()) 
	{
		return -1;
	}		
	
	std::array<wchar_t, 1024> tempBuf;
	std::vector<std::wstring> allLines;

	for(size_t segIndex = 0; segIndex != segments.size(); ++segIndex)
	{		
		unsigned char crc = 0, sum;
		
		switch (segments[segIndex].recordType)
		{
		case Segment::RecordType::H1:
			wsprintf(tempBuf.data(), L"%s", L":00000001FF\r\n");
			break;
		case Segment::RecordType::H2:
			sum = (unsigned char)(segments[segIndex].baseAddr >> 4) +  (unsigned char)(segments[segIndex].baseAddr >> 12);
			crc = ~(02 + 02 + sum) + 1;
			wsprintf(tempBuf.data(), L":02000002%04X%02X\r\n", (unsigned short)(segments[segIndex].baseAddr >> 4), crc);
			break;
		case Segment::RecordType::H3:
			sum = (unsigned char)segments[segIndex].baseAddr +  (unsigned char)(segments[segIndex].baseAddr >> 8) +  (unsigned char)(segments[segIndex].baseAddr >> 16) +  (unsigned char)(segments[segIndex].baseAddr >> 24);
			crc = ~(02 + 03 + sum) + 1;
			wsprintf(tempBuf.data(), L":04000003%08X%02X\r\n", segments[segIndex].baseAddr, crc);
			break;
		case Segment::RecordType::H4:
			sum = (unsigned char)(segments[segIndex].baseAddr >> 16) +  (unsigned char)(segments[segIndex].baseAddr >> 24);
			crc = ~(02 + 04 + sum) + 1;
			wsprintf(tempBuf.data(), L":02000004%04X%02X\r\n", (unsigned short)(segments[segIndex].baseAddr >> 16), crc);
			break;
		case Segment::RecordType::H5:
			sum = (unsigned char)segments[segIndex].baseAddr +  (unsigned char)(segments[segIndex].baseAddr >> 8) +  (unsigned char)(segments[segIndex].baseAddr >> 16) +  (unsigned char)(segments[segIndex].baseAddr >> 24);
			crc = ~(02 + 05 + sum) + 1;
			wsprintf(tempBuf.data(), L":04000005%08X%02X\r\n", segments[segIndex].baseAddr, crc);
			break;
		}

		allLines.push_back(tempBuf.data());

		size_t segSize = segments[segIndex].data.size();

		const size_t lineCount = (size_t)std::ceil(1.0 * segSize / segments[segIndex].recordSize);
		if (lineCount == 0) // ¿Õ¶Î
			continue;

		for (size_t lineIndex = 0; lineIndex < lineCount; ++lineIndex)
		{
			unsigned char realLineSize =  segments[segIndex].recordSize;
			if ((segSize % segments[segIndex].recordSize) != 0 && (lineIndex + 1) == lineCount)
				realLineSize = segSize % segments[segIndex].recordSize;
		
			unsigned short offset = segments[segIndex].offsetAddr + lineIndex * segments[segIndex].recordSize;
			wsprintf(tempBuf.data(), L":%02X%04X00", realLineSize, offset);// (unsigned short)(segments[segIndex].offsetAddr + lineIndex * segments[segIndex].recordSize));

			std::wstring line(tempBuf.data());

			crc = realLineSize + (unsigned char)(offset) + (unsigned char)(offset >> 8);

			for (int i = 0; i < realLineSize; ++i)
			{				
				unsigned char d = segments[segIndex].data[i + lineIndex * segments[segIndex].recordSize];

				crc += d;	
				wsprintf(tempBuf.data(), L"%02X", d);
				line.append(tempBuf.data());
			}

			wsprintf(tempBuf.data(), L"%02X\r\n", (unsigned char)((~crc) + 1));
			line.append(tempBuf.data());	

			allLines.push_back(line);
		}
	}

	for(const std::wstring & line : allLines)
		outFile.write(line.data(), line.size());

	outFile.close();
	return (int)segments.size();
}
  
int HexFile::GetCleanRawData(const std::wstring & inFile, const std::wstring & tidyFile)
{
	float progress = 0.0f;
	size_t matchedChars = 0;
	std::vector<std::wstring> matchedLines;

	std::shared_future<int> segCount = std::async(std::launch::async, HexFile::GetRegData, inFile, L"^:[A-Fa-f0-9]{6}0[0-5][A-Fa-f0-9]{2,}", std::ref(matchedLines), std::ref(matchedChars), std::ref(progress));

	std::thread updateProgressThread(
			[this, &segCount, &progress]()
		{		
			std::chrono::milliseconds ms(100);
			while(segCount.wait_for(ms) == std::future_status::timeout)
			{
				std::this_thread::sleep_for(ms);
				parseProgress = progress * readToParsingRadio;
			}
		}
	);

	updateProgressThread.join();

	if (segCount.get() > 0)
	{
		std::wstreampos curPos = 0; 

		std::wofstream outFile(tidyFile.c_str(), std::ios::trunc);
		
		for(std::vector<std::wstring>::iterator itor = matchedLines.begin(); itor != matchedLines.end(); ++itor)
		{
			curPos += itor->size();
			parseProgress = readToParsingRadio + (float)((1 - readToParsingRadio) * curPos / matchedChars);

			// skip a segment which no data
			if (itor->at(0) == L':' && (itor+1) != matchedLines.end())
				if (itor->at(8) != L'0' && (itor + 1)->at(8) != L'0')
					continue;

			outFile << *itor << std::endl;
		}

		outFile.close();

		return matchedLines.size();
	}

	return 0;
}

// for limit of count of input parameters on std::async(), 2013-06-04
//int HexFile::GetRegData(const std::wstring & fileName, const std::wstring & regExp, std::vector<std::wstring> & matchedLines, size_t & matchedChars, std::atomic<float> & progress, int groupIndex, bool caseSensitive)
//int HexFile::GetRegData(const std::wstring & fileName, const std::wstring & regExp, std::vector<std::wstring> & matchedLines, size_t & matchedChars, float & progress, int groupIndex)
int HexFile::GetRegData(const std::wstring & fileName, const std::wstring & regExp, std::vector<std::wstring> & matchedLines, size_t & matchedChars, float & progress)
{
	int groupIndex = 0;				/// delete these two line, after compiler support
	bool caseSensitive = false;

	std::wifstream file(fileName.c_str(), std::ios::in);

	if (!file.is_open()) 
		return -1;

	file.seekg(0, std::ifstream::end);
    std::wstreampos curPos =0, fileSize = file.tellg(); 

	file.seekg(0);

	matchedLines.clear();
	matchedChars = 0;
	progress = 0;

	int invalidLines = 0;

	std::wstring line;
	std::wregex regLine(regExp,  caseSensitive ? std::regex_constants::ECMAScript : std::wregex::icase);	
	std::wsmatch what;

	while(!file.eof()) 
	{	   
		std::getline(file, line);	

		curPos = file.tellg();

		progress = (float)(1.0f * curPos / fileSize);

		if (groupIndex == 0)
		{
			if (std::regex_match(line, regLine))
			{
				matchedLines.push_back(line);
				matchedChars += line.size();
			}
		}
		else
			if (std::regex_search(line, what, regLine))
			{
				matchedLines.push_back(what[groupIndex]);
				matchedChars += what[groupIndex].str().size();
			}
			else
				invalidLines++;
	}

	file.close();

	return matchedLines.size();
}

