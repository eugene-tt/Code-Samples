// CppTest.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#include <vector>
#include <stack>
#include <set>


#pragma region Assignment1
void* OS_malloc(int sz)
{
	// Check the size
	if (sz <= 0)
	{
		throw new std::exception("Zero alloc size");
	}

	if ((sz % 16) != 0)
	{
		throw new std::exception("Invalid alloc size");
	}
	
	return malloc(sz);
}

void OS_free(int sz, void* p)
{
	if (!p)
	{
		throw new std::exception("Null ref exception");
	}

	if ((sz % 16) != 0)
	{
		throw new std::exception("Invalid free size");
	}

	free(p);
}

const int LENGTH_BUF_SIZE = (int)(4 * sizeof(int));

void* _malloc(int sz)
{
	// Check the size
	if (sz <= 0)
	{
		throw new std::exception("Zero alloc size");
	}

	// Round up the size
	sz = (int)ceil((double)sz / 16.0) * 16;

	// Add a bit of space to store the size in front of the buffer
	auto szTotal = sz + LENGTH_BUF_SIZE;
	auto buffer = (char*)OS_malloc(szTotal);
	memset(buffer, 0, szTotal);

	// Copy the size info
	memcpy(buffer, &sz, sizeof(sz));

	// Return the buffer ptr offset by the size block
	return buffer + LENGTH_BUF_SIZE;
}

void _free(void* p)
{
	// Basic checks
	if (!p)
	{
		throw new std::exception("Null ref exception");
	}

	// Read the size info preceding the buffer
	auto realPtr = (char*)p - LENGTH_BUF_SIZE;
	auto sz = *(int*)(realPtr);

	// Call OS Free
	OS_free(sz, realPtr);
}


// the idea is to round up the size to the next x16, also add to it extra 16 bytes for the size and put it in front of the buffer
// Return the allocated buffer offset by that 16 byte prefix with length
// When freein we read preceding 16 bytes, find length there and call OS_free with that length
void Assignment1MallocTask()
{
	std::cout << "-------------------------------------------" << std::endl;
	std::cout << "Assignment1 MallocTask" << std::endl;
	int szVar = 0;
	std::vector<decltype(szVar)> sizes = { 1, 3, 7, 8, 24, 13, 1002 };
	std::vector<void*> pointers;

	std::cout << "Allocations:" << std::endl;
	for (auto&& sz : sizes)
	{
		std::cout << "Allocating " << sz << " bytes..." << std::endl;
		auto memBuf = _malloc(sz);
		pointers.push_back(memBuf);
		std::cout << "Allocated " << *(int*)((char*)memBuf - LENGTH_BUF_SIZE) << " bytes" << std::endl;
	}

	// Free all the pointers
	std::cout << "Deallocations:" << std::endl;
	for (auto&& ptr : pointers)
	{
		std::cout << "Freeing " << *(int*)((char*)ptr - LENGTH_BUF_SIZE) << " bytes..." << std::endl;
		_free(ptr);
	}
	pointers.clear();
}

#pragma endregion

#pragma region Assignment2
// The function would use a stack to push everyopening bracket it finds in the string
// When it encounters a closing bracket it pops the last bracket from stack and makes sure that they match
// At the end we also check to make sure that the stack remains empty, i.e. there were no unmatched brackets left
bool CheckString(const std::string const& expr)
{
	std::stack<char> brackets;
	std::set<char> openingBrackets = { '{', '(', '[', };
	std::set<char> closingBrackets = { '}', ')', ']'};
	std::set<char> arithmeticOps = { '*', '/', '+', '-' };
	std::set<char> digits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
	std::set<char> whitespaces = { ' ', '\t', '\r', '\n'};

	// Build the allowed symbols
	std::set<char> allowedSymbols;
	allowedSymbols.insert(openingBrackets.begin(), openingBrackets.end());
	allowedSymbols.insert(closingBrackets.begin(), closingBrackets.end());
	allowedSymbols.insert(arithmeticOps.begin(), arithmeticOps.end());
	allowedSymbols.insert(digits.begin(), digits.end());
	allowedSymbols.insert(whitespaces.begin(), whitespaces.end());

	// validators	
	auto isValid = [allowedSymbols](char s) { return allowedSymbols.find(s) != allowedSymbols.end(); };
	auto isOpenBracket = [openingBrackets](char s) { return openingBrackets.find(s) != openingBrackets.end(); };
	auto isCloseBracket = [=](char s) { return closingBrackets.find(s) != closingBrackets.end(); };

	if (expr.length() == 0)
	{
		// Empty string is valid
		return true;
	}

	for (auto&& sym : expr)
	{
		if (!isValid(sym))
		{
			return false;
		}

		if (isOpenBracket(sym))
		{
			// Push to stack and continue
			brackets.push(sym);
			continue;
		}

		if (isCloseBracket(sym))
		{
			// Check the stack size
			if (brackets.empty())
			{
				return false;
			}

			// Check the brackets match by type
			auto lastBracket = brackets.top();
			if (
				lastBracket == '{' && sym != '}' ||
				lastBracket == '[' && sym != ']' ||
				lastBracket == '(' && sym != ')'
				)
			{
				return false;
			}

			// Pop it up
			brackets.pop();
			continue;
		}
	}

	// No brackets must remain
	return brackets.empty();
}

void Assignment2Arithmetic()
{
	std::cout << "-------------------------------------------" << std::endl;
	std::cout << Assignment2Arithmetic << std::endl;

	std::vector<std::string> expressions = {
		// valid
		"",
		"     \t\n",
		"2+3  - 4    *89",
		"(1 + 2) -    3",
		"(1 + 2) - { 4/ 5} *  3",
		"[1 + 2] - { 4/ 5} *  3",
		"[1 + {2 + 5}] - { 4/ 5} *  3",
		"[1 + {2 + (1 + 2) -    3}] - { 4/ 5} *  3",
		// invalid
		"})",
		"(1 + 2] -    3",
		"(1 + bsdf] -    3",
		"(1 + 2) - [ 4/ 5} *  3",
		"[1 + 2] - ( 4/ 5} *  3",
		"[1 + {2 + 5} - { 4/ 5]} *  3",
		"[1 + {2 + (1 + 2} -    3}] - { 4/ 5} *  3",
		"[({",
		"[({})"
	};

	for (auto&& expr : expressions)
	{
		std::cout << "Checking: " << expr << std::endl;
		std::cout << "Is Valid: " << CheckString(expr) << std::endl;
	}
}

#pragma endregion

#pragma region Assignment3

void Assignment3Polygon()
{
	// Critical logging:
	// I would use a concurrent lock-free queue container.
	// Log messages are enqueued to the log_queue without any locks (lock-free) and execution continues
	// There a background thread periodically or constantly trying to dequeue the older message from the queue's head and save it to a log file
	// After that it removes the log message item from the queue's head qithout blocking any other thread.
	// In order to avoid lock and in order to ensure that the original order of the log messages is not broken there should always be
	// only one background thread picking queueu messages and saving them to the file (single reader, multiple writers)
	//

	std::cout << "-------------------------------------------" << std::endl;
	std::cout << "Assignment3 Polygon" << std::endl;
	std::cout << "Check code comments" << std::endl;
}

#pragma endregion


#pragma region Assignment4

void Assignment4Log()
{
/*

1. Read all the sides and save the corner points to a map -> {Vector<point> connected_points}
2. Every time we meet the same point we add the second point to the connected_points
3. Check that all points have exactly count=2 in connected_points
4. Check that there are at least 3 points in the map
5. We start with any point and take the first point from the connected_points array from the map record 
6. We write the edge [THIS_POINT, connected_points[0]]
7. We remove the just taken corner from THIS_POINT's connected_points array
7. We take the the connected_points[0] that we just used and try to find it in the map, if it's not there then error
8. We repeat step 6
9. If THIS_POINT's connected_points record has no more items at any stage we remove this record from the points map
10. At the end the map of corners must remain empty or error
*/

	std::cout << "-------------------------------------------" << std::endl;
	std::cout << "Assignment4 Log" << std::endl;
	std::cout << "Check code comments" << std::endl;
}
 
#pragma endregion

int main()
{
	Assignment1MallocTask();
	Assignment2Arithmetic();
	Assignment3Polygon();
	Assignment4Log();

    std::cout << "Hello World!\n";
}
