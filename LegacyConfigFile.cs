using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;

// Loadsa warnings and stuff here, buuuuuut
// not going to care, it's just loading of old configs.
// It's not something that's going to be maintained really.

namespace Aural_Probe
{
	public class LegacyConfigFile
	{
		private const string ConfigFilePath = "Aural Probe.cfg";
		private const string DefaultConfigFilePath = "Default.cfg";

		[JsonIgnore] public const int MaxSearchStringsPerCategory = 256;
		[JsonIgnore] private const int MaxCategories = 256;
		[JsonIgnore] private const int MaxDirectories = 256;
		[JsonIgnore] private const int VersionedConfigFileID = -666;

		// Version History
		// 0 = Unversioned config file
		// 1 = Initial first versioned config file, no change in data
		// 2 = Added default favorites directory
		// 3 = Added file types + autoplay
		// 4 = Added default sound device
		// 5 = Added always on top
		// 6 = Added use regular expression option (per category)
		// ...
		public int kCurrentConfigFileVersion = 6;

		public string[] categoryName;
		public string[,] categorySearchStrings;
		public bool[] categoryUseRegularExpressions;

		public string[] searchDirectories;

		public int lnNumCategories;
		public int[] lnNumCategorySearchStrings;

		public int lnSampleDisplaySizeW;
		public int lnSampleDisplaySizeH;

		public bool lbRescanPrompt;
		public bool lbIncludeFilePaths;
		public bool lbAlwaysOnTop;
		public string defaultFavoritesDirectory;
		public bool lbAutoplay;
		public int lnAutoplayRepeats;
		public bool lbWAV;
		public bool lbAIFF;
		public bool lbFLAC;
		public bool lbMP3;
		public bool lbWMA;
		public bool lbOGG;
		public string defaultSoundDevice;

		public LegacyConfigFile()
		{
			this.categoryName = new string[MaxCategories];
			this.categorySearchStrings = new string[MaxCategories, MaxSearchStringsPerCategory];
			this.categoryUseRegularExpressions = new bool[MaxCategories];
			this.searchDirectories = new string[MaxDirectories];
			this.lnNumCategorySearchStrings = new int[MaxCategories];
			this.lbRescanPrompt = true;
			this.lbIncludeFilePaths = true;
			this.lbAlwaysOnTop = false;
			this.lbAutoplay = false;
			this.lnAutoplayRepeats = 4;
			this.lbWAV = true;
			this.lbAIFF = true;
			this.lbFLAC = true;
			this.lbMP3 = true;
			this.lbWMA = true;
			this.lbOGG = true;
			this.lnSampleDisplaySizeH = 32;
			this.lnSampleDisplaySizeH = 192;
		}

		private string GetConfigFilePath()
		{
			return MainForm.GetApplicationDataPath() + "\\" + ConfigFilePath;
		}

		public static LegacyConfigFile CreateAndLoad()
		{
			var config = new LegacyConfigFile();
			config.Load();

			return config;
		}

		private void Load()
		{
			Directory.SetCurrentDirectory(MainForm.workingDirectory);

			bool configExists = File.Exists(this.GetConfigFilePath());
			bool oldConfigExists = File.Exists(ConfigFilePath);
			bool defaultExists = File.Exists(DefaultConfigFilePath);

			if (configExists)
			{
				this.ReadDataFromFile(this.GetConfigFilePath());
			}
			else if (oldConfigExists)
			{
				this.ReadDataFromFile(ConfigFilePath);
			}
			else if (defaultExists)
			{
				this.ReadDataFromFile(DefaultConfigFilePath);
				this.lnSampleDisplaySizeW = 192;
				this.lnSampleDisplaySizeH = 32;
			}
			else
			{
				// Set up defaults
				this.lnNumCategories = 1;
				this.categoryName[0] = "All Samples";
				this.lnSampleDisplaySizeW = 192;
				this.lnSampleDisplaySizeH = 32;
			}
		}

		private int ReadVersion(Stream myFileStream, BinaryFormatter deserializer)
		{
			int nVersion = (int)deserializer.Deserialize(myFileStream);

			if (nVersion == VersionedConfigFileID)
			{
				// We're reading a versioned config file, so read the actual version in now
				nVersion = (int)deserializer.Deserialize(myFileStream);
				this.lnNumCategories = (int)deserializer.Deserialize(myFileStream);
			}
			else
			{
				// Unversioned config file, so version is 0 and first int was actually number of categories
				this.lnNumCategories = nVersion;
				nVersion = 0;
			}

			return nVersion;
		}

		private void ReadCategories(
			Stream myFileStream,
			BinaryFormatter deserializer,
			int nVersion
		) {
			for (int lnCategory = 0; lnCategory < this.lnNumCategories; lnCategory++ )
			{
				this.categoryName[lnCategory] = (string)deserializer.Deserialize(myFileStream);
				this.lnNumCategorySearchStrings[lnCategory] = (int)deserializer.Deserialize(myFileStream);

				for (int lnSS = 0; lnSS < this.lnNumCategorySearchStrings[lnCategory]; lnSS++ )
				{
					this.categorySearchStrings[lnCategory, lnSS] = (string)deserializer.Deserialize(myFileStream);
				}

				if (nVersion >= 6)
				{
					this.categoryUseRegularExpressions[lnCategory] = (bool)deserializer.Deserialize(myFileStream);
				}
			}
		}

		private void ReadSearchDirectories(
			Stream myFileStream,
			BinaryFormatter deserializer
		) {
			var lnNumSearchDirectories = (int)deserializer.Deserialize(myFileStream);

			for(int lnSearchDirectory = 0; lnSearchDirectory < lnNumSearchDirectories; lnSearchDirectory++ )
			{
				this.searchDirectories[lnSearchDirectory] = (string)deserializer.Deserialize(myFileStream);
			}
		}

		private void ReadSettings(
			Stream myFileStream,
			BinaryFormatter deserializer
		) {
			this.lnSampleDisplaySizeH = (int)deserializer.Deserialize(myFileStream);
			this.lbRescanPrompt = (bool)deserializer.Deserialize(myFileStream);
			this.lbIncludeFilePaths = (bool)deserializer.Deserialize(myFileStream);
		}

		private void ReadFavouritesDirectory(
			Stream myFileStream,
			BinaryFormatter deserializer,
			int nVersion
		) {
			if (nVersion >= 2)
			{
				this.defaultFavoritesDirectory = (string)deserializer.Deserialize(myFileStream);
			}
			else
			{
				this.defaultFavoritesDirectory = "";
			}
		}

		private void ReadFormatOptions(
			Stream myFileStream,
			BinaryFormatter deserializer,
			int nVersion
		) {
			if (nVersion >= 3)
			{
				this.lbAutoplay = (bool)deserializer.Deserialize(myFileStream);
				this.lnAutoplayRepeats = (int)deserializer.Deserialize(myFileStream);
				this.lbWAV = (bool)deserializer.Deserialize(myFileStream);
				this.lbAIFF = (bool)deserializer.Deserialize(myFileStream);
				this.lbFLAC = (bool)deserializer.Deserialize(myFileStream);
				this.lbMP3 = (bool)deserializer.Deserialize(myFileStream);
				this.lbWMA = (bool)deserializer.Deserialize(myFileStream);
				this.lbOGG = (bool)deserializer.Deserialize(myFileStream);
				this.lnSampleDisplaySizeW = (int)deserializer.Deserialize(myFileStream);
			}
			else
			{
				this.lbAutoplay = false;
				this.lnAutoplayRepeats = 4;
				this.lbWAV = true;
				this.lbAIFF = true;
				this.lbFLAC = true;
				this.lbMP3 = true;
				this.lbWMA = true;
				this.lbOGG = true;
				this.lnSampleDisplaySizeW = this.lnSampleDisplaySizeH * 6;
			}
		}

		private void ReadSoundDeviceOptions(
			Stream myFileStream,
			BinaryFormatter deserializer,
			int nVersion
		) {
			if (nVersion >= 4)
			{
				this.defaultSoundDevice = (string)deserializer.Deserialize(myFileStream);
			}
			else
			{
				this.defaultSoundDevice = "";
			}
		}

		private void ReadWindowOptions(
			Stream myFileStream,
			BinaryFormatter deserializer,
			int nVersion
		) {
			if (nVersion >= 5)
			{
				this.lbAlwaysOnTop = (bool)deserializer.Deserialize(myFileStream);
			}
			else
			{
				this.lbAlwaysOnTop = false;
			}
		}

		private void ReadDataFromFile(string filename)
		{
			using (Stream myFileStream = File.OpenRead(filename))
			{
				BinaryFormatter deserializer = new BinaryFormatter();

				int nVersion = this.ReadVersion(myFileStream, deserializer);
				this.ReadCategories(myFileStream, deserializer, nVersion);
				this.ReadSearchDirectories(myFileStream, deserializer);
				this.ReadSettings(myFileStream, deserializer);
				this.ReadFavouritesDirectory(myFileStream, deserializer, nVersion);
				this.ReadFormatOptions(myFileStream, deserializer, nVersion);
				this.ReadSoundDeviceOptions(myFileStream, deserializer, nVersion);
				this.ReadWindowOptions(myFileStream, deserializer, nVersion);
			}
		}
	}
}