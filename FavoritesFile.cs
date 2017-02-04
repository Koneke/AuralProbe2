using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace Aural_Probe
{
	/// <summary>
	/// Favorite file container.
	/// </summary>
	public class FavoritesFile
	{
		private const string Extension = ".apf";
		private const string DefaultFavoritesFilename = "Untitled";

		// Version History
		// 1 = Initial first versioned favorites file, reads sampleIndices array index (which may be invalid), then full sample filename
		// ...
		public string FileName;
		public int CurrentConfigFileVersion = 1;
		public List<string> Favorites;

		// Version file data
		[JsonIgnore] public bool Loaded;

		public static string GetDefaultFilename()
		{
			return DefaultFavoritesFilename;
		}

		public FavoritesFile(string filename = null)
		{
			this.FileName = filename ?? GetDefaultFilename();
			this.Loaded = false;
			this.Favorites = new List<string>();
		}

		public void Reset()
		{
			this.Loaded = false;
			this.Favorites.Clear();
		}

		public static FavoritesFile Load(string filename)
		{
			if (File.Exists(filename))
			{
				return JsonConvert.DeserializeObject<FavoritesFile>(File.ReadAllText(filename));
			}

			MessageBox.Show(
				"Can't load favorites file, it doesn't exist.",
				"Failed to load favorites file",
				MessageBoxButtons.OK,
				MessageBoxIcon.Error);

			return null;
		}

		public void Save(string filename)
		{
			File.WriteAllText(filename, JsonConvert.SerializeObject(this, Formatting.Indented));
		}

		public void Save()
		{
			this.Save(this.FileName);
		}
	}
}
