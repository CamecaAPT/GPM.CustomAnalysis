using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Media;
using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities.Legacy;
using Microsoft.Win32.SafeHandles;


namespace GPM.CustomAnalyses.Analyses.FrequencyDistribution;

internal class GPM_FrequencyDistribution : ICustomAnalysis<GPM_FrequencyDistributionOptions>
{
	public IIonDisplayInfo IonDisplayInfo { get; set; } = RandomColorIonDisplayInfo.Instance;

	// Parameters 
	// -------------
	private int iCalculationId = 0;             // Calculation Id
	private int iSelectedElementId = 1;         // Selected element
	private int iBlocSize = 100;                // Size of the bloc used for calculation
	private int iCalculationType = 0;           // Type of calculation 0 : in atoms / 1 : in %
	private int iTheoricalDst = 0;              // Therical distribution - 0 for Bernouilly - 1 for Poisson - 2 for Gauss
	private int iMinBlocSize = 50;              // Min size of bloc for Thuvander test
	private int iMaxBlocSize = 200;             // Max size of bloc for Thuvander test
	private int iBlocStep = 50;                 // Step increment for Thuvander test



	// Variable declaration
	// ----------------------
	List<float> mass = new List<float>();
	List<Vector3> pos_init = new List<Vector3>();
	List<byte> name = new List<byte>();
	List<Color?> EltColor;

	int Name_Id;
	private Vector3[] exp_dist;
	private Vector3[] rnd_dist;

	CMapping Map3d = new CMapping();
	
	CAtom Atom = CustomAnalysesModule.Atom;

	bool bFilteringState = false;

	int iNbParticuleMax = 50000;

	Stopwatch ExecutionTime = new Stopwatch();



	public void Run(IIonData ionData, GPM_FrequencyDistributionOptions options, IViewBuilder viewBuilder)
	{

		// Conversion US-FR
		// ---------------------
		Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
		Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");


		// Local variables
		// -----------------
		List<string> impact_name;

		bool[] bShowElt;
		bool[] bShowCluster;

		int i, j, k;
		int iTest, iNbAtom;
		int iNbElt, iIndex;

		float fBlocSize;
		float fHistogramResolution = (float)0.1;
		int[,] iHistogram = new int[Convert.ToInt32(102 / fHistogramResolution), 2];


		// Menu parameters
		// -----------------
		iCalculationId = options.CalculationId;
		iSelectedElementId = options.SelectedElementId;
		iBlocSize = options.BlocSize;
		iCalculationType = options.CalculationType;
		iTheoricalDst = options.TheoricalDst;
		iMinBlocSize = options.MinBlocSize;
		iMaxBlocSize = options.MaxBlocSize;
		iBlocStep = options.BlocStep;


		Console.WriteLine("		");


		// Data extraction for GPM
		// -------------------------
		if (Atom.bState == false)
		{
			Console.WriteLine("Create Atom data memory ...");
			Atom.bInitMemory2(ionData, IonDisplayInfo);
		}

		Console.WriteLine("Atom memory size = {0}    NbElement = {1}    NbCluster = {2}\n", Atom.iMemSize, Atom.iNbElt, Atom.iNbCluster);


		// Test the calculation Id
		if (iCalculationId == 0)
		{
			Console.WriteLine("Select a Calculation Id to compute !!");
			return;
		}


		// Frequency distriibution
		// ----------------------------
		if (iCalculationId == 1)
		{
			Console.WriteLine("GPM  -  Frequency distribution 2");
			Console.WriteLine("Selected element = {0}    /    Size of bloc = {1}", Atom.EltName[iSelectedElementId], iBlocSize);
			Console.WriteLine("Type of calculation = {0}    /    Theorical distribution = {1}", iCalculationType, iTheoricalDst);
			ExecutionTime.Restart();


			// XY sampling
			// ---------------
			int iLocalBlocSize = iBlocSize;
			int[] iNbAtomForCompo = new int[2];
			float fSizeValue;
			int[] iNbSubBloc = new int[2];
			float[] fSubSize = new float[2];

			fBlocSize = (float)iLocalBlocSize;
			fSizeValue = Math.Min(Atom.fLimit[0, 1], Atom.fLimit[1, 1]) / 1.5f;
			fSizeValue = Math.Min(fSizeValue, Atom.fLimit[2, 1]);
			fSizeValue = (float)((int)fSizeValue - 2);

			iNbAtomForCompo[0] = iNbAtomForCompo[1] = 0;

			for (i = 0; i < Atom.iMemSize; i++)
				if (Atom.bEltId[i, 0] < Atom.iNbElt)
				{
					iNbAtomForCompo[0]++;

					if (Atom.fPos[i, 0] >= -fSizeValue && Atom.fPos[i, 0] <= fSizeValue)
						if (Atom.fPos[i, 1] >= -fSizeValue && Atom.fPos[i, 1] <= fSizeValue)
							if (Atom.fPos[i, 2] >= -fSizeValue && Atom.fPos[i, 2] <= fSizeValue)
								iNbAtomForCompo[1]++;
				}

			//Console.WriteLine("iNbAtomForCompo[0] = {0}    iNbAtomForCompo[1] = {1}    fSizeValue = {2}", iNbAtomForCompo[0], iNbAtomForCompo[1], fSizeValue);


			fSizeValue *= 2;
			fSizeValue = (float)Math.Pow(iLocalBlocSize * fSizeValue * fSizeValue * fSizeValue / (float)iNbAtomForCompo[1], (float)1 / 3);

			Console.WriteLine("fSizeValue = {0} ", fSizeValue);

			for (i = 0; i < 2; i++)
			{
				iNbSubBloc[i] = 1 + (int)((Atom.fLimit[i, 1] - Atom.fLimit[i, 0]) / fSizeValue);
				fSubSize[i] = (0.01f + Atom.fLimit[i, 1] - Atom.fLimit[i, 0]) / iNbSubBloc[i];
			}

			Console.WriteLine("X -> Nb Bloc = {0}    Size = {1}", iNbSubBloc[0], fSubSize[0]);
			Console.WriteLine("Y -> Nb Bloc = {0}    Size = {1}", iNbSubBloc[1], fSubSize[1]);


			// Prepare Atom Id for fast calculation
			// -----------------------------------------
			int x, y;
			int[,] iNbAtomPerBloc = new int[iNbSubBloc[0] + 2, iNbSubBloc[1] + 2];

			j = 0;

			for (i = 0; i < Atom.iMemSize; i++)
			{
				x = (int)((Atom.fPos[i, 0] - Atom.fLimit[0, 0]) / fSubSize[0]);
				y = (int)((Atom.fPos[i, 1] - Atom.fLimit[1, 0]) / fSubSize[1]);
				iNbAtomPerBloc[x, y]++;
				j = Math.Max(iNbAtomPerBloc[x, y], j);
			}

			j += 2;

			int[,,] iAtomId = new int[iNbSubBloc[0] + 2, iNbSubBloc[1] + 2, j];

			Array.Clear(iNbAtomPerBloc, 0, iNbAtomPerBloc.Length);

			for (i = 0; i < Atom.iMemSize; i++)
			{
				x = (int)((Atom.fPos[i, 0] - Atom.fLimit[0, 0]) / fSubSize[0]);
				y = (int)((Atom.fPos[i, 1] - Atom.fLimit[1, 0]) / fSubSize[1]);
				iAtomId[x, y, iNbAtomPerBloc[x, y]] = i;
				iNbAtomPerBloc[x, y]++;
			}


			// Allocate - Init - Build Real and Random composition profiles
			// ---------------------------------------------------------------
			int iNbBloc = 1 + iNbAtomForCompo[0] / iLocalBlocSize;
			float[,,] fCountAtom = new float[iNbBloc + 2, 2, 2];
			int[] iBlocId = new int[2];


			// Build Real and Random composition profiles
			iBlocId[0] = iBlocId[1] = 0;

			for (y = 0; y < iNbSubBloc[1]; y++)
			for (x = 0; x < iNbSubBloc[0]; x++)
				if (iNbAtomPerBloc[x, y] > 0)
				{
					for (j = 0; j < 2; j++)
					{
						iBlocId[j]++;
						iNbAtomForCompo[j] = 0;

						for (i = 0; i < iNbAtomPerBloc[x, y]; i++)
						{
							if (Atom.bEltId[iAtomId[x, y, i], j] < Atom.iNbElt)
							{
								iNbAtomForCompo[j]++;
								if (iNbAtomForCompo[j] > iLocalBlocSize)
								{
									iBlocId[j]++;
									iNbAtomForCompo[j] = 1;
								}

								if (iBlocId[j] < iNbBloc)
								{
									if (Atom.bEltId[iAtomId[x, y, i], j] == iSelectedElementId)
										fCountAtom[iBlocId[j], j, 0]++;
									fCountAtom[iBlocId[j], j, 1]++;
								}
							}

						}


						// Test the number of atom in the last bloc
						if (iNbAtomForCompo[j] < iLocalBlocSize)
						{
							fCountAtom[iBlocId[j], j, 0] = fCountAtom[iBlocId[j], j, 1] = 0;
							iBlocId[j]--;
						}
					}

				}


			//				Console.WriteLine("Test : Nb blocs = {0}  /  {1}                    ", iBlocId[0], iNbBloc);



			// Build Frequency distribution of real data
			// --------------------------------------------
			float fCompositionValue;
			float fNbSampleforDst;

			int iProfileSize = iLocalBlocSize + 100;
			float[,] fFrequencyDst = new float[3, iProfileSize];

			fNbSampleforDst = 0;

			for (i = 0; i < iNbBloc; i++)
			for (j = 0; j < 2; j++)
				if (fCountAtom[i, j, 1] >= iLocalBlocSize)
				{
					fCompositionValue = fCountAtom[i, j, 0];
					if (iCalculationType == 1)
						fCompositionValue *= 100 / fCountAtom[i, j, 1];
					fFrequencyDst[j, (int)fCompositionValue]++;

					if (j == 0)
						fNbSampleforDst++;
				}


			// Calculate Average and Variance
			// ------------------------------------
			float fAverage, fVariance, fSum;

			fAverage = fVariance = fSum = 0;

			for (i = 0; i < iLocalBlocSize; i++)
				if (fFrequencyDst[0, i] > 0)
				{
					fAverage += fFrequencyDst[0, i] * i;
					fVariance += fFrequencyDst[0, i] * i * i;
					fSum += fFrequencyDst[0, i];
				}

			fAverage /= Math.Max(fSum, 1);
			fVariance = (float)Math.Sqrt((fVariance - fSum * fAverage * fAverage) / fSum);

			//				Console.WriteLine("fAverage = {0}      fVariance = {1}", fAverage, fVariance);


			// Build theorical distribution
			// --------------------------------
			float fNum, fDenom;
			float fP, fQ;
			float fPValue, fQValue;

			switch (iTheoricalDst)
			{
				case 0:

					// Bernouilli
					// -------------
					fP = fAverage / fBlocSize;
					fQ = 1 - fP;
					fNum = fDenom = fPValue = 1;
					fQValue = (float)Math.Pow(fQ, fBlocSize);
					fFrequencyDst[2, 0] = fQValue;

					for (i = 1; i < iLocalBlocSize; i++)
					{
						fNum *= (fBlocSize - i + 1) / 100;
						fDenom *= (float)i / 100;

						fPValue *= fP;
						fQValue /= fQ;

						fFrequencyDst[2, i] = (float)(fPValue * fQValue * fNum / fDenom);
					}


					break;

				case 1:

					// Poisson
					// ------------
					fNum = (float)Math.Exp(-1 * fAverage);
					fDenom = 1;
					fFrequencyDst[2, 0] = fNum;

					for (i = 1; i <= iLocalBlocSize; i++)
					{
						fNum *= fAverage;
						fDenom *= i;
						fFrequencyDst[2, i] = fNum / fDenom;
					}

					break;

				case 2:

					// Gauss
					// ---------
					fP = 1 / (fVariance * (float)Math.Sqrt(2 * Math.PI));
					fDenom = 2 * fVariance * fVariance;

					for (i = 0; i < iLocalBlocSize; i++)
					{
						fNum = (float)i - fAverage;
						fNum *= fNum;
						fFrequencyDst[2, i] = fP * (float)Math.Exp(-fNum / fDenom);
					}

					break;
			}


			for (i = 0; i < iLocalBlocSize; i++)
				fFrequencyDst[2, i] *= fNbSampleforDst;

			//				for (i = 0; i < iBlocSize; i++)
			//					Console.WriteLine("i = {0}      dst1 = {1}    dst2 = {2}    dst3 = {3}", i, fFrequencyDst[0, i], fFrequencyDst[1, i], fFrequencyDst[2, i]);


			// Adjust min / max distribution statistics
			// --------------------------------------------
			int iBlocLim1, iBlocLim2;

			for (j = 0; j < 3; j++)
			{
				iBlocLim1 = 0;
				for (i = iLocalBlocSize - 1; i >= 0; i--)
					if (fFrequencyDst[j, i] >= 5)
						iBlocLim1 = i;

				iBlocLim1 = Math.Max(1, iBlocLim1);

				fNum = 0;
				for (i = 0; i < iBlocLim1; i++)
					fNum += fFrequencyDst[j, i];

				fFrequencyDst[j, iBlocLim1 - 1] = fNum;
				for (i = 0; i < iBlocLim1 - 1; i++)
					fFrequencyDst[j, i] = 0;
			}

			for (j = 0; j < 3; j++)
			{
				iBlocLim2 = 0;
				for (i = 0; i < iLocalBlocSize; i++)
					if (fFrequencyDst[j, i] > fFrequencyDst[j, iBlocLim2])
						iBlocLim2 = i;

				iBlocLim1 = 0;
				for (i = iBlocLim2; i < iLocalBlocSize; i++)
					if (fFrequencyDst[j, i] >= 5)
						iBlocLim1 = i;

				if (iBlocLim1 < iLocalBlocSize)
				{
					fNum = 0;
					for (i = iBlocLim1; i < iLocalBlocSize; i++)
						fNum += fFrequencyDst[j, i];

					fFrequencyDst[j, iBlocLim1] = fNum;
					for (i = iBlocLim1 + 1; i < iLocalBlocSize; i++)
						fFrequencyDst[j, i] = 0;
				}
			}


			// Display frequency distributions
			// ------------------------------------
			float[] xVals = new float[iLocalBlocSize];
			float[] exp_dist = new float[iLocalBlocSize];
			float[] random_dist = new float[iLocalBlocSize];
			float[] theo_dist = new float[iLocalBlocSize];

			string[] str = new string[3];
			str[0] = "Frequency distribution for " + Atom.EltName[iSelectedElementId];
			str[2] = "Number of events";
			if (iCalculationType == 0)
				str[1] = "Number of atoms";
			else
				str[1] = "Composition (ion%)";

			var freqDistChart = viewBuilder.AddChart2D(str[0], str[1], str[2]);

			for (i = 0; i < exp_dist.Length; i++)
			{
				xVals[i] = (float)i;
				exp_dist[i] = fFrequencyDst[0, i];
				random_dist[i] = fFrequencyDst[1, i];
				theo_dist[i] = fFrequencyDst[2, i];
			}

			freqDistChart.AddLine(xVals, exp_dist, Atom.EltColor[iSelectedElementId], "Experimental", 2);
			freqDistChart.AddLine(xVals, random_dist, Colors.Black, "Randomized", 1);
			freqDistChart.AddLine(xVals, theo_dist, Colors.Gray, "Theorical", 1);


			ExecutionTime.Stop();
			Console.WriteLine("Execution time : " + ExecutionTime.Elapsed.TotalSeconds + " s\n");


			return;
		}


		// Thuvander Test
		// ----------------------------
		if (iCalculationId == 2)
		{
			Console.WriteLine("GPM  -  Thuvander Calculation");
			Console.WriteLine("Selected element = {0}    /    Size of bloc = from {1} to {2}", Atom.EltName[iSelectedElementId], iMinBlocSize, iMaxBlocSize);
			Console.WriteLine("Type of calculation = {0}    /    Theorical distribution = {1}", iCalculationType, iTheoricalDst);
			ExecutionTime.Restart();


			// XY sampling
			// ---------------
			int iLocalBlocSize = iBlocSize;
			int[] iNbAtomForCompo = new int[2];
			int[] iNbAtomForCalc = new int[2];
			float fSizeValue0, fSizeValue;
			int[] iNbSubBloc = new int[2];
			float[] fSubSize = new float[2];

			fSizeValue0 = Math.Min(Atom.fLimit[0, 1], Atom.fLimit[1, 1]) / 1.5f;
			fSizeValue0 = Math.Min(fSizeValue0, Atom.fLimit[2, 1]);
			fSizeValue0 = (float)((int)fSizeValue0 - 2);

			iNbAtomForCalc[0] = iNbAtomForCalc[1] = 0;

			for (i = 0; i < Atom.iMemSize; i++)
				if (Atom.bEltId[i, 0] < Atom.iNbElt)
				{
					iNbAtomForCalc[0]++;

					if (Atom.fPos[i, 0] >= -fSizeValue0 && Atom.fPos[i, 0] <= fSizeValue0)
						if (Atom.fPos[i, 1] >= -fSizeValue0 && Atom.fPos[i, 1] <= fSizeValue0)
							if (Atom.fPos[i, 2] >= -fSizeValue0 && Atom.fPos[i, 2] <= fSizeValue0)
								iNbAtomForCalc[1]++;
				}

			//				Console.WriteLine("iNbAtomForCalc[0] = {0}    iNbAtomForCalc[1] = {1}    fSizeValue = {2}", iNbAtomForCalc[0], iNbAtomForCalc[1], fSizeValue);


			// Thuvander parameters
			// -----------------------
			int ii, jj;

			int iThuvCurveSize = 0;
			for (i = iMinBlocSize; i <= iMaxBlocSize; i += iBlocStep)
				iThuvCurveSize++;

			float[,] fThuvMemory = new float[8, iThuvCurveSize];


			for (ii = iMinBlocSize, jj = 0; ii <= iMaxBlocSize; ii += iBlocStep, jj++)
			{
				iLocalBlocSize = ii;
				fBlocSize = (float)iLocalBlocSize;

				fSizeValue = 2 * fSizeValue0;
				fSizeValue = (float)Math.Pow(iLocalBlocSize * fSizeValue * fSizeValue * fSizeValue / (float)iNbAtomForCalc[1], (float)1 / 3);

				for (i = 0; i < 2; i++)
				{
					iNbSubBloc[i] = 1 + (int)((Atom.fLimit[i, 1] - Atom.fLimit[i, 0]) / fSizeValue);
					fSubSize[i] = (0.01f + Atom.fLimit[i, 1] - Atom.fLimit[i, 0]) / iNbSubBloc[i];
				}


				// Prepare Atom Id for fast calculation
				// -----------------------------------------
				int x, y;
				int[,] iNbAtomPerBloc = new int[iNbSubBloc[0] + 2, iNbSubBloc[1] + 2];

				j = 0;

				for (i = 0; i < Atom.iMemSize; i++)
				{
					x = (int)((Atom.fPos[i, 0] - Atom.fLimit[0, 0]) / fSubSize[0]);
					y = (int)((Atom.fPos[i, 1] - Atom.fLimit[1, 0]) / fSubSize[1]);
					iNbAtomPerBloc[x, y]++;
					j = Math.Max(iNbAtomPerBloc[x, y], j);
				}

				j += 2;

				int[,,] iAtomId = new int[iNbSubBloc[0] + 2, iNbSubBloc[1] + 2, j];

				Array.Clear(iNbAtomPerBloc, 0, iNbAtomPerBloc.Length);

				for (i = 0; i < Atom.iMemSize; i++)
				{
					x = (int)((Atom.fPos[i, 0] - Atom.fLimit[0, 0]) / fSubSize[0]);
					y = (int)((Atom.fPos[i, 1] - Atom.fLimit[1, 0]) / fSubSize[1]);
					iAtomId[x, y, iNbAtomPerBloc[x, y]] = i;
					iNbAtomPerBloc[x, y]++;
				}


				// Allocate - Init - Build Real and Random composition profiles
				// ---------------------------------------------------------------
				int iNbBloc = 1 + iNbAtomForCalc[0] / iLocalBlocSize;
				float[,,] fCountAtom = new float[iNbBloc + 2, 2, 2];
				int[] iBlocId = new int[2];


				// Build Real and Random composition profiles
				iBlocId[0] = iBlocId[1] = 0;

				for (y = 0; y < iNbSubBloc[1]; y++)
				for (x = 0; x < iNbSubBloc[0]; x++)
					if (iNbAtomPerBloc[x, y] > 0)
					{
						for (j = 0; j < 2; j++)
						{
							iBlocId[j]++;
							iNbAtomForCompo[j] = 0;

							for (i = 0; i < iNbAtomPerBloc[x, y]; i++)
							{
								if (Atom.bEltId[iAtomId[x, y, i], j] < Atom.iNbElt)
								{
									iNbAtomForCompo[j]++;
									if (iNbAtomForCompo[j] > iLocalBlocSize)
									{
										iBlocId[j]++;
										iNbAtomForCompo[j] = 1;
									}

									if (iBlocId[j] < iNbBloc)
									{
										if (Atom.bEltId[iAtomId[x, y, i], j] == iSelectedElementId)
											fCountAtom[iBlocId[j], j, 0]++;
										fCountAtom[iBlocId[j], j, 1]++;
									}
								}

							}


							// Test the number of atom in the last bloc
							if (iNbAtomForCompo[j] < iLocalBlocSize)
							{
								fCountAtom[iBlocId[j], j, 0] = fCountAtom[iBlocId[j], j, 1] = 0;
								iBlocId[j]--;
							}
						}

					}


				// Build Frequency distribution of real data
				// --------------------------------------------
				float fCompositionValue;
				float fNbSampleforDst;

				int iProfileSize = iLocalBlocSize + 100;
				float[,] fFrequencyDst = new float[3, iProfileSize];

				fNbSampleforDst = 0;

				for (i = 0; i < iNbBloc; i++)
				for (j = 0; j < 2; j++)
					if (fCountAtom[i, j, 1] >= iLocalBlocSize)
					{
						fCompositionValue = fCountAtom[i, j, 0];
						if (iCalculationType == 1)
							fCompositionValue *= 100 / fCountAtom[i, j, 1];
						fFrequencyDst[j, (int)fCompositionValue]++;

						if (j == 0)
							fNbSampleforDst++;
					}


				// Calculate Average and Variance
				// ------------------------------------
				float fAverage, fVariance, fSum;

				fAverage = fVariance = fSum = 0;

				for (i = 0; i < iLocalBlocSize; i++)
					if (fFrequencyDst[0, i] > 0)
					{
						fAverage += fFrequencyDst[0, i] * i;
						fVariance += fFrequencyDst[0, i] * i * i;
						fSum += fFrequencyDst[0, i];
					}

				fAverage /= Math.Max(fSum, 1);
				fVariance = (float)Math.Sqrt((fVariance - fSum * fAverage * fAverage) / fSum);


				// Build theorical distribution
				// --------------------------------
				float fNum, fDenom;
				float fP, fQ;
				float fPValue, fQValue;

				switch (iTheoricalDst)
				{
					case 0:

						// Bernouilli
						// -------------
						fP = fAverage / fBlocSize;
						fQ = 1 - fP;
						fNum = fDenom = fPValue = 1;
						fQValue = (float)Math.Pow(fQ, fBlocSize);
						fFrequencyDst[2, 0] = fQValue;

						for (i = 1; i < iLocalBlocSize; i++)
						{
							fNum *= (fBlocSize - i + 1) / 100;
							fDenom *= (float)i / 100;

							fPValue *= fP;
							fQValue /= fQ;

							fFrequencyDst[2, i] = (float)(fPValue * fQValue * fNum / fDenom);
						}

						break;

					case 1:

						// Poisson
						// ------------
						fNum = (float)Math.Exp(-1 * fAverage);
						fDenom = 1;
						fFrequencyDst[2, 0] = fNum;

						for (i = 1; i <= iLocalBlocSize; i++)
						{
							fNum *= fAverage;
							fDenom *= i;
							fFrequencyDst[2, i] = fNum / fDenom;
						}

						break;

					case 2:

						// Gauss
						// ---------
						fP = 1 / (fVariance * (float)Math.Sqrt(2 * Math.PI));
						fDenom = 2 * fVariance * fVariance;

						for (i = 0; i < iLocalBlocSize; i++)
						{
							fNum = (float)i - fAverage;
							fNum *= fNum;
							fFrequencyDst[2, i] = fP * (float)Math.Exp(-fNum / fDenom);
						}

						break;
				}


				for (i = 0; i < iLocalBlocSize; i++)
					fFrequencyDst[2, i] *= fNbSampleforDst;


				// Adjust min / max distribution statistics
				// --------------------------------------------
				int iBlocLim1, iBlocLim2;

				for (j = 0; j < 3; j++)
				{
					iBlocLim1 = 0;
					for (i = iLocalBlocSize - 1; i >= 0; i--)
						if (fFrequencyDst[j, i] >= 5)
							iBlocLim1 = i;

					iBlocLim1 = Math.Max(1, iBlocLim1);

					fNum = 0;
					for (i = 0; i < iBlocLim1; i++)
						fNum += fFrequencyDst[j, i];

					fFrequencyDst[j, iBlocLim1 - 1] = fNum;
					for (i = 0; i < iBlocLim1 - 1; i++)
						fFrequencyDst[j, i] = 0;
				}

				for (j = 0; j < 3; j++)
				{
					iBlocLim2 = 0;
					for (i = 0; i < iLocalBlocSize; i++)
						if (fFrequencyDst[j, i] > fFrequencyDst[j, iBlocLim2])
							iBlocLim2 = i;

					iBlocLim1 = 0;
					for (i = iBlocLim2; i < iLocalBlocSize; i++)
						if (fFrequencyDst[j, i] >= 5)
							iBlocLim1 = i;

					if (iBlocLim1 < iLocalBlocSize)
					{
						fNum = 0;
						for (i = iBlocLim1; i < iLocalBlocSize; i++)
							fNum += fFrequencyDst[j, i];

						fFrequencyDst[j, iBlocLim1] = fNum;
						for (i = iBlocLim1 + 1; i < iLocalBlocSize; i++)
							fFrequencyDst[j, i] = 0;
					}
				}


				// Thuvander calculation
				float[] fCumulatedValue = new float[3];
				float[,] fLocalConc = new float[3, iLocalBlocSize + 2];
				float[,] fThuvParam = new float[3, 10];

				for (j = 0, fCumulatedValue[0] = 0, fCumulatedValue[1] = 0; j < iLocalBlocSize; j++)
				{
					fCompositionValue = 100 * (float)j / (float)iLocalBlocSize;

					for (i = 0; i < 3; i++)
					{
						fCumulatedValue[i] += fFrequencyDst[i, j];
						fLocalConc[i, j] = fFrequencyDst[i, j] * fCompositionValue;
						fLocalConc[i, iLocalBlocSize] += fLocalConc[i, j];
					}
				}


				for (i = 0; i < 3; i++)
				{
					if (fCumulatedValue[i] > 0)
						fLocalConc[i, iLocalBlocSize] /= fCumulatedValue[i];
					else
						fLocalConc[i, iLocalBlocSize] = 0;
				}


				for (j = 0; j < iLocalBlocSize; j++)
				{
					fCompositionValue = 100 * (float)j / (float)iLocalBlocSize;

					for (i = 0; i < 3; i++)
						fThuvParam[i, 0] += (fCompositionValue - fLocalConc[i, iLocalBlocSize]) * (fCompositionValue - fLocalConc[i, iLocalBlocSize]) * fFrequencyDst[i, j];
				}


				for (i = 0; i < 3; i++)
				{
					if (fCumulatedValue[i] > 1)
						fThuvParam[i, 0] /= (fCumulatedValue[i] - 1);
					else
						fThuvParam[i, 0] = 0;

					if (fLocalConc[i, iLocalBlocSize] > 0 && fLocalConc[i, iLocalBlocSize] < 100)
						fThuvParam[i, 1] = fThuvParam[i, 0] / (fLocalConc[i, iLocalBlocSize] * (100 - fLocalConc[i, iLocalBlocSize]));
					else if (fLocalConc[i, iLocalBlocSize] >= 100)
						fThuvParam[i, 1] = 100;
					else
						fThuvParam[i, 1] = 0;

					if (fLocalConc[i, iLocalBlocSize] < 100)
						fThuvParam[i, 2] = fLocalConc[i, iLocalBlocSize] * (100 - fLocalConc[i, iLocalBlocSize]) / (float)iLocalBlocSize;
					else
						fThuvParam[i, 2] = 100;

					if (fLocalConc[i, iLocalBlocSize] > 0 && fLocalConc[i, iLocalBlocSize] < 100)
						fThuvParam[i, 3] = fThuvParam[i, 2] / (fLocalConc[i, iLocalBlocSize] * (100 - fLocalConc[i, iLocalBlocSize]));
					else if (fLocalConc[i, iLocalBlocSize] >= 100)
						fThuvParam[i, 3] = 100;
					else
						fThuvParam[i, 3] = 0;
				}


				// Update Thuvander curve
				for (i = 0; i < 3; i++)
					fThuvMemory[i, jj] = fThuvParam[i, 0];
				fThuvMemory[3, jj] = fThuvParam[0, 2];

				for (i = 0; i < 3; i++)
					fThuvMemory[4 + i, jj] = fThuvParam[i, 1];
				fThuvMemory[7, jj] = fThuvParam[0, 3];


				Console.WriteLine("Thuvander Curves : Bloc size = {0}    Real_S2 = {1}    PQ/N = {2}", ii, fThuvMemory[0, jj], fThuvMemory[3, jj]);

			}
            

			// Display frequency distributions
			// ------------------------------------
			float[] xVals = new float[iThuvCurveSize];
			float[] exp_dist = new float[iThuvCurveSize];
			float[] random_dist = new float[iThuvCurveSize];
			float[] theo_dist = new float[iThuvCurveSize];
			float[] compare_dst = new float[iThuvCurveSize];

			string[] str = new string[3];
			str[0] = "Standard deviation curve for " + Atom.EltName[iSelectedElementId];
			str[2] = "Relative frequency (%)";
			str[1] = "Bloc size (number of atoms)";

			var freqDistChart1 = viewBuilder.AddChart2D(str[0], str[1], str[2]);

			for (ii = iMinBlocSize, jj = 0; ii <= iMaxBlocSize; ii += iBlocStep, jj++)
			{
				xVals[jj] = (float)ii;
				exp_dist[jj] = fThuvMemory[0, jj];
				random_dist[jj] = fThuvMemory[1, jj];
				theo_dist[jj] = fThuvMemory[2, jj];
				compare_dst[jj] = fThuvMemory[3, jj];
			}

			freqDistChart1.AddLine(xVals, exp_dist, Atom.EltColor[iSelectedElementId], "Experimental", 2);
			freqDistChart1.AddLine(xVals, random_dist, Colors.Green, "Randomized", 1);
			freqDistChart1.AddLine(xVals, theo_dist, Colors.Pink, "Theorical", 1);
			freqDistChart1.AddLine(xVals, compare_dst, Colors.Blue, "PQ/N value", 1);

			str[0] = "Normalized curve (S²/C(1-C)) for " + Atom.EltName[iSelectedElementId];
			str[2] = "Normalized value";
			str[1] = "Bloc size (number of atoms)";

			var freqDistChart2 = viewBuilder.AddChart2D(str[0], str[1], str[2]);

			for (ii = iMinBlocSize, jj = 0; ii <= iMaxBlocSize; ii += iBlocStep, jj++)
			{
				xVals[jj] = (float)ii;
				exp_dist[jj] = fThuvMemory[4, jj];
				random_dist[jj] = fThuvMemory[5, jj];
				theo_dist[jj] = fThuvMemory[6, jj];
				compare_dst[jj] = fThuvMemory[7, jj];
			}

			freqDistChart2.AddLine(xVals, exp_dist, Atom.EltColor[iSelectedElementId], "Experimental", 2);
			freqDistChart2.AddLine(xVals, random_dist, Colors.Green, "Randomized", 1);
			freqDistChart2.AddLine(xVals, theo_dist, Colors.Pink, "Theorical", 1);
			freqDistChart2.AddLine(xVals, compare_dst, Colors.Blue, "PQ/N value", 1);


			ExecutionTime.Stop();
			Console.WriteLine("Execution time : " + ExecutionTime.Elapsed.TotalSeconds + " s\n");
		}
			
	}



	// Functions to extract data
	private void iterator_mass(float m)
	{
		mass.Add(m);
	}

	private void iterator_pos(float x, float y, float z)
	{
		pos_init.Add(new Vector3(x, y, z));
	}

	private void iterator_name(byte nom)
	{
		name.Add(nom);
	}

	// Function Console
	public class ConsoleHelper
	{
		[DllImport("kernel32.dll")]
		public static extern IntPtr GetStdHandle(int nStdHandle);

		/// <summary>
		/// Allocates a new console for current process.
		/// </summary>
		[DllImport("kernel32.dll")]
		public static extern bool AllocConsole();

		/// <summary>
		/// Frees the console.
		/// </summary>
		[DllImport("kernel32.dll")]
		public static extern bool FreeConsole();

		public const int STD_OUTPUT_HANDLE = -11;
		public const int MY_CODE_PAGE = 437;
	}

	// Function Random
	static Random random = new Random();
	public static double GetRandomNumber(int minimum, int maximum)
	{
		return random.NextDouble() * (maximum - minimum) + minimum;
	}

	// Console properties
	public void vPrepareConsole()
	{
		IntPtr stdHandle = ConsoleHelper.GetStdHandle(ConsoleHelper.STD_OUTPUT_HANDLE);
		SafeFileHandle safeFileHandle = new SafeFileHandle(stdHandle, true);
		FileStream fileStream = new FileStream(safeFileHandle, FileAccess.Write);
		Encoding encoding = System.Text.Encoding.GetEncoding(ConsoleHelper.MY_CODE_PAGE);
		StreamWriter standardOutput = new StreamWriter(fileStream, encoding);
		standardOutput.AutoFlush = true;
		Console.SetOut(standardOutput);
	}

	// Grid structure
	public struct Grid_st
	{
		public double N_int;
		public double N_com;
	}

	public struct X_gridst
	{
		public double Exp;
		public double Rnd;
	}

	// Return the greater value of two numbers 
	public static int GetMax(int first, int second)
	{
		if (first > second)
		{
			return first;
		}
		else
		{
			return second;
		}
	}

	// Return the lower value of two numbers 
	public static int GetMin(int first, int second)
	{
		if (first > second)
		{
			return second;
		}
		else
		{
			return first;
		}
	}





}
