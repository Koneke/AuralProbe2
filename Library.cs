using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace Aural_Probe
{
	public class Sample
	{
		// kinda wish we didn't need to keep category,
		// it's a kinda weird way of organising things,
		// atleast the way they work right now,
		// but I'm not here to remove features.
		public string Name => this.Path.Split('\\').Last();
		public Category Category;
		public string Path;
		public bool Exists => File.Exists(this.Path);
		public int ColorIndex;
		public int BitField;
		public bool Favorited;
		public bool Missing;

		public Sample(string path)
		{
			this.Path = path;
		}
	}

	public class Tag
	{
	}

	// we should get rid of this actually,
	// just have a category name or whatever on sample
	public class Category
	{
		[JsonIgnore] private App app;

		// we need an underlying field, or automatic update of the name
		// in the category listbox won't work
		// (doesn't handle nested properties)
		[JsonIgnore] private string name;
		public string Name
		{
			get
			{
				return this.name;
			}
			set
			{
				this.name = value;
			}
		}

		[JsonIgnore] public Category Cat => this;

		[JsonIgnore] public string ListName
		{
			get
			{
				var listName = this.name;

				listName += " (" +
					(this.app.lbFavoritesOnly ? this.Favorites : this.Samples).Count
					+ ")";

				return listName;
			}
		}

		public List<Sample> Samples;
		[JsonIgnore] public List<Sample> Favorites => this.Samples.Where(sample => sample.Favorited).ToList();
		[JsonIgnore] public bool IsEmpty => this.Samples.Count == 0;
		public List<string> SearchStrings;
		public bool UseRegex;
		public string Regex;

		public Category(App app)
		{
			this.app = app;
			this.Samples = new List<Sample>();
		}
	}

	public class Library
	{
		private App app;

		public List<Category> Categories => this.app.Files.ConfigFile.Categories; // lel...
		public int FavoriteCount => this.Categories?.Sum(category => category.Favorites.Count) ?? 0;
		public List<Sample> Samples => this.Categories.SelectMany(category => category.Samples).ToList();

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

		public Category CreateCategory(
			string name,
			List<string> searchStrings,
			bool useRegex,
			string regex)
		{
			return new Category(this.app) {
				Name = name,
				SearchStrings = searchStrings,
				UseRegex= useRegex,
				Regex = regex
			};
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

		public void ClearAll()
		{
			// yet another reason why categories probably just should be
			// a name or whatever on samples
			// so we just have each sample in a single list
			// instead of distributed like this.

			this.Samples.Clear();

			foreach (var category in this.Categories)
			{
				category.Samples.Clear();
			}
		}
	}
}