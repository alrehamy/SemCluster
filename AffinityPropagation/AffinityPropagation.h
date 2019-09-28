#pragma once
#include <vector>
using namespace System;
class AffinityPropagation
{
public:
	AffinityPropagation(void);
	~AffinityPropagation(void);
	array<int>^ Run(array<double>^ S,int S_d, int prefType, double damping, int maxit, int convit);
};
array<int>^ Run(
    array<double>^ S,
	int S_d =0,
    int prefType = 1,
    double damping = 0.9,
    int maxit = 1000,
    int convit = 50
);