#pragma once
using namespace System;
public ref class AffinityPropagationClustering {
public:
	AffinityPropagation *ptrUClass;
	AffinityPropagationClustering() : ptrUClass(new AffinityPropagation()) {};

	// Class exposed functions
	array<int>^ Run(array<double>^ S,int S_d, int prefType, double damping, int maxit, int convit) { return ptrUClass->Run(S,S_d,prefType,damping,maxit,convit); };
};