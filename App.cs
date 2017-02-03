using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace Aural_Probe
{
	public class App
	{
		public FmodManager fmodManager;
		public FMOD.CHANNEL_CALLBACK cbFMOD = null;

		private MainForm mainForm;

		public bool lbFavoritesOnly;
		public bool lbDirtyFavorites;
		public bool bUseCachedSamplesIfPossible;

		private int bitFavorite = 0;
		private int bitMissing = 1;

		public Library Library;
		public Files Files;

		public int listSamplesSingleSelectedIndex; // when there are multiple selections, this is -1, otherwise it's listSamples.SelectedIndex
		public int[] listSamplesLastSelectedIndices; // remember the last selected indices to properly handle ListBox item invalidation

		public static int knMaxColors = 16;
		public int nColorInc;
		public Color[,] colorList;

		public App(MainForm mainForm)
		{
			this.Library = new Library(this);
			this.Files = new Files();
		}

		private void Init()
		{
			this.Files.workingDirectory = Directory.GetCurrentDirectory();
			this.Files.configFile = ConfigFile.Load();

			var a = JsonConvert.SerializeObject(this.Files.configFile, Formatting.Indented);

			this.lbDirtyFavorites = false;

			this.Files.favoritesFile = new FavoritesFile();
			this.mainForm.configurationForm = new ConfigurationForm();
			this.mainForm.aboutForm = new AboutForm();
			this.mainForm.progressForm = new ProgressBar(this.mainForm);

			this.nColorInc = 0;
			this.colorList = new Color[knMaxColors,2];
			for (int i = 0; i < knMaxColors; ++i)
			{
				float H = i * (360.0f / knMaxColors);
				float S = 0.25f;
				float V1 = 0.5f;
				float V2 = 1.0f;
				float R = 0.0f;
				float G = 0.0f;
				float B = 0.0f;

				ColorUtils.HSVtoRGB(ref H, ref S, ref V1, ref R, ref G, ref B);
				this.colorList[i, 0] = Color.FromArgb((int)(R * 255.0f), (int)(G * 255.0f), (int)(B * 255.0f));

				ColorUtils.HSVtoRGB(ref H, ref S, ref V2, ref R, ref G, ref B);
				this.colorList[i, 1] = Color.FromArgb((int)(R * 255.0f), (int)(G * 255.0f), (int)(B * 255.0f));
			}

			this.Library.sampleIndicesCount = new int[ConfigFile.MaxCategories];
			this.Library.sampleFavoritesCount = new int[ConfigFile.MaxCategories];

			this.lbFavoritesOnly = false;

			this.bUseCachedSamplesIfPossible = true; // set to true once on startup
		}

		public void InitContextMenu()
		{
			// Init context menu
			this.mainForm.sampleListMenu = new ContextMenu();
			this.mainForm.sampleListMenu.MenuItems.Add(
				0, 
				new MenuItem(
					"Explore...\tEnter",
					new EventHandler(this.ExploreSamples)));
			this.mainForm.sampleListMenu.MenuItems.Add(
				1,
				new MenuItem(
					"Copy\tCtrl+C",
					new EventHandler(this.CopySamples)));
			this.mainForm.sampleListMenu.MenuItems.Add(
				2, 
				new MenuItem(
					"Copy as path\tCtrl+Shift+C",
					new EventHandler(this.CopySamplesShortcut)));
			this.mainForm.sampleListMenu.MenuItems.Add(
				3,
				new MenuItem(
					"Favorite\tSpace",
					new EventHandler(this.AddRemoveFromFavorites)));
			this.mainForm.sampleListMenu.MenuItems.Add(
				4,
				new MenuItem("Delete\tDel",
				new EventHandler(this.DeleteSamples)));

			this.mainForm.listSamples.ContextMenu = this.mainForm.sampleListMenu;
		}

		public void InitHandlers()
		{
			Application.ApplicationExit += new EventHandler(this.Application_ApplicationExit);
			SystemEvents.SessionEnding += new SessionEndingEventHandler(this.SystemEvents_SessionEnding);
			this.cbFMOD = new FMOD.CHANNEL_CALLBACK(MainForm.soundEndedCallback);
		}

		public void foo(MainForm mainForm)
		{
			this.mainForm = mainForm;

			try
			{
				this.Init();
				this.InitContextMenu();
				this.InitHandlers();
				this.fmodManager = new FmodManager(this);
			}
			catch (Exception e)
			{
				MessageBox.Show("MainForm::MainForm " + e.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		public string GetCategoryListName(int i)
		{
			try
			{
				// Category name, followed by number of favourites in category (if in favourite mode), or number of samples in category, in parenthesis.
				return this.Files.configFile.Categories[i].Name + " (" + (this.lbFavoritesOnly ? this.Library.sampleFavoritesCount[i] : this.Library.sampleIndicesCount[i]).ToString() + ")";
			}
			catch (Exception e)
			{
				MessageBox.Show("GetCategoryListName " + i + " " + e.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
				return "";
			}
		}

		public bool GetSampleFlag(int sample, int bit) => (this.Library.sampleBitField[sample] & (1 << bit)) != 0;

		public void SetSampleFlag(int sample, int bit, bool val)
		{
			try
			{
				if (val == true)
				{
					this.Library.sampleBitField[sample] |= 1 << bit;
				}
				else
				{
					this.Library.sampleBitField[sample] &= ~(1 << bit);
				}
			}
			catch (Exception e)
			{
				MessageBox.Show("SetSampleFlag " + e.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		public int CalculateRealSampleIndex(int listBoxIndex)
		{
			try
			{
				if (!this.lbFavoritesOnly)
				{
					return listBoxIndex;
				}
				else
				{
					int nCurrentCategory = this.mainForm.categoriesList.SelectedIndex;

					if (nCurrentCategory < 0 || this.Library.sampleIndicesCount[nCurrentCategory] == 0)
					{
						return -1;
					}

					int nFavoriteCount = 0;
					for (int i = 0; i < this.Library.sampleIndicesCount[nCurrentCategory]; ++i)
					{
						int nSampleIndex = this.Library.sampleIndices[nCurrentCategory, i];
						if (this.GetSampleFlag(nSampleIndex, this.bitFavorite))
						{
							if (nFavoriteCount == listBoxIndex)
								return i;
							nFavoriteCount++;
						}
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("CalculateRealSampleIndex " + listBoxIndex + " " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
			return -1;
		}

		public void ExploreSamples(object sender, EventArgs e)
		{
			try
			{
				this.fmodManager.StopSoundPlayback();

				int nCurrentCategory = this.mainForm.categoriesList.SelectedIndex;
				if (nCurrentCategory < 0 || this.Library.sampleIndicesCount[nCurrentCategory] == 0) return;
				if (this.mainForm.listSamples.SelectedIndices.Count == 0) return;
				if (this.mainForm.listSamples.SelectedIndices.Count > 1 &&
					DialogResult.No == MessageBox.Show(
						"Are you sure you want to open " + this.mainForm.listSamples.SelectedIndices.Count.ToString() + " explorer windows?",
						"Explore multiple samples?",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Question))
					return;

				for (int i = 0; i < this.mainForm.listSamples.SelectedIndices.Count; ++i)
				{
					int nCurrentSample = this.mainForm.CalculateRealSampleIndex(this.mainForm.listSamples.SelectedIndices[i]);
					if (nCurrentSample < 0) continue;

					string sampleName = this.mainForm.sampleList[this.Library.sampleIndices[nCurrentCategory, nCurrentSample]];
					System.Diagnostics.Process proc = new System.Diagnostics.Process();
					proc.EnableRaisingEvents = false;
					proc.StartInfo.FileName = "explorer";
					proc.StartInfo.Arguments = "/n,/select," + sampleName;
					proc.Start();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("ExploreSamples " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		public void CopySamples(object sender, EventArgs e)
		{
			try
			{
				int nCurrentCategory = this.mainForm.categoriesList.SelectedIndex;
				if (nCurrentCategory < 0 || this.Library.sampleIndicesCount[nCurrentCategory] == 0)
					return;
				if (this.mainForm.listSamples.SelectedIndices.Count == 0)
					return;
				DataObject objData = new DataObject();
				string[] filename = new string[this.mainForm.listSamples.SelectedIndices.Count];
				for (int i = 0; i < this.mainForm.listSamples.SelectedIndices.Count; ++i)
				{
					int nCurrentSample = this.CalculateRealSampleIndex(this.mainForm.listSamples.SelectedIndices[i]);
					if (nCurrentSample < 0)
						continue;
					filename[i] = this.mainForm.sampleList[this.Library.sampleIndices[nCurrentCategory, nCurrentSample]];
				}
				objData.SetData(DataFormats.FileDrop, true, filename);  
				Clipboard.SetDataObject(objData, true);
				this.mainForm.statusBarPanel.Text = "Copied file(s) to clipboard.";
				this.mainForm.statusBarPanel.ToolTipText = "";
			}
			catch (Exception ex)
			{
				MessageBox.Show("CopySamples " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
			}
		}

		public void CopySamplesShortcut(object sender, EventArgs e)
		{
			try
			{
				int nCurrentCategory = this.mainForm.categoriesList.SelectedIndex;
				if (nCurrentCategory < 0 || this.Library.sampleIndicesCount[nCurrentCategory] == 0)
					return;
				if (this.mainForm.listSamples.SelectedIndices.Count == 0)
					return;
				string sampleNames = "";
				for (int i = 0; i < this.mainForm.listSamples.SelectedIndices.Count; ++i)
				{
					int nCurrentSample = this.CalculateRealSampleIndex(this.mainForm.listSamples.SelectedIndices[i]);
					if (nCurrentSample < 0)
						continue;
					sampleNames += this.mainForm.sampleList[this.Library.sampleIndices[nCurrentCategory, nCurrentSample]] + "\r\n";
				}
				Clipboard.SetDataObject(sampleNames,true);
				this.mainForm.statusBarPanel.Text = "Copied file path(s) to clipboard.";
				this.mainForm.statusBarPanel.ToolTipText = "";
			}
			catch (Exception ex)
			{
				MessageBox.Show("CopySamplesShortcut " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
			}
		}

		public void AddRemoveFromFavorites(object sender, EventArgs e)
		{
			try
			{
				int nCurrentCategory = this.mainForm.categoriesList.SelectedIndex;
				if (nCurrentCategory < 0 || this.Library.sampleIndicesCount[nCurrentCategory] == 0)
					return;
				if (this.mainForm.listSamples.SelectedIndices.Count == 0)
					return;

				int nFavoriteSample = this.CalculateRealSampleIndex(this.mainForm.listSamples.SelectedIndices[0]);
				if (nFavoriteSample < 0)
					return;
				int favoriteSampleIndex = this.Library.sampleIndices[nCurrentCategory, nFavoriteSample];
				bool isFavorite = this.GetSampleFlag(favoriteSampleIndex, this.bitFavorite);

				for (int i = 0; i < this.mainForm.listSamples.SelectedIndices.Count; ++i)
				{
					int nCurrentSample = this.CalculateRealSampleIndex(this.mainForm.listSamples.SelectedIndices[i]);
					if (nCurrentSample < 0)
						continue;
					int sampleIndex = this.Library.sampleIndices[nCurrentCategory, nCurrentSample];
					this.SetSampleFlag(sampleIndex, this.bitFavorite, !isFavorite);

					this.lbDirtyFavorites = true;
				}

				this.mainForm.UpdateSampleFavorites();

				if (this.lbFavoritesOnly)
				{
					this.mainForm.UpdateAudioSamples();
				}
				else
				{
					for (int i = 0; i < this.mainForm.listSamples.SelectedIndices.Count; ++i)
						this.mainForm.listSamples.Invalidate(this.mainForm.listSamples.GetItemRectangle(this.mainForm.listSamples.SelectedIndices[i]));
				}
				// update favorites checkbox state in popup menu
				if (this.listSamplesSingleSelectedIndex != -1)
				{
					int nCurrentSample = this.CalculateRealSampleIndex(this.listSamplesSingleSelectedIndex);
					if (nCurrentSample >= 0)
					{
						int sampleIndex = this.Library.sampleIndices[nCurrentCategory, nCurrentSample];
						this.mainForm.sampleListMenu.MenuItems[3].Checked = this.GetSampleFlag(sampleIndex, this.bitFavorite);
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("AddRemoveFromFavorites " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
			}
		}

		public void DeleteSamples(object sender, EventArgs e)
		{
			this.fmodManager.StopSoundPlayback();

			try
			{
				int nCurrentCategory = this.mainForm.categoriesList.SelectedIndex;
				if (nCurrentCategory < 0 || this.Library.sampleIndicesCount[nCurrentCategory] == 0) return;
				if (this.mainForm.listSamples.SelectedIndices.Count == 0) return;
				else if (this.mainForm.listSamples.SelectedIndices.Count == 1)
				{
					if (DialogResult.No == MessageBox.Show(
						"Are you sure you want to permanently delete this sample from your computer? Deleted samples will remain in the samples list until you rescan search folders.",
						"Delete sample?",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Question,
						MessageBoxDefaultButton.Button2))
						return;
				}
				else if (this.mainForm.listSamples.SelectedIndices.Count > 1)
				{
					if (DialogResult.No == MessageBox.Show(
						"Are you sure you want to permanently delete " + this.mainForm.listSamples.SelectedIndices.Count.ToString() + " samples from your computer? " +
							"Deleted samples will remain in the samples list until you rescan search folders.",
						"Delete multiple samples?",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Question,
						MessageBoxDefaultButton.Button2))
						return;
				}
				string deleteErrors = "";
				for (int i = 0; i < this.mainForm.listSamples.SelectedIndices.Count; ++i)
				{
					int nCurrentSample = this.CalculateRealSampleIndex(this.mainForm.listSamples.SelectedIndices[i]);
					if (nCurrentSample < 0) continue;
					string sampleName = this.mainForm.sampleList[this.Library.sampleIndices[nCurrentCategory, nCurrentSample]];
					try
					{
						File.Delete(sampleName);
						this.SetSampleFlag(nCurrentSample, this.bitMissing, true);
						this.mainForm.listSamples.Invalidate(this.mainForm.listSamples.GetItemRectangle(this.mainForm.listSamples.SelectedIndices[i]));
					}
					catch (Exception ex)
					{
						deleteErrors += ex.Message.ToString() + "\n";
					}
				}

				if (deleteErrors != "")
					MessageBox.Show("One or more errors were encountered during delete:\n" + deleteErrors, "Delete operation completed with errors", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
			catch (Exception ex)
			{
				MessageBox.Show("DeleteSamples " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		public void Application_ApplicationExit(object sender, EventArgs e) 
		{
			try
			{
				if (!this.lbDirtyFavorites || this.Library.sampleFavoritesCount[0] == 0)
				{
					return;
				}

				// User is trying to quit. Prompt the user with choices
				var dr = MessageBox.Show(
					"Do you want to save changes to your favorites?\n",
					"Save changes?",
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Question);

				if (dr == DialogResult.Yes)
				{
					this.mainForm.SaveFavorites(); // if we cancel for some reason, don't abort program, let them save again!
				}
				else if (dr == DialogResult.No)
				{
					return;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Application_ApplicationExit " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		public void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e) 
		{
			try
			{
				if (!this.lbDirtyFavorites || this.Library.sampleFavoritesCount[0] == 0) return;

				// User is trying to log out. Prompt the user with choices
				var dr = MessageBox.Show(
					"Do you want to save changes to your favorites before you logout?\n"+
						"Click Yes to save favorites and log out\n"+
						"Click No to logout without saving favorites\n"+
						"Click Cancel to cancel logout and manually close Aural Probe",
					"Save changes?",
					MessageBoxButtons.YesNoCancel,
					MessageBoxIcon.Exclamation);

				// User promises to be good and manually stop the app from now on(yeah right)
				// Cancel the logout request, app continues
				if(dr == DialogResult.Cancel)
				{
					e.Cancel = true;
				}
					// Good user! Santa will bring lots of data this year
					// Save data and logout
				else if(dr == DialogResult.Yes)
				{
					e.Cancel = !this.mainForm.SaveFavorites(); // if we cancel for some reason, don't abort program, let them save again!

				}
					// Bad user! doesn't care about poor data
				else if (dr == DialogResult.No)
				{
					e.Cancel = false;
					return;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("SystemEvents_SessionEnding " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}
	}
}