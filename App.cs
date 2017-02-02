using System;
using System.Drawing;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Win32;

namespace Aural_Probe
{
	class ColorUtils
	{
		public static void HSVtoRGB(ref float H, ref float S, ref float V, ref float R, ref float G, ref float B)
		{
			try
			{
				// Build color list from HSV values
				int Hi = ((int)(H / 60.0f)) % 6;
				float f = (H / 60.0f) - Hi;
				float p = V * (1.0f - S);
				float q = V * (1.0f - (f * S));
				float t = V * (1.0f - ((1.0f - f) * S));
				if (Hi == 0) { R = V; G = t; B = p; }
				else if (Hi == 1) { R = q; G = V; B = p; }
				else if (Hi == 2) { R = p; G = V; B = t; }
				else if (Hi == 3) { R = p; G = q; B = V; }
				else if (Hi == 4) { R = t; G = p; B = V; }
				else if (Hi == 5) { R = V; G = p; B = q; }
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("HSVtoRGB " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}
	}

	public class App
	{
		public FmodManager fmodManager;
		public FMOD.CHANNEL_CALLBACK cbFMOD = null;

		private MainForm mainForm;
		/*private ConfigurationForm configurationForm;
		private AboutForm aboutForm;
		private ProgressBar progressForm;
		private ContextMenu sampleListMenu;*/

		public string workingDirectory;
		public ConfigFile configFile;
		public FavoritesFile favoritesFile;

		public bool lbFavoritesOnly;
		public bool lbDirtyFavorites;
		public bool bUseCachedSamplesIfPossible;

		private int bitFavorite = 0;
		private int bitMissing = 1;

		public string[] sampleList;
		public int[] sampleColorIndex;
		public int[,] sampleIndices;
		public int[] sampleIndicesCount;
		public int[] sampleFavoritesCount;
		public int[] sampleBitField;

		public int listSamplesSingleSelectedIndex; // when there are multiple selections, this is -1, otherwise it's listSamples.SelectedIndex
		public int[] listSamplesLastSelectedIndices; // remember the last selected indices to properly handle ListBox item invalidation

		public static int knMaxColors = 16;
		public int nColorInc;
		public Color[,] colorList;

		public void AllocateSampleData(int nSize)
		{
			try
			{
				sampleList = new string[nSize];
				sampleColorIndex = new int[nSize];
				sampleBitField = new int[nSize];
				sampleIndices = new int[configFile.kMaxCategories,nSize];
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("AllocateSampleData " + nSize + " " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
			}
		}

		private void Init()
		{
			workingDirectory = Directory.GetCurrentDirectory();

			lbDirtyFavorites = false;

			configFile = new ConfigFile();
			configFile.Load();

			favoritesFile = new FavoritesFile();
			mainForm.configurationForm = new ConfigurationForm();
			mainForm.aboutForm = new AboutForm();
			mainForm.progressForm = new ProgressBar(mainForm);

			nColorInc = 0;
			colorList = new Color[knMaxColors,2];
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
				colorList[i, 0] = Color.FromArgb((int)(R * 255.0f), (int)(G * 255.0f), (int)(B * 255.0f));

				ColorUtils.HSVtoRGB(ref H, ref S, ref V2, ref R, ref G, ref B);
				colorList[i, 1] = Color.FromArgb((int)(R * 255.0f), (int)(G * 255.0f), (int)(B * 255.0f));
			}

			sampleIndicesCount = new int[configFile.kMaxCategories];
			sampleFavoritesCount = new int[configFile.kMaxCategories];

			lbFavoritesOnly = false;

			bUseCachedSamplesIfPossible = true; // set to true once on startup
		}

		public void InitContextMenu()
		{
			// Init context menu
			mainForm.sampleListMenu = new ContextMenu(); 
			mainForm.sampleListMenu.MenuItems.Add(
				0, 
				new MenuItem(
					"Explore...\tEnter",
					new System.EventHandler(ExploreSamples))); 
			mainForm.sampleListMenu.MenuItems.Add(
				1,
				new MenuItem(
					"Copy\tCtrl+C",
					new System.EventHandler(CopySamples)));
			mainForm.sampleListMenu.MenuItems.Add(
				2, 
				new MenuItem(
					"Copy as path\tCtrl+Shift+C",
					new System.EventHandler(CopySamplesShortcut)));
			mainForm.sampleListMenu.MenuItems.Add(
				3,
				new MenuItem(
					"Favorite\tSpace",
					new System.EventHandler(AddRemoveFromFavorites)));
			mainForm.sampleListMenu.MenuItems.Add(
				4,
				new MenuItem("Delete\tDel",
				new System.EventHandler(DeleteSamples)));

			mainForm.listSamples.ContextMenu = mainForm.sampleListMenu;
		}

		public void InitHandlers()
		{
			Application.ApplicationExit += new EventHandler(Application_ApplicationExit);
			SystemEvents.SessionEnding += new SessionEndingEventHandler(SystemEvents_SessionEnding);
			cbFMOD = new FMOD.CHANNEL_CALLBACK(MainForm.soundEndedCallback);
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
			catch (System.Exception e)
			{
				MessageBox.Show("MainForm::MainForm " + e.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		public string GetCategoryListName(int i)
		{
			try
			{
				return configFile.categoryName[i] + " (" + (lbFavoritesOnly ? sampleFavoritesCount[i] : sampleIndicesCount[i]).ToString() + ")";
			}
			catch (System.Exception e)
			{
				MessageBox.Show("GetCategoryListName " + i + " " + e.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
				return "";
			}
		}

		public bool GetSampleFlag(int sample, int bit) => (sampleBitField[sample] & (1 << bit)) != 0;

		public void SetSampleFlag(int sample, int bit, bool val)
		{
			try
			{
				if (val == true)
				{
					sampleBitField[sample] |= 1 << bit;
				}
				else
				{
					sampleBitField[sample] &= ~(1 << bit);
				}
			}
			catch (System.Exception e)
			{
				MessageBox.Show("SetSampleFlag " + e.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		public int CalculateRealSampleIndex(int listBoxIndex)
		{
			try
			{
				if (!lbFavoritesOnly)
					return listBoxIndex;
				else
				{
					int nCurrentCategory = mainForm.categoriesList.SelectedIndex;

					if (nCurrentCategory < 0 || sampleIndicesCount[nCurrentCategory] == 0)
					{
						return -1;
					}

					int nFavoriteCount = 0;
					for (int i = 0; i < sampleIndicesCount[nCurrentCategory]; ++i)
					{
						int nSampleIndex = sampleIndices[nCurrentCategory, i];
						if (GetSampleFlag(nSampleIndex, bitFavorite))
						{
							if (nFavoriteCount == listBoxIndex)
								return i;
							nFavoriteCount++;
						}
					}
				}
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("CalculateRealSampleIndex " + listBoxIndex + " " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
			return -1;
		}

		public void ExploreSamples(object sender, EventArgs e)
		{
			try
			{
				fmodManager.StopSoundPlayback();

				int nCurrentCategory = mainForm.categoriesList.SelectedIndex;
				if (nCurrentCategory < 0 || sampleIndicesCount[nCurrentCategory] == 0) return;
				if (mainForm.listSamples.SelectedIndices.Count == 0) return;
				if (
					mainForm.listSamples.SelectedIndices.Count > 1 &&
					DialogResult.No == MessageBox.Show(
						"Are you sure you want to open " + mainForm.listSamples.SelectedIndices.Count.ToString() + " explorer windows?",
						"Explore multiple samples?",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Question))
					return;

				for (int i = 0; i < mainForm.listSamples.SelectedIndices.Count; ++i)
				{
					int nCurrentSample = mainForm.CalculateRealSampleIndex(mainForm.listSamples.SelectedIndices[i]);
					if (nCurrentSample < 0) continue;

					string sampleName = mainForm.sampleList[sampleIndices[nCurrentCategory, nCurrentSample]];
					System.Diagnostics.Process proc = new System.Diagnostics.Process();
					proc.EnableRaisingEvents = false;
					proc.StartInfo.FileName = "explorer";
					proc.StartInfo.Arguments = "/n,/select," + sampleName;
					proc.Start();
				}
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("ExploreSamples " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		public void CopySamples(object sender, EventArgs e)
		{
			try
			{
				int nCurrentCategory = mainForm.categoriesList.SelectedIndex;
				if (nCurrentCategory < 0 || sampleIndicesCount[nCurrentCategory] == 0)
					return;
				if (mainForm.listSamples.SelectedIndices.Count == 0)
					return;
				DataObject objData = new DataObject();
				string[] filename = new string[mainForm.listSamples.SelectedIndices.Count];
				for (int i = 0; i < mainForm.listSamples.SelectedIndices.Count; ++i)
				{
					int nCurrentSample = CalculateRealSampleIndex(mainForm.listSamples.SelectedIndices[i]);
					if (nCurrentSample < 0)
						continue;
					filename[i] = mainForm.sampleList[sampleIndices[nCurrentCategory, nCurrentSample]];
				}
				objData.SetData(DataFormats.FileDrop, true, filename);  
				Clipboard.SetDataObject(objData, true);  
				mainForm.statusBarPanel.Text = "Copied file(s) to clipboard.";
				mainForm.statusBarPanel.ToolTipText = "";
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("CopySamples " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
			}
		}

		public void CopySamplesShortcut(object sender, EventArgs e)
		{
			try
			{
				int nCurrentCategory = mainForm.categoriesList.SelectedIndex;
				if (nCurrentCategory < 0 || sampleIndicesCount[nCurrentCategory] == 0)
					return;
				if (mainForm.listSamples.SelectedIndices.Count == 0)
					return;
				string sampleNames = "";
				for (int i = 0; i < mainForm.listSamples.SelectedIndices.Count; ++i)
				{
					int nCurrentSample = CalculateRealSampleIndex(mainForm.listSamples.SelectedIndices[i]);
					if (nCurrentSample < 0)
						continue;
					sampleNames += mainForm.sampleList[sampleIndices[nCurrentCategory, nCurrentSample]] + "\r\n";
				}
				Clipboard.SetDataObject(sampleNames,true);
				mainForm.statusBarPanel.Text = "Copied file path(s) to clipboard.";
				mainForm.statusBarPanel.ToolTipText = "";
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("CopySamplesShortcut " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
			}
		}

		public void AddRemoveFromFavorites(object sender, EventArgs e)
		{
			try
			{
				int nCurrentCategory = mainForm.categoriesList.SelectedIndex;
				if (nCurrentCategory < 0 || sampleIndicesCount[nCurrentCategory] == 0)
					return;
				if (mainForm.listSamples.SelectedIndices.Count == 0)
					return;

				int nFavoriteSample = CalculateRealSampleIndex(mainForm.listSamples.SelectedIndices[0]);
				if (nFavoriteSample < 0)
					return;
				int favoriteSampleIndex = sampleIndices[nCurrentCategory, nFavoriteSample];
				bool isFavorite = GetSampleFlag(favoriteSampleIndex, bitFavorite);

				for (int i = 0; i < mainForm.listSamples.SelectedIndices.Count; ++i)
				{
					int nCurrentSample = CalculateRealSampleIndex(mainForm.listSamples.SelectedIndices[i]);
					if (nCurrentSample < 0)
						continue;
					int sampleIndex = sampleIndices[nCurrentCategory, nCurrentSample];
					SetSampleFlag(sampleIndex, bitFavorite, !isFavorite);

					lbDirtyFavorites = true;
				}

				mainForm.UpdateSampleFavorites();

				if (lbFavoritesOnly)
				{
					mainForm.UpdateAudioSamples();
				}
				else
				{
					for (int i = 0; i < mainForm.listSamples.SelectedIndices.Count; ++i)
						mainForm.listSamples.Invalidate(mainForm.listSamples.GetItemRectangle(mainForm.listSamples.SelectedIndices[i]));
				}
				// update favorites checkbox state in popup menu
				if (listSamplesSingleSelectedIndex != -1)
				{
					int nCurrentSample = CalculateRealSampleIndex(listSamplesSingleSelectedIndex);
					if (nCurrentSample >= 0)
					{
						int sampleIndex = sampleIndices[nCurrentCategory, nCurrentSample];
						mainForm.sampleListMenu.MenuItems[3].Checked = GetSampleFlag(sampleIndex, bitFavorite);
					}
				}
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("AddRemoveFromFavorites " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
			}
		}

		public void DeleteSamples(object sender, EventArgs e)
		{
			fmodManager.StopSoundPlayback();

			try
			{
				int nCurrentCategory = mainForm.categoriesList.SelectedIndex;
				if (nCurrentCategory < 0 || sampleIndicesCount[nCurrentCategory] == 0) return;
				if (mainForm.listSamples.SelectedIndices.Count == 0) return;
				else if (mainForm.listSamples.SelectedIndices.Count == 1)
				{
					if (DialogResult.No == MessageBox.Show(
						"Are you sure you want to permanently delete this sample from your computer? Deleted samples will remain in the samples list until you rescan search folders.",
						"Delete sample?",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Question,
						MessageBoxDefaultButton.Button2))
						return;
				}
				else if (mainForm.listSamples.SelectedIndices.Count > 1)
				{
					if (DialogResult.No == MessageBox.Show(
						"Are you sure you want to permanently delete " + mainForm.listSamples.SelectedIndices.Count.ToString() + " samples from your computer? " +
							"Deleted samples will remain in the samples list until you rescan search folders.",
						"Delete multiple samples?",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Question,
						MessageBoxDefaultButton.Button2))
						return;
				}
				string deleteErrors = "";
				for (int i = 0; i < mainForm.listSamples.SelectedIndices.Count; ++i)
				{
					int nCurrentSample = CalculateRealSampleIndex(mainForm.listSamples.SelectedIndices[i]);
					if (nCurrentSample < 0) continue;
					string sampleName = mainForm.sampleList[sampleIndices[nCurrentCategory, nCurrentSample]];
					try
					{
						File.Delete(sampleName);
						SetSampleFlag(nCurrentSample, bitMissing, true);
						mainForm.listSamples.Invalidate(mainForm.listSamples.GetItemRectangle(mainForm.listSamples.SelectedIndices[i]));
					}
					catch (System.Exception ex)
					{
						deleteErrors += ex.Message.ToString() + "\n";
					}
				}

				if (deleteErrors != "")
					MessageBox.Show("One or more errors were encountered during delete:\n" + deleteErrors, "Delete operation completed with errors", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("DeleteSamples " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		public void Application_ApplicationExit(object sender, EventArgs e) 
		{
			try
			{
				if (!lbDirtyFavorites || sampleFavoritesCount[0] == 0)
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
					mainForm.SaveFavorites(); // if we cancel for some reason, don't abort program, let them save again!
				}
				else if (dr == DialogResult.No)
				{
					return;
				}
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("Application_ApplicationExit " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		public void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e) 
		{
			try
			{
				if (!lbDirtyFavorites || sampleFavoritesCount[0] == 0) return;

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
					e.Cancel = !mainForm.SaveFavorites(); // if we cancel for some reason, don't abort program, let them save again!

				}
					// Bad user! doesn't care about poor data
				else if (dr == DialogResult.No)
				{
					e.Cancel = false;
					return;
				}
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("SystemEvents_SessionEnding " + ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}
	}
}