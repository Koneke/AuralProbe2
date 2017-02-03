using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Aural_Probe
{
	/// <summary>
	/// Configuration file container.
	/// </summary>
	public class ConfigFile
	{
		private const string ConfigFilePath = "Aural Probe.cfg.json";
		private const string DefaultConfigFilePath = "Default.cfg.json";
		private const string OldFormatConfigFilePath = "Aural Probe.cfg";

		[JsonIgnore] public const int MaxCategories = 256;

		// Version History
		// 0 = Unversioned config file
		// 1 = Initial first versioned config file, no change in data
		// 2 = Added default favorites directory
		// 3 = Added file types + autoplay
		// 4 = Added default sound device
		// 5 = Added always on top
		// 6 = Added use regular expression option (per category)
		// 7 = Switched to JSON
		// ...

		// Resharper complains about it not being used, but we're writing it to file.
		// ReSharper disable once UnusedMember.Global
		public const int CurrentConfigFileVersion = 7;

		public readonly List<Category> Categories;
		public List<string> SearchDirectories;
		public List<string> SearchDirectoriesScrubbed;

		public bool RescanPrompt;
		public bool IncludeFilePaths;

		public bool AlwaysOnTop;
		public string DefaultFavoritesDirectory;
		public bool Autoplay;
		public int AutoplayRepeats;
		public bool LoadWav;
		public bool LoadAiff;
		public bool LoadFlac;
		public bool LoadMp3;
		public bool LoadWma;
		public bool LoadOgg;
		public string DefaultSoundDevice;

		public int SampleDisplaySizeW;
		public int SampleDisplaySizeH;

		private ConfigFile()
		{
			this.Categories = new List<Category>();
			this.SearchDirectories = new List<string>();
			this.SearchDirectoriesScrubbed = new List<string>();

			this.RescanPrompt = true;
			this.IncludeFilePaths = true;
			this.AlwaysOnTop = false;
			this.Autoplay = false;
			this.AutoplayRepeats = 4;
			this.LoadWav = true;
			this.LoadAiff = true;
			this.LoadFlac = true;
			this.LoadMp3 = true;
			this.LoadWma = true;
			this.LoadOgg = true;
			this.SampleDisplaySizeH = 32;
			this.SampleDisplaySizeH = 192;
		}

		private static ConfigFile ConvertLegacyConfig(LegacyConfigFile legacyConfig)
		{
			var config = new ConfigFile();

			for (var i = 0; i < legacyConfig.lnNumCategories; ++i)
			{
				var searchStrings = new List<string>();

				for (var j = 0; j < LegacyConfigFile.MaxSearchStringsPerCategory; ++j)
				{
					if (legacyConfig.categorySearchStrings[i, j] == null)
					{
						break;
					}

					searchStrings.Add(legacyConfig.categorySearchStrings[i, j]);
				}

				config.Categories.Add(new Category {
					Name = legacyConfig.categoryName[i],
					SearchStrings = searchStrings,
					UseRegex = legacyConfig.categoryUseRegularExpressions[i]
				});
			}

			config.SearchDirectories = legacyConfig.searchDirectories.Where(sd => sd != null).ToList();

			config.DefaultFavoritesDirectory = legacyConfig.defaultFavoritesDirectory;
			config.RescanPrompt = legacyConfig.lbRescanPrompt;
			config.IncludeFilePaths = legacyConfig.lbIncludeFilePaths;
			config.AlwaysOnTop = legacyConfig.lbAlwaysOnTop;
			config.Autoplay = legacyConfig.lbAutoplay;
			config.AutoplayRepeats = legacyConfig.lnAutoplayRepeats;
			config.LoadWav = legacyConfig.lbWAV;
			config.LoadAiff = legacyConfig.lbAIFF;
			config.LoadFlac = legacyConfig.lbFLAC;
			config.LoadMp3 = legacyConfig.lbMP3;
			config.LoadWma = legacyConfig.lbWMA;
			config.LoadOgg = legacyConfig.lbOGG;
			config.SampleDisplaySizeH = legacyConfig.lnSampleDisplaySizeH;
			config.SampleDisplaySizeW = legacyConfig.lnSampleDisplaySizeW;
			config.DefaultSoundDevice = legacyConfig.defaultSoundDevice;

			return config;
		}

		// scrub = search..?
		// i.e. basically look in, find samples, or whatever
		private void UpdateScrubbedSearchDirectories()
		{
			var scrubbedList = new List<string>();

			// honestly I don't see exactly what this thing is for?
			// I'm guessing if you have several search directories, and they overlap in some manner?
			// or something like that?
			foreach (var searchDirectory in this.SearchDirectories)
			{
				var str = searchDirectory?.ToLower();

				if (str == null)
				{
					continue;
				}

				if (Directory.Exists(str))
				{
					// skip duplicates
					// (or maybe just like, yknow, don't allowed duplicates)
					// (probably better way of handling this)
					if (!scrubbedList.Contains(str))
					{
						var bAlreadyContainedByOtherDirectory = false;

						//for (int j = 0; j < lnNumSearchDirectories; ++j)
						foreach (var secondSearchDirectory in this.SearchDirectories)
						{
							var newStr = secondSearchDirectory?.ToLower();

							if (newStr == null || str == newStr)
							{
								continue;
							}

							if (str.IndexOf(newStr, StringComparison.Ordinal) != -1)
							{
								bAlreadyContainedByOtherDirectory = true;
								break;
							}
						}

						if (!bAlreadyContainedByOtherDirectory)
						{
							scrubbedList.Add(searchDirectory);
						}
					}
				}
			}

			this.SearchDirectoriesScrubbed = scrubbedList;
		}

		private static string GetOldFormatConfigFilePath()
		{
			return MainForm.GetApplicationDataPath() + "\\" + OldFormatConfigFilePath;
		}

		private static string GetConfigFilePath()
		{
			return MainForm.GetApplicationDataPath() + "\\" + ConfigFilePath;
		}

		private static ConfigFile LoadJsonConfig(string path)
		{
			var json = File.ReadAllText(path);
			var configFile = JsonConvert.DeserializeObject<ConfigFile>(json);
			return configFile;
		}

		public static ConfigFile Load()
		{
			ConfigFile config;

			Directory.SetCurrentDirectory(MainForm.workingDirectory);

			var configExists = File.Exists(GetConfigFilePath());
			var defaultExists = File.Exists(DefaultConfigFilePath);

			var oldFormatConfigExists = File.Exists(GetOldFormatConfigFilePath());
			var oldOldFormatConfigExists = File.Exists(OldFormatConfigFilePath);

			if (configExists)
			{
				config = LoadJsonConfig(GetConfigFilePath());
			}
			else if (oldFormatConfigExists || oldOldFormatConfigExists)
			{
				config = ConvertLegacyConfig(LegacyConfigFile.CreateAndLoad());
			}
			else if (defaultExists)
			{
				config = LoadJsonConfig(DefaultConfigFilePath);
			}
			else
			{
				config = new ConfigFile
				{
					// Set up defaults
					SampleDisplaySizeW = 192,
					SampleDisplaySizeH = 32
				};

				config.Categories.Add(new Category {
					Name = "All Samples"
				});
			}

			config.UpdateScrubbedSearchDirectories();

			return config;
		}

		public void Save()
		{
			File.WriteAllText(
				GetConfigFilePath(),
				JsonConvert.SerializeObject(this, Formatting.Indented));

			this.UpdateScrubbedSearchDirectories();
		}
	}
}