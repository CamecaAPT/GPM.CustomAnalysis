using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GPM.CustomAnalyses;
internal class varGlob
{
	public static Color[] tabColor = {Colors.Blue, Colors.Red, Colors.Green, Colors.Yellow, Colors.Pink, Colors.Brown, Colors.Gray, Colors.Orange, Colors.Purple, Colors.Aqua};

	public struct SAtom
	{
		public bool bState;

		public int iMemSize;
		public int iNbCluster;
		public float[,] fLimit;

		public byte[,] bEltId;
		public float[,] fPos;
		public float[,] fMass;
		public int[] iCluId;
		public Int32[] iMultipleEvent;
		public float[] fVDC;
		public float[,] fDPos;
		public int[] iFamilyId;
	}

	public struct Elt
	{
		public List<string> Name;
		public List<Color> Color;
		public int iNbElt;
	}

	public struct Family 
	{
		public bool A;
		public bool B;
		public bool C;
		public bool D;
		public bool E;
		public bool F;
	}
}
