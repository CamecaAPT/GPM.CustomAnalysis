using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Linq;
using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using static GPM.CustomAnalyses.varGlob;

namespace GPM.CustomAnalyses;
internal class fctGlob
{
	public static async Task<List<T>> LoadAtoMemory<T>(IIonData LocalIonData, string field , int memSize = 0 ) where T : unmanaged
	{ 
		// Extract ApSuite data
		string[] requiredSections = new string[] {field};
		bool allSectionsAvailable = await LocalIonData.EnsureSectionsAvailable(requiredSections, null);
		if (allSectionsAvailable)
		{
			Console.WriteLine(field + " Load : OK");
			return LocalIonData.ReadSectionToArray<T>(field).ToList();
		}
		else
		{
			T[] newField = new T[memSize];
			LocalIonData.AddSectionIfNotExists<T>(AddSectionContext.CreateOneToOne<T>(field));
			LocalIonData.WriteSection(field, newField);
			Console.WriteLine(field + " New field created");
			return newField.ToList();
		}
	}

	public static Elt LoadEltMemory(IIonData LocalIonData, IIonDisplayInfo ionDisplayInfo)
	{
		Elt elt = new Elt();
		elt.Name = LocalIonData.Ions.Select(o => o.Name).ToList();
		elt.Color = LocalIonData.Ions.Select(x => ionDisplayInfo.GetColor(x)).ToList();
		elt.iNbElt = elt.Name.Count;

		return elt;
	}

	public static void RandomizeElt(ref byte[,] eltId, List<byte> ionType, int memSize)
	{
		for (int i = 0; i < memSize; i++)
		{
			int j = (int)GetRandomNumber(0, memSize - 1);
			eltId[j, 1] = ionType[i];
			eltId[i, 1] = ionType[j];
		}
	}

	public static void RandomizeMass(ref float[,] mass, int memSize)
	{
		for (int i = 0; i < memSize; i++)
		{
			int j = (int)GetRandomNumber(0, memSize - 1);
			mass[j, 1] = mass[i, 0];
			mass[i, 1] = mass[j, 0];
		}
	}

	public static void AllocateMemory(ref SAtom Atom)
	{
		// Allocate and Init Atom memory
		Atom.fPos = new float[Atom.iMemSize, 3];
		Atom.fDPos = new float[Atom.iMemSize, 2];
		Atom.fMass = new float[Atom.iMemSize, 2];
		Atom.iCluId = new int[Atom.iMemSize];
		Atom.bEltId = new byte[Atom.iMemSize, 2];
		Atom.iMultipleEvent = new Int32[Atom.iMemSize];
		Atom.fVDC = new float[Atom.iMemSize];
		Atom.fDPos = new float[Atom.iMemSize, 2];
		Atom.iFamilyId = new int[Atom.iMemSize];
	}

	public static void FillField<T>(ref T[] tab, List<T> lst)
	{
		for (int i = 0; i < lst.Count; i++)
		{
			tab[i] = lst[i];
		}
	}

	public static void FillField<T>(ref T[,] tab, List<T> lst, int column)
	{
		for (int i = 0; i < lst.Count; i++)
		{
			tab[i,column] = lst[i];
		}
	}

	public static float[,] CalculLimit( List<Vector3> pos)
	{
		float[,] result = new float[3,2];
		for (int i = 0; i < 3; i++)
		{
			result[i, 0] = 900000;
			result[i, 1] = -900000;
		}

		result[0, 0] = pos.Min(vec => vec.X);
		result[0, 1] = pos.Max(vec => vec.X);
		result[1, 0] = pos.Min(vec => vec.Y);
		result[1, 1] = pos.Max(vec => vec.Y);
		result[2, 0] = pos.Min(vec => vec.Z);
		result[2, 1] = pos.Max(vec => vec.Z);

		return result;
	} 

	public static float[,] Vec3toArray(Vector3[] data, int memSize)
	{
		float[,] result = new float[memSize, 3];

		for (int i = 0; i < memSize; i++)
		{
			result[i, 0] = data[i].X;
			result[i, 1] = data[i].Y;
			result[i, 2] = data[i].Z;
		}

		return result;
	}

	public static void CenterVolume(ref SAtom Atom)
	{
		for (int i = 0; i < Atom.iMemSize; i++)
		{
			Atom.fPos[i, 0] -= (Atom.fLimit[0, 1] + Atom.fLimit[0, 0]) / 2;
			Atom.fPos[i, 1] -= (Atom.fLimit[1, 1] + Atom.fLimit[1, 0]) / 2;
			Atom.fPos[i, 2] -= Atom.fLimit[2, 0] + (Atom.fLimit[2, 1] - Atom.fLimit[2, 0]) / 2;
		}

		for (int j = 0; j < 2; j++)
		{
			Atom.fLimit[j, 0] -= (Atom.fLimit[j, 1] + Atom.fLimit[j, 0]) / 2;
			Atom.fLimit[j, 1] -= (Atom.fLimit[j, 1] + Atom.fLimit[j, 0]) / 2;
		}

		float fValue = Atom.fLimit[2, 0] + (Atom.fLimit[2, 1] - Atom.fLimit[2, 0]) / 2;
		Atom.fLimit[2, 0] -= fValue;
		Atom.fLimit[2, 1] -= fValue;
	}

	public static void ClearArray<T>(ref T[] tableau)
	{
		// Effacement du tableau
		Array.Clear(tableau, 0, tableau.Length);

		// Définir la référence sur null
		tableau = null;
	}

	static Random random = new Random();
	private static double GetRandomNumber(int minimum, int maximum)
	{
		return random.NextDouble() * (maximum - minimum) + minimum;
	}

}
