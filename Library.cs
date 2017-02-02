using System.Collections.Generic;
using System.Windows.Forms;

namespace Aural_Probe
{
	public class Sample
	{
	}

	public class Tag
	{
	}

	public class Category
	{
		public string Name;
		public List<string> SearchStrings;
		public bool UseRegex;
	}

	public class Library
	{
		private App app;

		public List<Category> Categories;

		public string[] sampleList;
		public int[] sampleColorIndex;
		public int[,] sampleIndices;
		public int[] sampleIndicesCount;
		public int[] sampleFavoritesCount;
		public int[] sampleBitField;

		public Library(App app)
		{
			this.app = app;
		}

		public void AllocateSampleData(int nSize)
		{
			try
			{
				sampleList = new string[nSize];
				sampleColorIndex = new int[nSize];
				sampleBitField = new int[nSize];
				sampleIndices = new int[ConfigFile.MaxCategories,nSize];
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("AllocateSampleData " + nSize + " " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
			}
		}
	}
}