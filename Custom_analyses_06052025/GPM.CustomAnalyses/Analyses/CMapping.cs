using System;
using System.Threading;
using System.Threading.Tasks;
using static GPM.CustomAnalyses.varGlob;
//using System.Windows.Forms;  
// BindableBase


namespace GPM.CustomAnalyses.Analyses;

class CMapping
{

	// Variables
	// --------------
	public bool bState;

	public int iSize;
	public int iNbElt;
	public int[] iNbStep = new int[3];

	public float[] fNewSampSize = new float[3];
	public float[] fSubLimit = new float[3];

	public float[,,,] fMem;

	public int[,,] iNbAtom;
	public int[,,,] iAtomId;


	// Functions
	// --------------
	public bool bBuildComposition(float[] fMapSize, float fDelocalization, SAtom Atom, Elt elt, bool[] bShowElt)
	{

		int i, j, k;
		int a, b, c;

		int[] iDeltaDeloc = new int[3];
		int[] iDisplayStep = new int[3];
		int[] iBlocId = new int[4];
		int[] iMemCalc = new int[2];
		
		int[,,,] iMem;
		int[,,,] iLocalMem;

		// Init parameters for Composition Map
		iNbElt = elt.iNbElt;

		for (i = 0; i < 3; i++)
		{
			iNbStep[i] = 1 + (int)((Atom.fLimit[i, 1] - Atom.fLimit[i, 0]) / fMapSize[i]);
			fNewSampSize[i] = (0.01f + Atom.fLimit[i, 1] - Atom.fLimit[i, 0]) / iNbStep[i];
			fSubLimit[i] = Atom.fLimit[i, 0];
			fSubLimit[i] -= fNewSampSize[i];
			iNbStep[i] += 2;

			iDeltaDeloc[i] = (int)(3 * fDelocalization / fNewSampSize[i]);
			iDisplayStep[i] = Math.Max(3 * 2 * iDeltaDeloc[i], 1);
		}

		iMemCalc[0] = iNbStep[0];
		iMemCalc[1] = iNbStep[0] * iNbStep[1];


		// Allocate and Init the memory for Composition Map
		iSize = (iNbStep[0] + 2) * (iNbStep[1] + 2) * (iNbStep[2] + 2);
		fMem = new float[(iNbStep[0] + 2), (iNbStep[1] + 2), (iNbStep[2] + 2), 2];
		iMem = new int[(iNbStep[0] + 2), (iNbStep[1] + 2), (iNbStep[2] + 2), 2];
		iLocalMem = new int[(iNbStep[0] + 5), (iNbStep[1] + 5), (iNbStep[2] + 5), 2];


		// Without delocalization
		// ------------------------
		if (iDeltaDeloc[0] == 0)
		{
			for (i = 0; i < Atom.iMemSize; i++)
			{
				for (j = 0; j < 3; j++)
					iBlocId[j] = (int)((Atom.fPos[i, j] - fSubLimit[j]) / fNewSampSize[j]);
					
				for (j = 0; j < 2; j++)
					if (Atom.bEltId[i, j] < iNbElt)
					{
						if (bShowElt[Atom.bEltId[i, j]] == true)
							iMem[iBlocId[0], iBlocId[1], iBlocId[2], j]++;
						iLocalMem[iBlocId[0], iBlocId[1], iBlocId[2], j]++;
					}
			}
		}

		// With delocalization
		// ------------------------
		else
		{
			Parallel.For(0, Atom.iMemSize, ii =>
			{
				int jj;
				int aa, bb, cc;
				int[] iLocalBlocId = new int[4];

				float fX, fY, fZ;
				float fDistance, fLevel;

				for (jj = 0; jj < 3; jj++)
					iLocalBlocId[jj] = (int)((Atom.fPos[ii,jj] - fSubLimit[jj]) / fNewSampSize[jj]);

				for (cc = Math.Max(iLocalBlocId[2] - iDeltaDeloc[2], 0); cc <= Math.Min(iLocalBlocId[2] + iDeltaDeloc[2], iNbStep[2] - 1); cc++)
				for (bb = Math.Max(iLocalBlocId[1] - iDeltaDeloc[1], 0); bb <= Math.Min(iLocalBlocId[1] + iDeltaDeloc[1], iNbStep[1] - 1); bb++)
				for (aa = Math.Max(iLocalBlocId[0] - iDeltaDeloc[0], 0); aa <= Math.Min(iLocalBlocId[0] + iDeltaDeloc[0], iNbStep[0] - 1); aa++)
				{
					fX = (fSubLimit[0] + ((float)aa + 0.5f) * fNewSampSize[0]) - Atom.fPos[ii, 0];
					fY = (fSubLimit[1] + ((float)bb + 0.5f) * fNewSampSize[1]) - Atom.fPos[ii, 1];
					fZ = (fSubLimit[2] + ((float)cc + 0.5f) * fNewSampSize[2]) - Atom.fPos[ii, 2];

					fDistance = fX * fX + fY * fY + fZ * fZ;
					fLevel = (float)(1 * Math.Exp(-fDistance / (2 * fDelocalization * fDelocalization)));
							
					for (jj = 0; jj < 2; jj++)
						if (Atom.bEltId[ii, jj] < elt.iNbElt)
						{
							if (bShowElt[Atom.bEltId[ii, jj]] == true)
								Interlocked.Add(ref iMem[aa, bb, cc, jj], (int)(1000 * fLevel));
							Interlocked.Add(ref iLocalMem[aa, bb, cc, jj], (int)(1000 * fLevel));
						}
				}

			});

		}


		// Calculate the concentration for each sample
		for (c = 0; c < iNbStep[2]; c++)
		for (b = 0; b < iNbStep[1]; b++)
		for (a = 0; a < iNbStep[0]; a++)
		for (i = 0; i < 2; i++)
			fMem[a, b, c, i] = 100 * (float)iMem[a, b, c, i] / (float)Math.Max(iLocalMem[a, b, c, i],1);

		bState = true;


		return true;

	}


	public bool bCalculateAtomComposition(float[] fResult , float fXpos, float fYpos, float fZpos)
	{

		int i, j, k, m;

		float[,] fConcValue = new float[2,2];

		float fDistanceValue;

		float fPtX, fPtY, fPtZ;

		float fX1, fX2, fX3 , fX4;


		// Init parameters
		fResult[0] = fResult[1] = 0;

/*
		// Only for Test
		fPtX = fXpos - fSubLimit[0];
		fPtY = fYpos - fSubLimit[1];
		fPtZ = fZpos - fSubLimit[2];

		i = (int)(fPtX / fNewSampSize[0]);
		j = (int)(fPtY / fNewSampSize[1]);
		k = (int)(fPtZ / fNewSampSize[2]);

		if (i < 0 || i > iNbStep[0] - 1 || j < 0 || j > iNbStep[1] - 1 || k < 0 || k > iNbStep[2] - 1)
			return false;

		fResult[0] = fRealMem[i, j, k, iElementId];
		fResult[1] = fRmdMem[i, j, k, iElementId];
*/

	
		// Init the bloc number
		fPtX = fXpos - fSubLimit[0];
		fPtY = fYpos - fSubLimit[1];
		fPtZ = fZpos - fSubLimit[2];
		//Console.WriteLine(fSubLimit[0] + "  " + fSubLimit[1] + "   " + fSubLimit[2]);

		i = (int)(fPtX / fNewSampSize[0]);
		if (i > 0 && fPtX < ((float)i + 0.5) * fNewSampSize[0])
			i--;

		j = (int)(fPtY / fNewSampSize[1]);
		if (j > 0 && fPtY < ((float)j + 0.5) * fNewSampSize[1])
			j--;

		k = (int)(fPtZ / fNewSampSize[2]);
		if (k > 0 && fPtZ < ((float)k + 0.5) * fNewSampSize[2])
			k--;

		if (i < 0 || i >= iNbStep[0] - 1 || j < 0 || j >= iNbStep[1] - 1 || k < 0 || k >= iNbStep[2] - 1)
			return false;

		// Trilinear interpolation for Real and Random memory
		for (m = k; m <= k + 1; m++)
		{
			// Calculate X components of concentration
			fDistanceValue = ((float)i + 1.5f) * fNewSampSize[0] - fPtX;
			fDistanceValue = (float)Math.Sqrt(fDistanceValue * fDistanceValue);
			fX1 = fDistanceValue * fMem[i, j, m, 0];
			fX2 = fDistanceValue * fMem[i, j + 1, m, 0];
			fX3 = fDistanceValue * fMem[i, j, m, 1];
			fX4 = fDistanceValue * fMem[i, j + 1, m, 1];
			fDistanceValue = ((float)i + 0.5f) * fNewSampSize[0] - fPtX;
			fDistanceValue = (float)Math.Sqrt(fDistanceValue * fDistanceValue);
			fX1 = (fX1 + fDistanceValue * fMem[i + 1, j, m, 0]) / fNewSampSize[0];
			fX2 = (fX2 + fDistanceValue * fMem[i + 1, j + 1, m, 0]) / fNewSampSize[0];
			fX3 = (fX3 + fDistanceValue * fMem[i + 1, j, m, 1]) / fNewSampSize[0];
			fX4 = (fX4 + fDistanceValue * fMem[i + 1, j + 1, m, 1]) / fNewSampSize[0];

			fDistanceValue = ((float)j + 1.5f) * fNewSampSize[1] - fPtY;
			fDistanceValue = (float)Math.Sqrt(fDistanceValue * fDistanceValue);
			fConcValue[m - k, 0] = fDistanceValue * fX1;
			fConcValue[m - k, 1] = fDistanceValue * fX3;

			fDistanceValue = ((float)j + 0.5f) * fNewSampSize[1] - fPtY;
			fDistanceValue = (float)Math.Sqrt(fDistanceValue * fDistanceValue);
			fConcValue[m - k, 0] = (fConcValue[m - k, 0] + fDistanceValue * fX2) / fNewSampSize[1];
			fConcValue[m - k, 1] = (fConcValue[m - k, 1] + fDistanceValue * fX4) / fNewSampSize[1];
		}


		// Calculate the total concentration
		fDistanceValue = ((float)k + 1.5f) * fNewSampSize[2] - fPtZ;
		fDistanceValue = (float)Math.Sqrt(fDistanceValue * fDistanceValue);
		fResult[0] = fDistanceValue * fConcValue[0, 0];
		fResult[1] = fDistanceValue * fConcValue[0, 1];

		fDistanceValue = ((float)k + 0.5f) * fNewSampSize[2] - fPtZ;
		fDistanceValue = (float)Math.Sqrt(fDistanceValue * fDistanceValue);
		fResult[0] = (fResult[0] + fDistanceValue * fConcValue[1, 0]) / fNewSampSize[2];
		fResult[1] = (fResult[1] + fDistanceValue * fConcValue[1, 1]) / fNewSampSize[2];
		

		return true;

	}

	public bool bBuildOptimization(float fAtomMapSize, int iSelectionType, SAtom Atom, bool[] bShowElt)
	{

		int i, j;

		int[] iBlocId = new int[3];

		int iNbAtomMax;


		// Init parameters
		for (i = 0; i < 3; i++)
		{
			iNbStep[i] = 1 + (int)((0.01 + Atom.fLimit[i, 1] - Atom.fLimit[i, 0]) / fAtomMapSize);
			fNewSampSize[i] = fAtomMapSize;
			fSubLimit[i] = Atom.fLimit[i, 0];
		}


		// Init number of atoms per bloc
		iNbAtom = new int[iNbStep[0] + 2, iNbStep[1] + 2, iNbStep[2] + 2];
		Array.Clear(iNbAtom, 0, iNbAtom.Length);

		iNbAtomMax = 0;
		for (i = 0; i < Atom.iMemSize; i++)
		{
			for (j = 0; j < 3; j++)
				iBlocId[j] = (int)((Atom.fPos[i, j] - fSubLimit[j]) / fNewSampSize[j]);

			if ((iSelectionType == 0) || (iSelectionType == 1 && bShowElt[Atom.bEltId[i, 0]] == true)
			                          || (iSelectionType == 2 && bShowElt[Atom.bEltId[i, 1]] == true)
			                          || (iSelectionType == 3 && Atom.iCluId[i] >= 1))
				iNbAtom[iBlocId[0], iBlocId[1], iBlocId[2]]++;

			iNbAtomMax = Math.Max(iNbAtom[iBlocId[0], iBlocId[1], iBlocId[2]], iNbAtomMax);
		}


		// Init Atom Id for each bloc
		iAtomId = new int[iNbStep[0] + 2, iNbStep[1] + 2, iNbStep[2] + 2, iNbAtomMax + 2];
		Array.Clear(iNbAtom, 0, iNbAtom.Length);

		for (i = 0; i < Atom.iMemSize; i++)
		{
			for (j = 0; j < 3; j++)
				iBlocId[j] = (int)((Atom.fPos[i, j] - fSubLimit[j]) / fNewSampSize[j]);

			if ((iSelectionType == 0) || (iSelectionType == 1 && bShowElt[Atom.bEltId[i, 0]] == true)
			                          || (iSelectionType == 2 && bShowElt[Atom.bEltId[i, 1]] == true)
			                          || (iSelectionType == 3 && Atom.iCluId[i] >= 1))
			{
				iAtomId[iBlocId[0], iBlocId[1], iBlocId[2], iNbAtom[iBlocId[0], iBlocId[1], iBlocId[2]]] = i;
				iNbAtom[iBlocId[0], iBlocId[1], iBlocId[2]]++;
			}
		}

		return true;
	}


}
