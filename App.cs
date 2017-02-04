using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace Aural_Probe
{
	public class App
	{
		public FmodManager fmodManager;
		public FMOD.CHANNEL_CALLBACK cbFMOD;

		private MainForm mainForm;

		public bool lbFavoritesOnly => this.mainForm.lbFavoritesOnly;
		public bool lbDirtyFavorites;
		public bool bUseCachedSamplesIfPossible;

		public Library Library;
		public Category CurrentCategory;
		public List<Sample> Selection;
		public Files Files;

		public static int knMaxColors = 16;
		public int nColorInc;
		public Color[,] colorList;

		public App(MainForm mainForm)
		{
			this.Library = new Library(this);
			this.Files = new Files();
			this.Selection = new List<Sample>();
		}

		private void Init()
		{
			this.Files.WorkingDirectory = Directory.GetCurrentDirectory();
			this.Files.ConfigFile = ConfigFile.Load();

			var a = JsonConvert.SerializeObject(this.Files.ConfigFile, Formatting.Indented);

			this.lbDirtyFavorites = false;

			this.Files.FavoritesFile = new FavoritesFile();
			this.mainForm.configurationForm = new ConfigurationForm();
			this.mainForm.aboutForm = new AboutForm();
			this.mainForm.progressForm = new ProgressBar(this.mainForm);

			this.nColorInc = 0;
			this.colorList = new Color[knMaxColors,2];
			for (var i = 0; i < knMaxColors; ++i)
			{
				var H = i * (360.0f / knMaxColors);
				var S = 0.25f;
				var V1 = 0.5f;
				var V2 = 1.0f;
				var R = 0.0f;
				var G = 0.0f;
				var B = 0.0f;

				ColorUtils.HSVtoRGB(ref H, ref S, ref V1, ref R, ref G, ref B);
				this.colorList[i, 0] = Color.FromArgb((int)(R * 255.0f), (int)(G * 255.0f), (int)(B * 255.0f));

				ColorUtils.HSVtoRGB(ref H, ref S, ref V2, ref R, ref G, ref B);
				this.colorList[i, 1] = Color.FromArgb((int)(R * 255.0f), (int)(G * 255.0f), (int)(B * 255.0f));
			}

			this.mainForm.lbFavoritesOnly = false;

			this.bUseCachedSamplesIfPossible = true; // set to true once on startup
		}

		private void InitContextMenu()
		{
			// Init context menu
			this.mainForm.sampleListMenu = new ContextMenu();
			this.mainForm.sampleListMenu.MenuItems.Add(
				0, 
				new MenuItem("Explore...\tEnter", this.ExploreSamples));
			this.mainForm.sampleListMenu.MenuItems.Add(
				1,
				new MenuItem("Copy\tCtrl+C", this.CopySamples));
			this.mainForm.sampleListMenu.MenuItems.Add(
				2, 
				new MenuItem("Copy as path\tCtrl+Shift+C", this.CopySamplesShortcut));
			this.mainForm.sampleListMenu.MenuItems.Add(
				3,
				new MenuItem("Favorite\tSpace", this.AddRemoveFromFavorites));
			this.mainForm.sampleListMenu.MenuItems.Add(
				4,
				new MenuItem("Delete\tDel", this.DeleteSamples));

			this.mainForm.listSamples.ContextMenu = this.mainForm.sampleListMenu;
		}

		private void InitHandlers()
		{
			Application.ApplicationExit += this.Application_ApplicationExit;
			SystemEvents.SessionEnding += this.SystemEvents_SessionEnding;
			this.cbFMOD = this.mainForm.SoundEndedCallback;
		}

		public void foo(MainForm mainForm)
		{
			this.mainForm = mainForm;

			try
			{
				this.Init();
				this.InitContextMenu();
				this.fmodManager = new FmodManager(this);
				this.InitHandlers();
			}
			catch (Exception e)
			{
				MessageBox.Show(
					"MainForm::MainForm " + e,
					"Error!",
					MessageBoxButtons.OK,
					MessageBoxIcon.Exclamation);
			}
		}

		public void ExploreSamples(object sender, EventArgs e)
		{
			try
			{
				this.fmodManager.Stop();

				if (this.CurrentCategory.Samples.Count == 0)
				{
					return;
				}

				if (this.Selection.Count == 0)
				{
					return;
				}

				var confirmOpenMany = DialogResult.Yes == MessageBox.Show(
						"Are you sure you want to open " + this.mainForm.listSamples.SelectedIndices.Count + " explorer windows?",
						"Explore multiple samples?",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Question);

				if (this.Selection.Count > 1 && !confirmOpenMany)
				{
					return;
				}

				foreach (var sample in this.Selection)
				{
					var process = new System.Diagnostics.Process
					{
						EnableRaisingEvents = false,
						StartInfo = {
							FileName = "explorer",
							Arguments = "/n,/select," + sample.Name
						}
					};
					process.Start();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("ExploreSamples " + ex, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		public void CopySamples(object sender, EventArgs e)
		{
			try
			{
				if (this.CurrentCategory.Samples.Count == 0)
				{
					return;
				}

				if (this.mainForm.listSamples.SelectedIndices.Count == 0)
				{
					return;
				}

				var objData = new DataObject();

				objData.SetData(
					DataFormats.FileDrop,
					true,
					this.Selection.Select(sample => sample.Path).ToArray());

				Clipboard.SetDataObject(objData, true);
				this.mainForm.statusBarPanel.Text = "Copied file(s) to clipboard.";
				this.mainForm.statusBarPanel.ToolTipText = "";
			}
			catch (Exception ex)
			{
				MessageBox.Show("CopySamples " + ex, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
			}
		}

		public void CopySamplesShortcut(object sender, EventArgs e)
		{
			try
			{
				if (this.CurrentCategory.Samples.Count == 0)
				{
					return;
				}

				if (this.mainForm.listSamples.SelectedIndices.Count == 0)
				{
					return;
				}

				Clipboard.SetDataObject(
					string.Join("\rn\n", this.Selection.Select(sample => sample.Path)),
					true);
				this.mainForm.statusBarPanel.Text = "Copied file path(s) to clipboard.";
				this.mainForm.statusBarPanel.ToolTipText = "";
			}
			catch (Exception ex)
			{
				MessageBox.Show("CopySamplesShortcut " + ex, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
			}
		}

		public void AddRemoveFromFavorites(object sender, EventArgs e)
		{
			try
			{
				var sample = (Sample)this.mainForm.listSamples.SelectedItem;
				sample.Favorited = !sample.Favorited;
				this.lbDirtyFavorites = true;

				this.mainForm.UpdateSampleFavorites();

				if (this.lbFavoritesOnly)
				{
					this.mainForm.UpdateAudioSamples();
				}
				else
				{
					this.mainForm.listSamples_RedrawSample(sample);
				}

				// update favorites checkbox state in popup menu
				this.mainForm.sampleListMenu.MenuItems[3].Checked = sample.Favorited;
			}
			catch (Exception ex)
			{
				MessageBox.Show("AddRemoveFromFavorites " + ex, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
			}
		}

		public void DeleteSamples(object sender, EventArgs e)
		{
			this.fmodManager.Stop();

			try
			{
				if (this.CurrentCategory.Samples.Count == 0)
				{
					return;
				}

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
						"Are you sure you want to permanently delete " + this.mainForm.listSamples.SelectedIndices.Count + " samples from your computer? " +
							"Deleted samples will remain in the samples list until you rescan search folders.",
						"Delete multiple samples?",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Question,
						MessageBoxDefaultButton.Button2))
						return;
				}

				var deleteErrors = "";

				foreach (var sample in this.mainForm.listSamples.SelectedItems)
				{
				}

				foreach (var sample in this.mainForm.listSamples.SelectedItems.Cast<Sample>())
				{
					try
					{
						File.Delete(sample.Name);
						sample.Missing = true;
						this.mainForm.listSamples_RedrawSample(sample);
					}
					catch (Exception ex)
					{
						deleteErrors += ex.Message + "\n";
					}
				}

				if (deleteErrors != "")
					MessageBox.Show("One or more errors were encountered during delete:\n" + deleteErrors, "Delete operation completed with errors", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
			catch (Exception ex)
			{
				MessageBox.Show("DeleteSamples " + ex, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		private void Application_ApplicationExit(object sender, EventArgs e) 
		{
			try
			{
				if (!this.lbDirtyFavorites || this.Library.FavoriteCount == 0)
				{
					return;
				}

				// User is trying to quit. Prompt the user with choices
				var saveFavorites = MessageBox.Show(
					"Do you want to save changes to your favorites?\n",
					"Save changes?",
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Question);

				if (saveFavorites == DialogResult.Yes)
				{
					this.mainForm.SaveFavorites(); // if we cancel for some reason, don't abort program, let them save again!
				}
				else if (saveFavorites == DialogResult.No)
				{
					return;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Application_ApplicationExit " + ex, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		private void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e) 
		{
			try
			{
				if (!this.lbDirtyFavorites || this.Library.FavoriteCount == 0)
				{
					return;
				}

				// User is trying to log out. Prompt the user with choices
				var saveFavorites = MessageBox.Show(
					"Do you want to save changes to your favorites before you logout?\n"+
						"Click Yes to save favorites and log out\n"+
						"Click No to logout without saving favorites\n"+
						"Click Cancel to cancel logout and manually close Aural Probe",
					"Save changes?",
					MessageBoxButtons.YesNoCancel,
					MessageBoxIcon.Exclamation);

				// User promises to be good and manually stop the app from now on(yeah right)
				// Cancel the logout request, app continues
				if(saveFavorites == DialogResult.Cancel)
				{
					e.Cancel = true;
				}
					// Good user! Santa will bring lots of data this year
					// Save data and logout
				else if(saveFavorites == DialogResult.Yes)
				{
					e.Cancel = !this.mainForm.SaveFavorites(); // if we cancel for some reason, don't abort program, let them save again!

				}
					// Bad user! doesn't care about poor data
				else if (saveFavorites == DialogResult.No)
				{
					e.Cancel = false;
					return;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("SystemEvents_SessionEnding " + ex, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}
	}
}