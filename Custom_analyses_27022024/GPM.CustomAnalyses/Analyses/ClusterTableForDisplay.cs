using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace GPM.CustomAnalyses.Analyses;

internal class ClusterTableForDisplay
{

	public int ClusterId { get; private set; }
	public int NbAtomForCompo { get; private set; }
	public int NbAtom_Noise { get; private set; }
	public int[] NbAtom { get; private set; }
	public float[] Composition { get; private set; }

	public float LgX { get; private set; }
	public float LgY { get; private set; }
	public float LgZ { get; private set; }
	public float LgTotal { get; private set; }
	public float Rg { get; private set; }

	public float X0 { get; private set; }
	public float Y0 { get; private set; }
	public float Z0 { get; private set; }

	public ClusterTableForDisplay(int iClusterId, int iTotalNbAtom, int iNbAtom_Noise, int[] iNbAtom, float[] fCompo)
	{
		ClusterId = iClusterId;
		NbAtomForCompo = iTotalNbAtom;
		NbAtom_Noise = iNbAtom_Noise;

		NbAtom = new int[iNbAtom.Length];
		Composition = new float[fCompo.Length];
		for(int i=0; i<NbAtom.Length; i++)
		{
			NbAtom[i] = iNbAtom[i];
			Composition[i] = fCompo[i];	
		}
	}

	public ClusterTableForDisplay(int iClusterId, int iTotalNbAtom, int iNbAtom_Noise, int[] iNbAtom, float[] fCompo, bool[] isVisible)
	{

		ClusterId = iClusterId;
		NbAtomForCompo = iTotalNbAtom;
		NbAtom_Noise = iNbAtom_Noise;

		NbAtom = new int[iNbAtom.Length];
		Composition = new float[fCompo.Length];
		for (int i = 0; i < NbAtom.Length; i++)
		{
			NbAtom[i] = iNbAtom[i];
			Composition[i] = fCompo[i];
		}
	}

	public ClusterTableForDisplay(int iClusterId, int iTotalNbAtom, int iNbAtom_Noise, int[] iNbAtom, float[] fCompo, float[] fInfo)
	{
		ClusterId = iClusterId;
		NbAtomForCompo = iTotalNbAtom;
		NbAtom_Noise = iNbAtom_Noise;

		NbAtom = new int[iNbAtom.Length];
		Composition = new float[fCompo.Length];
		for (int i = 0; i < NbAtom.Length; i++)
		{
			NbAtom[i] = iNbAtom[i];
			Composition[i] = fCompo[i];
		}

		LgX = fInfo[0];
		LgY = fInfo[1];
		LgZ = fInfo[2];
		LgTotal = fInfo[3];
		Rg = fInfo[4];

		X0 = fInfo[5];
		Y0 = fInfo[6];
		Z0 = fInfo[7];
	}
}
