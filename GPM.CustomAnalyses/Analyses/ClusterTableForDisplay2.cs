namespace GPM.CustomAnalyses.Analyses;

internal class ClusterTableForDisplay2
{

	public int ClusterId { get; private set; }
	public int NbAtomForCompo { get; private set; }
	public int NbAtom_Noise { get; private set; }
	public int NbAtom0 { get; private set; }
	public float Composition0 { get; private set; }
	public int NbAtom1 { get; private set; }
	public float Composition1 { get; private set; }
	public int NbAtom2 { get; private set; }
	public float Composition2 { get; private set; }
	public int NbAtom3 { get; private set; }
	public float Composition3 { get; private set; }

	public float LgX { get; private set; }
	public float LgY { get; private set; }
	public float LgZ { get; private set; }
	public float LgTotal { get; private set; }
	public float Rg { get; private set; }

	public float X0 { get; private set; }
	public float Y0 { get; private set; }
	public float Z0 { get; private set; }

	public ClusterTableForDisplay2(int iClusterId, int iTotalNbAtom, int iNbAtom_Noise, int[] iNbAtom, float[] fCompo, float[] fInfo)
	{

		ClusterId = iClusterId;
		NbAtomForCompo = iTotalNbAtom;
		NbAtom_Noise = iNbAtom_Noise;

		NbAtom0 = iNbAtom[0];
		Composition0 = fCompo[0];
		NbAtom1 = iNbAtom[1];
		Composition1 = fCompo[1];
		NbAtom2 = iNbAtom[2];
		Composition2 = fCompo[2];
		NbAtom3 = iNbAtom[3];
		Composition3 = fCompo[3];

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
