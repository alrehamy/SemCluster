#include "stdafx.h"
#include <cstdio>
#include <cstdlib>
#include <cmath>
#include <vector>
#include <algorithm>
#include <cassert>
#include "AffinityPropagation.h"
using namespace std;
namespace {
	struct Edge {
		int src;      // index of source
		int dst;      // index of destination
		double s;     // similarity s(src, dst)
		double r;     // responsibility r(src, dst)
		double a;     // availability a(src, dst)

		Edge(int src, int dst, double s): src(src), dst(dst), s(s), r(0), a(0) {}
		bool operator<(const Edge& rhs) const { return s < rhs.s; }
	};
	typedef vector<Edge*> Edges;
	struct Graph {
		int n;
		Edges* outEdges;
		Edges* inEdges;
		vector<Edge> edges;
	};
	Graph* ConstructGraph(array<double>^ S,int S_d, int prefType)
	{
		Graph* graph = new Graph;
		graph->n =  S_d ;

		graph->outEdges = new Edges[graph->n];
		graph->inEdges = new Edges[graph->n];
		vector<Edge>& edges = graph->edges;

		int h=0;
		for(int i=0;i<S_d;i++)
			for(int j=i+1;j<S_d;j++)
			{
				edges.push_back(Edge(i, j, S[h]));
				h++;
			}

		double pref;

		if (prefType == 1) {
			sort(edges.begin(), edges.end());
			int m = edges.size();
			pref = (m % 2) ? edges[m/2].s : (edges[m/2 - 1].s + edges[m/2].s) / 2.0;
		} else 
			if (prefType == 2) {
			pref = min_element(edges.begin(), edges.end())->s;
		} else 
			if (prefType == 3) {
			double minValue = min_element(edges.begin(), edges.end())->s;
			double maxValue = max_element(edges.begin(), edges.end())->s;
			pref = 2*minValue - maxValue;
		} 

		for (int i = 0; i < graph->n; ++i) {
			edges.push_back(Edge(i, i, pref));
		}

		for (size_t i = 0; i < edges.size(); ++i) {
			Edge* p = &edges[i];
			p->s += (1e-16 * p->s + 1e-300) * (rand() / (RAND_MAX + 1.0));
			graph->outEdges[p->src].push_back(p);
			graph->inEdges[p->dst].push_back(p);
		}

		return graph;
	}
	void DisposeGraph(Graph* graph)
	{
		delete [] graph->outEdges;
		delete [] graph->inEdges;
		delete graph;
	}
	inline void update(double& variable, double newValue, double damping)
	{
		variable = damping * variable + (1.0 - damping) * newValue;
	}
	void updateResponsibilities(Graph* graph, double damping)
	{
		for (int i = 0; i < graph->n; ++i) {
			Edges& edges = graph->outEdges[i];
			int m = edges.size();
			double max1 = -HUGE_VAL, max2 = -HUGE_VAL;
			double argmax1 = -1;
			for (int k = 0; k < m; ++k) {
				double value = edges[k]->s + edges[k]->a;
				if (value > max1) { swap(max1, value); argmax1 = k; }
				if (value > max2) { max2 = value; }
			}
			for (int k = 0; k < m; ++k) {
				if (k != argmax1) {
					update(edges[k]->r, edges[k]->s - max1, damping);
				} else {
					update(edges[k]->r, edges[k]->s - max2, damping);
				}
			}
		}
	}
	void updateAvailabilities(Graph* graph, double damping)
	{
		for (int k = 0; k < graph->n; ++k) {
			Edges& edges = graph->inEdges[k];
			int m = edges.size();
			double sum = 0.0;
			for (int i = 0; i < m-1; ++i) {
				sum += max(0.0, edges[i]->r);
			}
			double rkk = edges[m-1]->r;
			for (int i = 0; i < m-1; ++i) {
				update(edges[i]->a, min(0.0, rkk + sum - max(0.0, edges[i]->r)), damping);
			}
			update(edges[m-1]->a, sum, damping);
		}
	}
	bool updateExamplars(Graph* graph, int* examplar)
	{
		bool changed = false;
		for (int i = 0; i < graph->n; ++i) {
			Edges& edges = graph->outEdges[i];
			int m = edges.size();
			double maxValue = -HUGE_VAL;
			int argmax = i;
			for (int k = 0; k < m; ++k) {
				double value = edges[k]->a + edges[k]->r;
				if (value > maxValue) {
					maxValue = value;
					argmax = edges[k]->dst;
				}
			}
			if (examplar[i] != argmax) {
				examplar[i] = argmax;
				changed = true;
			}
		}
		return changed;
	}
}
AffinityPropagation::AffinityPropagation(void)
{
}
AffinityPropagation::~AffinityPropagation(void)
{
}
array<int>^ AffinityPropagation::Run(array<double>^ S,int S_d, int prefType, double damping, int maxit, int convit)
{
	Graph* graph = ConstructGraph(S,S_d, prefType);
	array<int>^ ret_examplar= gcnew array<int>(S_d);
	pin_ptr<int> p = &ret_examplar[0];
   int* examplar = p;
	for (int i = 0, nochange = 0; i < maxit && nochange < convit; ++i, ++nochange) {
		updateResponsibilities(graph, damping);
		updateAvailabilities(graph, damping);
		if (updateExamplars(graph, examplar)) { nochange = 0; }
	}
	DisposeGraph(graph);
	return ret_examplar;
}