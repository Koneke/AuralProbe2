﻿using System.Windows.Forms;

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
	}

	public class Library
	{
		private App app;

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
				sampleIndices = new int[app.Files.configFile.kMaxCategories,nSize];
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("AllocateSampleData " + nSize + " " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
			}
		}
	}
}