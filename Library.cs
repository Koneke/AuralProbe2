using System.Collections.Generic;
using System.IO;
using System.Linq;
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
		[JsonIgnore] protected readonly App App;

		// we need an underlying field, or automatic update of the name
		// in the category listbox won't work
		// (doesn't handle nested properties)
		[JsonIgnore] private string name;
		public string Name { get { return this.name; } set { this.name = value; } }

		[JsonIgnore] public Category Cat => this;

		[JsonIgnore] public string ListName
		{
			get
			{
				var listName = this.name;

				listName += " (" +
					(this.App.lbFavoritesOnly ? this.Favorites : this.Samples).Count
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
			this.App = app;
			this.Samples = new List<Sample>();
		}

		public void AddSample(Sample sample)
		{
			this.Samples.Add(sample);
			sample.Category = this;
		}
	}

	public class AllSamplesCategory : Category
	{
		public new List<Sample> Samples => this.App.Library.Categories
			.SelectMany(category => category.Samples)
			.ToList();

		public AllSamplesCategory(App app) : base(app)
		{
		}
	}

	public class Library
	{
		private readonly App app;

		public List<Category> Categories => this.app.Files.ConfigFile.Categories; // lel...
		public int FavoriteCount => this.Categories?.Sum(category => category.Favorites.Count) ?? 0;
		public List<Sample> Samples => this.Categories.SelectMany(category => category.Samples).ToList();
		public List<Sample> _Samples;

		public Library(App app)
		{
			this.app = app;
			this._Samples = new List<Sample>();
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

		public void AddSample(Category category, Sample sample)
		{
			this._Samples.Add(sample);

			// Category.AddSample sets category,
			// but we're going to phase that out.
			// so just so I don't forget down the line.
			category.AddSample(sample);
			sample.Category = category;
		}
	}
}