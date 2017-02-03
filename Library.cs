using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Aural_Probe
{
	public class Sample
	{
		// kinda wish we didn't need to keep category,
		// it's a kinda weird way of organising things,
		// atleast the way they work right now,
		// but I'm not here to remove features.
		public Category Category;
		public string Path;
		public int ColorIndex;
		public int BitField;
		public bool Favorited;
	}

	public class Tag
	{
	}

	public class Category
	{
		public string Name;
		public List<Sample> Samples;
		public int FavoriteCount;
		public List<string> SearchStrings;
		public bool UseRegex;
		public string Regex;

		public void UpdateFavoriteCount()
		{
			this.FavoriteCount = this.Samples.Count(sample => sample.Favorited);
		}
	}

	public class Library
	{
		private App app;

		public List<Category> Categories;
		public int FavoriteCount;
		public List<Sample> Samples;

		public string[] sampleList;
		public int[] sampleColorIndex;
		public int[,] sampleIndices;
		public int[] sampleIndicesCount;
		public int[] sampleFavoritesCount; // favourites per category
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

		public void UpdateFavoriteCounts()
		{
			foreach (var category in this.Categories)
			{
				category.UpdateFavoriteCount();
			}

			this.FavoriteCount = this.Categories.Sum(category => category.FavoriteCount);
		}
	}
}