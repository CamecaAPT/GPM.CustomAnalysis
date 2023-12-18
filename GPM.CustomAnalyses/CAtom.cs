using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows.Media;
using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;

//using System.Windows.Forms;  
// BindableBase


namespace GPM.CustomAnalyses;

public class CAtom
{
	public Color[] tabColor = { Colors.Blue, Colors.Red, Colors.Green, Colors.Yellow, Colors.Pink, Colors.Brown, Colors.Gray, Colors.Orange, Colors.Purple, Colors.Aqua};
	// Variables
	// --------------
	public bool bState;

	public int iMemSize;
	public int iNbElt;
	public int iNbCluster;
	public float[,] fLimit = new float[3, 2];

	public string[] EltName;
	public Color[] EltColor;

	public byte[,] bEltId;
	public float[,] fPos;
	public float[,] fMass;
	public int[] iCluId;
	public Int32[] iMultipleEvent;
	public float[] fVDC;
	public float[,] fDPos;

	public bool[,] bFamilyId;

	private List<float> mass = new List<float>();
	private List<Vector3> pos_init = new List<Vector3>();
	private List<byte> name = new List<byte>();
	private List<string> impact_name;
	private List<Int32> multiplicity = new List<Int32>();
	private List<float> vdc = new List<float>();
	private List<Vector2> pos_det = new List<Vector2>();
	
	
	// Functions
	// --------------
	public bool bInitMemory(int iSize, int iNumberOfElement)
	{
		int i;

		iMemSize = iSize;
		iNbElt = iNumberOfElement;
		iNbCluster = 0;

		fPos = new float[iMemSize, 3];
		fMass = new float[iMemSize, 2];
		iCluId = new int[iMemSize];
		bEltId = new byte[iMemSize, 2];
		iMultipleEvent = new Int32[iMemSize];

		for (i = 0; i < 3; i++)
		{
			fLimit[i, 0] = 900000;
			fLimit[i, 1] = -900000;
		}


		return true;
	}

	public bool bInitMemory2(IIonData LocalIonData, IIonDisplayInfo ionDisplayInfo)
	{
		int i, j;
		mass.Clear();
		pos_init.Clear();
		name.Clear();
		multiplicity.Clear();
		vdc.Clear();
		pos_det.Clear();

		// Extract ApSuite data
		foreach (var chunk in LocalIonData.CreateSectionDataEnumerable(IonDataSectionName.Mass, IonDataSectionName.Position, IonDataSectionName.IonType, IonDataSectionName.Multiplicity, IonDataSectionName.Voltage, IonDataSectionName.DetectorCoordinates))
		{
			var masses = chunk.ReadSectionData<float>(IonDataSectionName.Mass);
			var positions = chunk.ReadSectionData<Vector3>(IonDataSectionName.Position);
			var ionTypes = chunk.ReadSectionData<byte>(IonDataSectionName.IonType);
			var detector_position = chunk.ReadSectionData<Vector2>(IonDataSectionName.DetectorCoordinates);
			var voltage = chunk.ReadSectionData<float>(IonDataSectionName.Voltage);
			var multipleEvent = chunk.ReadSectionData<Int32>(IonDataSectionName.Multiplicity);
			for (int index = 0; index < chunk.Length; index++)
			{
				mass.Add(masses.Span[index]);
				pos_init.Add(positions.Span[index]);
				name.Add(ionTypes.Span[index]);
				pos_det.Add(detector_position.Span[index]);
				vdc.Add(voltage.Span[index]);
				multiplicity.Add(multipleEvent.Span[index]);
			}
		}
		Console.WriteLine("Multi :" + multiplicity.Count + "  "+ multiplicity[0]);
		iMemSize = Math.Min(pos_init.Count, mass.Count);
		iMemSize = Math.Min(iMemSize, name.Count);

		//Extract Range file information
		//string rrngFile = @"d:\\Documents\\Desktop\\Travail\\Labo\\APsuite\\Experiment files\\test1.RRNG";
		//bRangeFileAccess(false, rrngFile);
		bUpdateElt(LocalIonData, ionDisplayInfo);

		// Allocate and Init Atom memory
		iNbCluster = 0;
		fPos = new float[iMemSize, 3];
		fDPos = new float[iMemSize, 2];
		fMass = new float[iMemSize, 2];
		iCluId = new int[iMemSize];
		bEltId = new byte[iMemSize, 2];
		iMultipleEvent = new Int32[iMemSize];
		fVDC = new float[iMemSize];
		fDPos = new float[iMemSize, 2];
		bFamilyId = new bool[iMemSize, 6];

		for (i = 0; i < 3; i++)
		{
			fLimit[i, 0] = 900000;
			fLimit[i, 1] = -900000;
		}

		for (i = 0; i < iMemSize; i++)
		{
			fPos[i, 0] = pos_init[i].X;
			fPos[i, 1] = pos_init[i].Y;
			fPos[i, 2] = pos_init[i].Z;
			fDPos[i,0]= pos_det[i].X;
			fDPos[i,1] = pos_det[i].Y;
			fMass[i, 0] = mass[i];
			bEltId[i, 0] = name[i];
			iMultipleEvent[i] = multiplicity[i];
			fVDC[i] = vdc[i];

			for (j = 0; j < 3; j++)
			{
				fLimit[j, 0] = Math.Min(fLimit[j, 0], fPos[i, j]);
				fLimit[j, 1] = Math.Max(fLimit[j, 1], fPos[i, j]);
			}

			iCluId[i] = 0;
			iNbCluster = Math.Max(iCluId[i], iNbCluster);

			bEltId[i, 0] = name[i];
			j = (int)GetRandomNumber(0, iMemSize - 1);

			fMass[j, 1] = fMass[i, 0];
			fMass[i, 1] = fMass[j, 0];

			bEltId[j, 1] = name[i];
			bEltId[i, 1] = name[j];
			for (int k = 0; k < 6; k++)
			{
				bFamilyId[i, k] = false;
			}
		}

		for (i = 0; i < iMemSize; i++)
		{
			fPos[i, 0] -= (fLimit[0, 1] + fLimit[0, 0]) / 2;
			fPos[i, 1] -= (fLimit[1, 1] + fLimit[1, 0]) / 2;
			fPos[i, 2] -= fLimit[2, 0] + (fLimit[2, 1] - fLimit[2, 0]) / 2;
		}

		for (j = 0; j < 2; j++)
		{
			fLimit[j, 0] -= (fLimit[j, 1] + fLimit[j, 0]) / 2;
			fLimit[j, 1] -= (fLimit[j, 1] + fLimit[j, 0]) / 2;
		}

		float fValue = fLimit[2, 0] + (fLimit[2, 1] - fLimit[2, 0]) / 2;
		fLimit[2, 0] -= fValue;
		fLimit[2, 1] -= fValue;

		bState = true;

		Console.WriteLine("OK");
		return true;
	}

	public bool bUpdateElt(IIonData LocalIonData, IIonDisplayInfo ionDisplayInfo)
	{
		EltName = LocalIonData.Ions.Select(o => o.Name).ToArray();
		EltColor = LocalIonData.Ions.Select(x => ionDisplayInfo.GetColor(x)).ToArray();
		iNbElt = EltName.Length;

		bEltId = new byte[iMemSize, 2];
		name.Clear();
		// Extract ApSuite data
		foreach (var chunk in LocalIonData.CreateSectionDataEnumerable( IonDataSectionName.IonType))
		{
			var ionTypes = chunk.ReadSectionData<byte>(IonDataSectionName.IonType);
			for (int index = 0; index < chunk.Length; index++)
			{
				name.Add(ionTypes.Span[index]);
			}
		}
		for (int i = 0; i < iMemSize; i++)
		{
			bEltId[i, 0] = name[i];
			int j = (int)GetRandomNumber(0, iMemSize - 1);
			bEltId[j, 1] = name[i];
			bEltId[i, 1] = name[j];
		}
		return true;
	}


	public bool bAtomFileAccess(bool bWrite, string file)
	{
		int i, j;
		float fValue;
		uint ideb1, ideb2;


		// Save data in Atom file
		// ------------------------
		if (bWrite == true)
		{
			ideb1 = 0;
			ideb2 = 3;

			Console.WriteLine("Save data in {0} ...", file);

			using (BinaryWriter writer = new BinaryWriter(File.Open(file, FileMode.Create)))
			{
				writer.Write(ideb1);
				writer.Write(ideb2);
				for (i = 0; i < iMemSize; i++)
				{
					writer.Write((float)(fPos[i, 0] * 10));                 //x
					writer.Write((float)(fPos[i, 1] * 10));     //y
					writer.Write((float)(fPos[i, 2] * 10));     //z
					writer.Write(fMass[i, 0]);            //m
					writer.Write((float)iCluId[i]);                  //atom.id);//clusterid
					writer.Write((float)1);                   //nbPulse
					writer.Write((float)0);           //vdc
					writer.Write((float)0);                   //tof
					writer.Write((float)0);           // + ((tDet * 1e2) / 2)))/3); //Px
					writer.Write((float)0);           // + ((tDet * 1e2) / 2)))/3); //Py
					writer.Write((float)0);                   //ampli
					writer.Write((float)0);
					writer.Write((float)0);
					writer.Write((float)0);

				}

				Console.WriteLine("Save data in {0} : OK", file);

				return true;
			}

		}

		// Read data from Atom file
		// ------------------------
		Console.WriteLine("Read data from file {0} ...", file);

		FileInfo Info = new FileInfo(file);
		iMemSize = (int)(Info.Length - 2 * 4) / (14 * 4);

		using (BinaryReader reader = new BinaryReader(File.Open(file, FileMode.Open)))
		{
			ideb1 = reader.ReadUInt32();
			ideb2 = reader.ReadUInt32();

			for (i = 0; i < iMemSize; i++)
			{
				for (j = 0; j < 3; j++)
				{
					fPos[i, j] = reader.ReadSingle() / 10;
					fLimit[j, 0] = Math.Min(fPos[i, j], fLimit[j, 0]);
					fLimit[j, 1] = Math.Max(fPos[i, j], fLimit[j, 1]);
				}

				fMass[i, 0] = reader.ReadSingle();

				iCluId[i] = (int)reader.ReadSingle();

				for (j = 0; j < 9; j++)
					fValue = reader.ReadSingle();
			}

			Console.WriteLine("Read data from file {0} : OK", file);

			return true;
		}

	}


	public bool bRangeFileAccess(bool bWrite, string file)
	{

		// Save new Range file
		if (bWrite == true)
		{
			return true;
		}


		// Read selected Range file
		int iNbItv;

		string[] sSymbol;
		float[,] fEltInfo;
		int[] iNumElt;
		float[,] fItvInfo;

		string sToken, sRestOfText, sTest;
		int iStart, iLength;

		// Get the text inside the file
		string sFileText = File.ReadAllText(file);

		// Change '.' by ','
		sFileText = sFileText.Replace('.', ',');

		// Test the header
		iStart = 0;
		iLength = sFileText.IndexOf("\n");
		sToken = sFileText.Substring(iStart, iLength);
		sRestOfText = sFileText.Substring(iLength + 1);


		// Number of element
		iStart = 1 + sRestOfText.IndexOf("="); ;
		iLength = sRestOfText.IndexOf("\n") - 1;
		sToken = sRestOfText.Substring(iStart, iLength - iStart);
		sRestOfText = sRestOfText.Substring(iLength + 1);
		iNbElt = Convert.ToInt32(sToken);
		//          FileDebug.WriteLine ( "1 : iNbElt = " + iNbElt ) ;

		// Number of interval
		iStart = sFileText.IndexOf("[Ranges]");
		sRestOfText = sFileText.Substring(iStart + 10);
		iStart = 1 + sRestOfText.IndexOf("="); ;
		iLength = sRestOfText.IndexOf("\n") - 1;
		sToken = sRestOfText.Substring(iStart, iLength - iStart);
		sRestOfText = sRestOfText.Substring(iLength + 1);
		iNbItv = Convert.ToInt32(sToken);
		//          FileDebug.WriteLine ( "2 : iNbItv = " + iNbItv ) ;

		// Allocate memory
		sSymbol = new string[iNbItv];
		fEltInfo = new float[iNbItv, 4];
		iNumElt = new int[iNbItv];
		fItvInfo = new float[iNbItv, 5];

		// Element information
		iStart = sFileText.IndexOf("Ion1");
		sRestOfText = sFileText.Substring(iStart);

		for (int i = 0; i < iNbElt; i++)
		{
			iStart = 1 + sRestOfText.IndexOf("="); ;
			iLength = sRestOfText.IndexOf("\n") - 1;
			sSymbol[i] = sRestOfText.Substring(iStart, iLength - iStart);
			sRestOfText = sRestOfText.Substring(iLength + 2);
		}

		// Interval information
		iStart = sFileText.IndexOf("Range1");
		iLength = 0;
		sRestOfText = sFileText.Substring(iStart);

		for (int j = 0; j < iNbItv; j++)
		{
			// Min limit
			iStart = iLength;
			iLength = sRestOfText.IndexOf(" ");
			sToken = sRestOfText.Substring(iStart, iLength);
			sRestOfText = sRestOfText.Substring(iLength + 1);
			iStart = sToken.IndexOf("=");
			sToken = sToken.Substring(iStart + 1);
			fItvInfo[j, 0] = Convert.ToSingle(sToken);

			// Max limit
			iStart = 0;
			iLength = sRestOfText.IndexOf(" ");
			sToken = sRestOfText.Substring(iStart, iLength);
			sRestOfText = sRestOfText.Substring(iLength + 1);
			fItvInfo[j, 1] = Convert.ToSingle(sToken);

			// Volume
			iStart = 0;
			iLength = sRestOfText.IndexOf(" ");
			sToken = sRestOfText.Substring(iStart, iLength);
			sRestOfText = sRestOfText.Substring(iLength + 1);
			iStart = sToken.IndexOf(":");
			sToken = sToken.Substring(iStart + 1);
			fItvInfo[j, 2] = Convert.ToSingle(sToken);

			// Element Id
			iStart = 0;
			iLength = sRestOfText.IndexOf(" ");
			sToken = sRestOfText.Substring(iStart, iLength);
			sRestOfText = sRestOfText.Substring(iLength + 1);
			if (sToken.IndexOf("Name") == -1)
			{
				iStart = 0;
				iLength = sToken.IndexOf(":");
				sToken = sToken.Substring(iStart, iLength);
				fItvInfo[j, 3] = 1;
			}

			else
			{
				iStart = sToken.IndexOf(":");
				sToken = sToken.Substring(iStart + 1);
				fItvInfo[j, 3] = 1;
			}

			for (int i = 0; i < iNbElt; i++)
				if (string.Equals(sToken, sSymbol[i]) == true)
				{
					iNumElt[j] = i;
					break;
				}

			// Color
			iStart = 0;
			iLength = sRestOfText.IndexOf("\r");
			sToken = sRestOfText.Substring(iStart, iLength);
			sRestOfText = sRestOfText.Substring(iLength + 1);
			iStart = sToken.IndexOf(":");
			sToken = sToken.Substring(iStart + 1);

			for (int i = 0; i < 3; i++)
			{
				sTest = sToken.Substring(2 * i, 2);
				fEltInfo[iNumElt[j], i] = uint.Parse(sTest, System.Globalization.NumberStyles.AllowHexSpecifier);
			}

			iLength = 1 + sRestOfText.IndexOf("\n");
		}

		EltColor = new Color[iNbElt];
		EltName = new string[iNbElt];

		for (int i = 0; i < iNbElt; i++)
		{
			EltName[i] = sSymbol[i];
			EltColor[i].R = (byte)fEltInfo[i, 0];
			EltColor[i].G = (byte)fEltInfo[i, 1];
			EltColor[i].B = (byte)fEltInfo[i, 2];
			EltColor[i].A = 255;
		}


		return true;
	}


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


	static Random random = new Random();
	public static double GetRandomNumber(int minimum, int maximum)
	{
		return random.NextDouble() * (maximum - minimum) + minimum;
	}

}
