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
	/// <summary>
	/// Summary description for Form1.
	/// </summary>

	public class MainForm : Form
	{
		private StatusBar statusBar;
		public StatusBarPanel statusBarPanel;

		public static FMOD.Sound sound;
		private FMOD.Channel channel;
		private bool bAutoPlayNextSample;
		private int nAutoPlayRepeatsLeft;
		private FMOD.CHANNEL_CALLBACK cbFMOD = null;

		private PersistWindowState windowState;

		private const string SampleCacheFilename = "cache.dat";
		private const int VersionedSampleCacheID = 1;

		public static ConfigFile configFile => app.Files.ConfigFile;
		
		public static int knMaxColors = 16;
		public int nColorInc;

		private bool bUseCachedSamplesIfPossible;

		private string forceLoadFavoritesName = "";
		public int lnSamples;
		private bool lbDontPlayNextSample;
		public string[] sampleList => app.Library.sampleList;
		public int[] sampleColorIndex => app.Library.sampleColorIndex;
		public int[,] sampleIndices => app.Library.sampleIndices;
		public int[] sampleIndicesCount => app.Library.sampleIndicesCount;
		private int[] sampleBitField => app.Library.sampleBitField;

		private ToolBarButton toolBarButtonLoadFavorites;
		private ToolBarButton toolBarButtonSaveFavorites;
		private ToolBarButton toolBarButtonResetFavorites;
		private ToolBarButton toolBarButton2;
		private ToolBarButton toolBarButton3;
		private ToolBarButton toolBarButtonPlayStop;
		private System.Timers.Timer timer;
		public TrackBar trackBarMasterVol;
		private Label labelVolume;
		private Label labelVolumeValue;
		private NotifyIcon notifyIcon1;
		private ContextMenu contextMenuNotify;
		private MenuItem menuItem1;
		private MenuItem menuItem2;

		private const int BitFavorite = 0;
		private const int BitMissing = 1;

		private int listSamplesSingleSelectedIndex; // when there are multiple selections, this is -1, otherwise it's listSamples.SelectedIndex
		private int[] listSamplesLastSelectedIndices; // remember the last selected indices to properly handle ListBox item invalidation

		public static App app;
		public static string workingDirectory => app.Files.WorkingDirectory;

		private bool lbFavoritesOnly;
		private bool lbDirtyFavorites;

		public ConfigurationForm configurationForm;
		public AboutForm aboutForm;
		public ProgressBar progressForm;

		private ToolBar toolBar1;
		public ListBox categoriesList;
		private ToolBarButton toolBarButtonConfiguration;
		private ToolBarButton toolBarButton;
		private ToolBarButton toolBarButtonRescanFolders;
		private ToolBarButton toolBarButton1;
		private ToolBarButton toolBarButtonAbout;
		private ImageList imageList1;
		private ToolBarButton toolBarButtonHelp;
		private PictureBox pictureStatus;
		private Label statusLabel;
		//private SamplesListBox listSamples;
		public ListBox listSamples;
		private IContainer components;
		private StatusBarPanel statusBarProperties;
		private ToolBarButton toolBarButtonFavoritesOnly;
		private SplitContainer splitContainer2;
		private SplitContainer splitContainer1;
		private ListBox tagsList;
		public ContextMenu sampleListMenu;  

		public MainForm()
		{
			// Required for Windows Form Designer support
			this.InitializeComponent();

			this.windowState = new PersistWindowState(this);
			this.windowState.Parent = this;
			this.windowState.RegistryPath = @"Software\Aural Probe"; 

			try
			{
				app = new App(this);
				app.foo(this); // what the fuck why can't I just put this in the ctor of App? haha

				this.UpdateFormWithConfigurationData();
				this.Show();
				this.Refresh();

				this.RefreshForm();
			}
			catch (Exception e)
			{
				MessageBox.Show("MainForm::MainForm " + e, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);				
			}
			return;
		}

		private bool UpdateFavoriteDataFromFavoritesFile(ref FavoritesFile file, bool bShowWarning)
		{
			try
			{
				if (this.lnSamples == 0 && file.bLoaded)
				{
					if (bShowWarning)
					{
						MessageBox.Show(
							"You must scan the search folders for samples before trying to load favorites.",
							"No samples loaded!",
							MessageBoxButtons.OK,
							MessageBoxIcon.Information);
					}
					file.Reset(0);
					return false;
				}

				var bError = false;
				var errorMessage = "The following favorites could not be located:\n\n";

				for (var i = 0; i < this.lnSamples; ++i)
				{
					app.SetSampleFlag(i, BitFavorite, false);
				}

				for (var i = 0; i < file.nFavorites; ++i)
				{
					// See if we can quickly find favorite
					if (file.favoriteIndex[i] < this.lnSamples && file.favoriteName[i] == this.sampleList[file.favoriteIndex[i]])
					{
						// we have a winner!
						app.SetSampleFlag(file.favoriteIndex[i], BitFavorite, true);
					}
					else
					{
						// we have to find it manually as the index is a dud
						var j = 0;
						for (; j < this.lnSamples; ++j)
						{
							if (this.sampleList[j] == file.favoriteName[i])
							{
								// we found it!
								app.SetSampleFlag(file.favoriteIndex[i], BitFavorite, true);
								break;
							}
						}
						if (j == this.lnSamples) // we didn't find it!
						{
							errorMessage += file.favoriteName[i] + "\n";
							bError = true;
						}
					}
				}

				if (bError)
				{
					errorMessage += "\nThese samples have either been moved to a new location or deleted.";
					MessageBox.Show(
						errorMessage,
						"Some favorites missing!",
						MessageBoxButtons.OK,
						MessageBoxIcon.Exclamation);
				}

				return true;
			}
			catch (Exception e)
			{
				MessageBox.Show("UpdateFavoriteDataFromFavoritesFile " + e, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
				return false;
			}
		}

		private void UpdateFavoritesFileFromFavoriteData(ref FavoritesFile file, bool bShowWarning)
		{
			try
			{
				if (app.Library.sampleFavoritesCount[0] == 0)
				{
					if (bShowWarning)
					{
						MessageBox.Show(
							"There are no favorites to save.",
							"Nothing to save!",
							MessageBoxButtons.OK,
							MessageBoxIcon.Information);
					}

					return;
				}

				file.Reset(app.Library.sampleFavoritesCount[0]);
				var favoriteIndex = 0;

				for (var i = 0; i < this.lnSamples; ++i)
				{
					if (app.GetSampleFlag(i, BitFavorite))
					{
						file.favoriteIndex[favoriteIndex] = i;
						file.favoriteName[favoriteIndex] = this.sampleList[i];
						favoriteIndex++;
					}
				}
			}
			catch (Exception e)
			{
				MessageBox.Show("UpdateFavoritesFileFromFavoriteData " + e, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
			}
		}

		public void UpdateSampleFavorites()
		{
			try
			{
				app.Library.UpdateFavoriteCounts();

				for (var i = 0; i < configFile.Categories.Count; ++i)
				{
					app.Library.sampleFavoritesCount[i] = 0;

					for (var j = 0; j < this.sampleIndicesCount[i]; ++j)
					{
						var index = this.sampleIndices[i,j];
						if (app.GetSampleFlag(index, BitFavorite))
						{
							app.Library.sampleFavoritesCount[i]++;
						}
					}
				}

				this.UpdateTitleBarText();
				this.UpdateCategoryList();
			}
			catch (Exception e)
			{
				MessageBox.Show("UpdateSampleFavorites " + e, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}
		
		public void UpdateTitleBarText()
		{
			try
			{
				var title = "Aural Probe";
				var splitFilename = app.Files.FavoritesFile.currentFavoritesFilename.Split('\\');

				if (app.Files.FavoritesFile.bLoaded)
				{
					title += " - " + splitFilename[splitFilename.Length-1];
				}
				else
				{
					title += " - Untitled";
				}

				if (this.lbDirtyFavorites)
				{
					title += "*";
				}

				this.Text = title;
			}
			catch (Exception e)
			{
				MessageBox.Show("UpdateTitleBarText " + e, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
			}
		}

		public void UpdateAudioSamples()
		{
			try
			{
				var nCurrentCategory = this.categoriesList.SelectedIndex;
				if (nCurrentCategory < 0 || this.sampleIndicesCount[nCurrentCategory] == 0)
					return;
				var nCount = this.sampleIndicesCount[nCurrentCategory];
				this.listSamples.Items.Clear();
				for (var i = 0; i < nCount; ++i)
				{
					var nSampleIndex = this.sampleIndices[nCurrentCategory, i];
					if (!this.lbFavoritesOnly || app.GetSampleFlag(nSampleIndex, BitFavorite))
					{
						this.listSamples.Items.Add(i.ToString());
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("UpdateAudioSamples " + ex, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
			}
		}

		private void UpdateCategoryList()
		{
			try
			{
				if (this.lbFavoritesOnly)
				{
					for (var i = 0; i < configFile.Categories.Count; ++i)
						this.categoriesList.Items[i] = app.GetCategoryListName(i);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("UpdateCategoryList " + ex, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
			}
		}

		public void UpdateFormWithConfigurationData()
		{
			try
			{
				this.TopMost = configFile.AlwaysOnTop;

				this.bAutoPlayNextSample = false; // this gets set when playing a sound
				this.nAutoPlayRepeatsLeft = 0; // this gets set when playing a sound

				// Update the category list
				var oldIndex = this.categoriesList.SelectedIndex;
				var oldCategoriesSize = this.categoriesList.Items.Count;

				this.categoriesList.Items.Clear();
				for (var i = 0; i < configFile.Categories.Count; ++i)
					this.categoriesList.Items.Add(app.GetCategoryListName(i));

				if (oldCategoriesSize == this.categoriesList.Items.Count)
					this.categoriesList.SelectedIndex = oldIndex;

				this.UpdateStatusBarAndLabel();
				this.listSamples.ItemHeight = configFile.SampleDisplaySizeH;
				this.listSamples.ColumnWidth = configFile.SampleDisplaySizeW;

				//sampleListMenu.MenuItems[3].Enabled = !lbFavoritesOnly; // disable favorite editing when in favorites view

				this.UpdateVolume();
			}
			catch (Exception e)
			{
				MessageBox.Show("UpdateFormWithConfigurationData " + e, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		public void UpdateVolume()
		{
			var volumePercentage = this.trackBarMasterVol.Value / 100.0f;

			if (volumePercentage <= 0f)
			{
				this.labelVolumeValue.Text = "Off";
			}
			else
			{
				this.labelVolumeValue.Text = (20f * Math.Log10(volumePercentage)).ToString("F1") + " dB";
			}

			FMOD.ChannelGroup group = null;
			var result = app.fmodManager.SystemFmod.getMasterChannelGroup(ref group);
			group.setVolume(volumePercentage);

		}

		public void UpdateStatusBarAndLabel()
		{
			try
			{
				this.listSamples.Items.Clear();
				this.statusBarPanel.ToolTipText = "";
			
				if (configFile.SearchDirectories.Count <= 0)
				{
					this.pictureStatus.Visible = true;
					this.statusLabel.Visible = true;
					this.statusLabel.Text = "You must configure your search folders.";
					this.statusBarPanel.Text = "Ready";
					this.statusBarProperties.Text = "";
				}
				else if (configFile.Categories.Count <= 0)
				{
					this.pictureStatus.Visible = true;
					this.statusLabel.Visible = true;
					this.statusLabel.Text = "You must configure your audio sample categories.";
					this.statusBarPanel.Text = "No categories found";
					this.statusBarProperties.Text = "";
				}
				else if (this.lnSamples <= 0)
				{
					this.pictureStatus.Visible = true;
					this.statusLabel.Visible = true;
					this.statusLabel.Text = "No audio samples found.";
					this.statusBarPanel.Text = "No audio samples found";
					this.statusBarProperties.Text = "";
				}
				else if (this.categoriesList.SelectedIndex == -1)
				{
					this.pictureStatus.Visible = true;
					this.statusLabel.Visible = true;
					this.statusLabel.Text = "Select a category from the left.";
					this.statusBarPanel.Text = this.lnSamples + " sample(s)";
					this.statusBarProperties.Text = "";
				}
				else
				{
					this.pictureStatus.Visible = false;
					this.statusLabel.Visible = false;
					this.statusBarPanel.Text = (this.lbFavoritesOnly
						? app.Library.sampleFavoritesCount[this.categoriesList.SelectedIndex]
						: this.sampleIndicesCount[this.categoriesList.SelectedIndex]) + "/" + this.lnSamples + " sample(s)";
					this.statusBarProperties.Text = "";
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("UpdateStatusBarAndLabel " + ex, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		public void ClearSamples()
		{
			try
			{
				for (var i = 0; i < ConfigFile.MaxCategories; ++i)
				{
					this.sampleIndicesCount[i] = 0;
				}
				for (var i = 0; i < this.lnSamples; ++i)
				{
					this.sampleList[i] = ""; // do I need to do this? :/
					this.sampleBitField[i] = 0;
				}
				this.lnSamples = 0;
			}
			catch (Exception ex)
			{
				MessageBox.Show("ClearSamples " + ex, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		public bool PopulateCategoriesWithAudioSamples(bool bUseCache)
		{
			try
			{
				var bWantsToUseCache = bUseCache;
				bUseCache = bUseCache && File.Exists(GetSampleCacheFilepath());
				if (configFile.SearchDirectories.Count == 0)
				{
					if (!bWantsToUseCache) // only show this message if we're not trying to use the cache, ie. a fresh install
					{
						MessageBox.Show(
							"You must configure your search folders.",
							"No search folders found",
							MessageBoxButtons.OK,
							MessageBoxIcon.Information);
					}
					return false;
				}
				else if (configFile.Categories.Count == 0)
				{
					if (!bWantsToUseCache) // only show this message if we're not trying to use the cache, ie. a fresh install
						MessageBox.Show("You must configure your audio sample categories.", "No categories found", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return false;
				}
				else 
				{
					if (bUseCache)
					{
						this.statusLabel.Text = "Please wait. Loading audio samples from cache...";
						this.Refresh();
						this.progressForm.Restart(bUseCache);
						var result = this.progressForm.ShowDialog(this);
					}
					else
					{
						if (!configFile.RescanPrompt || DialogResult.Yes == MessageBox.Show("Would you like to scan all search folders for audio samples now?", "Scan search folders for audio samples?", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
						{
							this.statusLabel.Text = "Please wait. Scanning folders for audio samples...";
							this.Refresh();
							this.progressForm.Restart(bUseCache);
							var result = this.progressForm.ShowDialog(this);
							this.SaveAudioSampleCache();
						}
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				MessageBox.Show("PopulateCategoriesWithAudioSamples " + ex, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);				
				return false;
			}
		}

		static public string GetApplicationDataPath()
		{
			var applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Aural Probe";
			return applicationDataPath;
		}

		static public string GetSampleCacheFilepath()
		{
			return GetApplicationDataPath() + "\\" + SampleCacheFilename;
		}

		public bool SaveAudioSampleCache()
		{
			try
			{
				Directory.CreateDirectory(GetApplicationDataPath());
				using(Stream myFileStream = File.OpenWrite(GetSampleCacheFilepath()))
				{
					var serializer = new BinaryFormatter();
					serializer.Serialize(myFileStream, VersionedSampleCacheID);
					serializer.Serialize(myFileStream, this.lnSamples);
					for (var i = 0; i < this.lnSamples; ++i)
					{
						serializer.Serialize(myFileStream, this.sampleList[i]);
						serializer.Serialize(myFileStream, this.sampleColorIndex[i]);
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error! Could not save sample cache! " + ex, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				try { File.Delete(GetSampleCacheFilepath()); } 
				catch
				{
					MessageBox.Show("Error! Could not delete sample cache! " + ex, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
				return false;
			}
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (this.components != null) 
				{
					this.components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			var resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.statusBar = new System.Windows.Forms.StatusBar();
			this.statusBarPanel = new System.Windows.Forms.StatusBarPanel();
			this.statusBarProperties = new System.Windows.Forms.StatusBarPanel();
			this.toolBar1 = new System.Windows.Forms.ToolBar();
			this.toolBarButtonRescanFolders = new System.Windows.Forms.ToolBarButton();
			this.toolBarButton2 = new System.Windows.Forms.ToolBarButton();
			this.toolBarButtonFavoritesOnly = new System.Windows.Forms.ToolBarButton();
			this.toolBarButtonLoadFavorites = new System.Windows.Forms.ToolBarButton();
			this.toolBarButtonSaveFavorites = new System.Windows.Forms.ToolBarButton();
			this.toolBarButtonResetFavorites = new System.Windows.Forms.ToolBarButton();
			this.toolBarButton = new System.Windows.Forms.ToolBarButton();
			this.toolBarButtonPlayStop = new System.Windows.Forms.ToolBarButton();
			this.toolBarButton1 = new System.Windows.Forms.ToolBarButton();
			this.toolBarButtonConfiguration = new System.Windows.Forms.ToolBarButton();
			this.toolBarButton3 = new System.Windows.Forms.ToolBarButton();
			this.toolBarButtonHelp = new System.Windows.Forms.ToolBarButton();
			this.toolBarButtonAbout = new System.Windows.Forms.ToolBarButton();
			this.imageList1 = new System.Windows.Forms.ImageList(this.components);
			this.statusLabel = new System.Windows.Forms.Label();
			this.pictureStatus = new System.Windows.Forms.PictureBox();
			this.listSamples = new System.Windows.Forms.ListBox();
			this.categoriesList = new System.Windows.Forms.ListBox();
			this.timer = new System.Timers.Timer();
			this.trackBarMasterVol = new System.Windows.Forms.TrackBar();
			this.labelVolume = new System.Windows.Forms.Label();
			this.labelVolumeValue = new System.Windows.Forms.Label();
			this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
			this.contextMenuNotify = new System.Windows.Forms.ContextMenu();
			this.menuItem1 = new System.Windows.Forms.MenuItem();
			this.menuItem2 = new System.Windows.Forms.MenuItem();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.splitContainer2 = new System.Windows.Forms.SplitContainer();
			this.tagsList = new System.Windows.Forms.ListBox();
			((System.ComponentModel.ISupportInitialize)(this.statusBarPanel)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.statusBarProperties)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureStatus)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.timer)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBarMasterVol)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
			this.splitContainer2.Panel1.SuspendLayout();
			this.splitContainer2.Panel2.SuspendLayout();
			this.splitContainer2.SuspendLayout();
			this.SuspendLayout();
			// 
			// statusBar
			// 
			this.statusBar.Location = new System.Drawing.Point(0, 299);
			this.statusBar.Name = "statusBar";
			this.statusBar.Panels.AddRange(new System.Windows.Forms.StatusBarPanel[] {
            this.statusBarPanel,
            this.statusBarProperties});
			this.statusBar.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this.statusBar.ShowPanels = true;
			this.statusBar.Size = new System.Drawing.Size(632, 24);
			this.statusBar.TabIndex = 0;
			this.statusBar.Text = "Ready";
			this.statusBar.DrawItem += new System.Windows.Forms.StatusBarDrawItemEventHandler(this.statusBar_DrawItem);
			// 
			// statusBarPanel
			// 
			this.statusBarPanel.AutoSize = System.Windows.Forms.StatusBarPanelAutoSize.Spring;
			this.statusBarPanel.Name = "statusBarPanel";
			this.statusBarPanel.Style = System.Windows.Forms.StatusBarPanelStyle.OwnerDraw;
			this.statusBarPanel.Width = 385;
			// 
			// statusBarProperties
			// 
			this.statusBarProperties.Name = "statusBarProperties";
			this.statusBarProperties.Width = 230;
			// 
			// toolBar1
			// 
			this.toolBar1.Appearance = System.Windows.Forms.ToolBarAppearance.Flat;
			this.toolBar1.AutoSize = false;
			this.toolBar1.Buttons.AddRange(new System.Windows.Forms.ToolBarButton[] {
            this.toolBarButtonRescanFolders,
            this.toolBarButton2,
            this.toolBarButtonFavoritesOnly,
            this.toolBarButtonLoadFavorites,
            this.toolBarButtonSaveFavorites,
            this.toolBarButtonResetFavorites,
            this.toolBarButton,
            this.toolBarButtonPlayStop,
            this.toolBarButton1,
            this.toolBarButtonConfiguration,
            this.toolBarButton3,
            this.toolBarButtonHelp,
            this.toolBarButtonAbout});
			this.toolBar1.ButtonSize = new System.Drawing.Size(16, 16);
			this.toolBar1.DropDownArrows = true;
			this.toolBar1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.toolBar1.ImageList = this.imageList1;
			this.toolBar1.Location = new System.Drawing.Point(0, 0);
			this.toolBar1.Name = "toolBar1";
			this.toolBar1.ShowToolTips = true;
			this.toolBar1.Size = new System.Drawing.Size(632, 38);
			this.toolBar1.TabIndex = 1;
			this.toolBar1.ButtonClick += new System.Windows.Forms.ToolBarButtonClickEventHandler(this.toolBar1_ButtonClick);
			// 
			// toolBarButtonRescanFolders
			// 
			this.toolBarButtonRescanFolders.ImageIndex = 2;
			this.toolBarButtonRescanFolders.Name = "toolBarButtonRescanFolders";
			this.toolBarButtonRescanFolders.Text = "Rescan";
			this.toolBarButtonRescanFolders.ToolTipText = "Rescan Search Folders";
			// 
			// toolBarButton2
			// 
			this.toolBarButton2.Name = "toolBarButton2";
			this.toolBarButton2.Style = System.Windows.Forms.ToolBarButtonStyle.Separator;
			// 
			// toolBarButtonFavoritesOnly
			// 
			this.toolBarButtonFavoritesOnly.ImageIndex = 4;
			this.toolBarButtonFavoritesOnly.Name = "toolBarButtonFavoritesOnly";
			this.toolBarButtonFavoritesOnly.Style = System.Windows.Forms.ToolBarButtonStyle.ToggleButton;
			this.toolBarButtonFavoritesOnly.Text = "Favorites";
			this.toolBarButtonFavoritesOnly.ToolTipText = "View Favorites";
			// 
			// toolBarButtonLoadFavorites
			// 
			this.toolBarButtonLoadFavorites.ImageIndex = 5;
			this.toolBarButtonLoadFavorites.Name = "toolBarButtonLoadFavorites";
			this.toolBarButtonLoadFavorites.Text = "Open...";
			this.toolBarButtonLoadFavorites.ToolTipText = "Open Favorites...";
			// 
			// toolBarButtonSaveFavorites
			// 
			this.toolBarButtonSaveFavorites.ImageIndex = 6;
			this.toolBarButtonSaveFavorites.Name = "toolBarButtonSaveFavorites";
			this.toolBarButtonSaveFavorites.Text = "Save...";
			this.toolBarButtonSaveFavorites.ToolTipText = "Save Favorites...";
			// 
			// toolBarButtonResetFavorites
			// 
			this.toolBarButtonResetFavorites.ImageIndex = 9;
			this.toolBarButtonResetFavorites.Name = "toolBarButtonResetFavorites";
			this.toolBarButtonResetFavorites.Text = "Reset";
			this.toolBarButtonResetFavorites.ToolTipText = "Reset Favorites";
			// 
			// toolBarButton
			// 
			this.toolBarButton.Name = "toolBarButton";
			this.toolBarButton.Style = System.Windows.Forms.ToolBarButtonStyle.Separator;
			// 
			// toolBarButtonPlayStop
			// 
			this.toolBarButtonPlayStop.Enabled = false;
			this.toolBarButtonPlayStop.ImageIndex = 7;
			this.toolBarButtonPlayStop.Name = "toolBarButtonPlayStop";
			this.toolBarButtonPlayStop.Text = "Play";
			// 
			// toolBarButton1
			// 
			this.toolBarButton1.Name = "toolBarButton1";
			this.toolBarButton1.Style = System.Windows.Forms.ToolBarButtonStyle.Separator;
			// 
			// toolBarButtonConfiguration
			// 
			this.toolBarButtonConfiguration.ImageIndex = 3;
			this.toolBarButtonConfiguration.Name = "toolBarButtonConfiguration";
			this.toolBarButtonConfiguration.Text = "Config...";
			this.toolBarButtonConfiguration.ToolTipText = "Configuration...";
			// 
			// toolBarButton3
			// 
			this.toolBarButton3.Name = "toolBarButton3";
			this.toolBarButton3.Style = System.Windows.Forms.ToolBarButtonStyle.Separator;
			// 
			// toolBarButtonHelp
			// 
			this.toolBarButtonHelp.ImageIndex = 1;
			this.toolBarButtonHelp.Name = "toolBarButtonHelp";
			this.toolBarButtonHelp.Text = "Help";
			this.toolBarButtonHelp.ToolTipText = "Help...";
			// 
			// toolBarButtonAbout
			// 
			this.toolBarButtonAbout.ImageIndex = 0;
			this.toolBarButtonAbout.Name = "toolBarButtonAbout";
			this.toolBarButtonAbout.Text = "About";
			this.toolBarButtonAbout.ToolTipText = "About...";
			// 
			// imageList1
			// 
			this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
			this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
			this.imageList1.Images.SetKeyName(0, "");
			this.imageList1.Images.SetKeyName(1, "");
			this.imageList1.Images.SetKeyName(2, "");
			this.imageList1.Images.SetKeyName(3, "");
			this.imageList1.Images.SetKeyName(4, "");
			this.imageList1.Images.SetKeyName(5, "");
			this.imageList1.Images.SetKeyName(6, "");
			this.imageList1.Images.SetKeyName(7, "");
			this.imageList1.Images.SetKeyName(8, "");
			this.imageList1.Images.SetKeyName(9, "");
			// 
			// statusLabel
			// 
			this.statusLabel.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.statusLabel.BackColor = System.Drawing.Color.Black;
			this.statusLabel.ForeColor = System.Drawing.Color.Silver;
			this.statusLabel.Location = new System.Drawing.Point(117, 180);
			this.statusLabel.Name = "statusLabel";
			this.statusLabel.Size = new System.Drawing.Size(176, 40);
			this.statusLabel.TabIndex = 6;
			this.statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.statusLabel.Visible = false;
			// 
			// pictureStatus
			// 
			this.pictureStatus.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.pictureStatus.BackColor = System.Drawing.Color.Black;
			this.pictureStatus.Image = ((System.Drawing.Image)(resources.GetObject("pictureStatus.Image")));
			this.pictureStatus.Location = new System.Drawing.Point(141, 49);
			this.pictureStatus.Name = "pictureStatus";
			this.pictureStatus.Size = new System.Drawing.Size(128, 128);
			this.pictureStatus.TabIndex = 5;
			this.pictureStatus.TabStop = false;
			this.pictureStatus.Visible = false;
			// 
			// listSamples
			// 
			this.listSamples.BackColor = System.Drawing.Color.Black;
			this.listSamples.ColumnWidth = 32;
			this.listSamples.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listSamples.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.listSamples.Font = new System.Drawing.Font("Lucida Console", 6F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.listSamples.ForeColor = System.Drawing.Color.Silver;
			this.listSamples.IntegralHeight = false;
			this.listSamples.ItemHeight = 32;
			this.listSamples.Location = new System.Drawing.Point(0, 0);
			this.listSamples.MultiColumn = true;
			this.listSamples.Name = "listSamples";
			this.listSamples.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
			this.listSamples.Size = new System.Drawing.Size(418, 261);
			this.listSamples.TabIndex = 1;
			this.listSamples.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.listSamples_DrawItem);
			this.listSamples.SelectedIndexChanged += new System.EventHandler(this.listSamples_SelectedIndexChanged);
			this.listSamples.KeyDown += new System.Windows.Forms.KeyEventHandler(this.listSamples_OnKeyDown);
			this.listSamples.MouseMove += new System.Windows.Forms.MouseEventHandler(this.listSamples_OnMouseMove);
			// 
			// categoriesList
			// 
			this.categoriesList.BackColor = System.Drawing.Color.Black;
			this.categoriesList.Dock = System.Windows.Forms.DockStyle.Fill;
			this.categoriesList.ForeColor = System.Drawing.Color.Silver;
			this.categoriesList.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.categoriesList.IntegralHeight = false;
			this.categoriesList.Location = new System.Drawing.Point(0, 0);
			this.categoriesList.Name = "categoriesList";
			this.categoriesList.Size = new System.Drawing.Size(210, 130);
			this.categoriesList.TabIndex = 0;
			this.categoriesList.SelectedIndexChanged += new System.EventHandler(this.categoriesList_SelectedIndexChanged);
			// 
			// timer
			// 
			this.timer.Enabled = true;
			this.timer.Interval = 10D;
			this.timer.SynchronizingObject = this;
			this.timer.Elapsed += new System.Timers.ElapsedEventHandler(this.timer_Elapsed);
			// 
			// trackBarMasterVol
			// 
			this.trackBarMasterVol.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.trackBarMasterVol.AutoSize = false;
			this.trackBarMasterVol.LargeChange = 1;
			this.trackBarMasterVol.Location = new System.Drawing.Point(506, 22);
			this.trackBarMasterVol.Maximum = 100;
			this.trackBarMasterVol.Name = "trackBarMasterVol";
			this.trackBarMasterVol.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this.trackBarMasterVol.Size = new System.Drawing.Size(120, 16);
			this.trackBarMasterVol.TabIndex = 7;
			this.trackBarMasterVol.Tag = "";
			this.trackBarMasterVol.TickFrequency = 10;
			this.trackBarMasterVol.TickStyle = System.Windows.Forms.TickStyle.None;
			this.trackBarMasterVol.Value = 100;
			this.trackBarMasterVol.Scroll += new System.EventHandler(this.trackBarMasterVol_Scroll);
			// 
			// labelVolume
			// 
			this.labelVolume.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.labelVolume.Location = new System.Drawing.Point(509, 7);
			this.labelVolume.Name = "labelVolume";
			this.labelVolume.Size = new System.Drawing.Size(67, 16);
			this.labelVolume.TabIndex = 8;
			this.labelVolume.Text = "Volume:";
			// 
			// labelVolumeValue
			// 
			this.labelVolumeValue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.labelVolumeValue.Location = new System.Drawing.Point(567, 7);
			this.labelVolumeValue.Name = "labelVolumeValue";
			this.labelVolumeValue.Size = new System.Drawing.Size(56, 16);
			this.labelVolumeValue.TabIndex = 9;
			this.labelVolumeValue.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// notifyIcon1
			// 
			this.notifyIcon1.ContextMenu = this.contextMenuNotify;
			this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
			this.notifyIcon1.Text = "Aural Probe";
			this.notifyIcon1.Visible = true;
			this.notifyIcon1.DoubleClick += new System.EventHandler(this.notifyIcon1_DoubleClick);
			// 
			// contextMenuNotify
			// 
			this.contextMenuNotify.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuItem1,
            this.menuItem2});
			this.contextMenuNotify.Popup += new System.EventHandler(this.contextMenuNotify_Popup);
			// 
			// menuItem1
			// 
			this.menuItem1.DefaultItem = true;
			this.menuItem1.Index = 0;
			this.menuItem1.Text = "Open Aural Probe";
			this.menuItem1.Click += new System.EventHandler(this.menuItem1_Click);
			// 
			// menuItem2
			// 
			this.menuItem2.Index = 1;
			this.menuItem2.Text = "Exit";
			this.menuItem2.Click += new System.EventHandler(this.menuItem2_Click);
			// 
			// splitContainer1
			// 
			this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer1.Location = new System.Drawing.Point(0, 0);
			this.splitContainer1.Name = "splitContainer1";
			this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.categoriesList);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.tagsList);
			this.splitContainer1.Size = new System.Drawing.Size(210, 261);
			this.splitContainer1.SplitterDistance = 130;
			this.splitContainer1.TabIndex = 7;
			// 
			// splitContainer2
			// 
			this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer2.Location = new System.Drawing.Point(0, 38);
			this.splitContainer2.Name = "splitContainer2";
			// 
			// splitContainer2.Panel1
			// 
			this.splitContainer2.Panel1.Controls.Add(this.splitContainer1);
			// 
			// splitContainer2.Panel2
			// 
			this.splitContainer2.Panel2.Controls.Add(this.statusLabel);
			this.splitContainer2.Panel2.Controls.Add(this.pictureStatus);
			this.splitContainer2.Panel2.Controls.Add(this.listSamples);
			this.splitContainer2.Size = new System.Drawing.Size(632, 261);
			this.splitContainer2.SplitterDistance = 210;
			this.splitContainer2.TabIndex = 10;
			// 
			// tagsList
			// 
			this.tagsList.BackColor = System.Drawing.Color.Black;
			this.tagsList.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tagsList.ForeColor = System.Drawing.Color.Silver;
			this.tagsList.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.tagsList.IntegralHeight = false;
			this.tagsList.Location = new System.Drawing.Point(0, 0);
			this.tagsList.Name = "tagsList";
			this.tagsList.Size = new System.Drawing.Size(210, 127);
			this.tagsList.TabIndex = 1;
			// 
			// MainForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(632, 323);
			this.Controls.Add(this.labelVolumeValue);
			this.Controls.Add(this.trackBarMasterVol);
			this.Controls.Add(this.labelVolume);
			this.Controls.Add(this.splitContainer2);
			this.Controls.Add(this.toolBar1);
			this.Controls.Add(this.statusBar);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MinimumSize = new System.Drawing.Size(640, 350);
			this.Name = "MainForm";
			this.Text = "Aural Probe";
			this.Load += new System.EventHandler(this.MainForm_Load);
			this.Resize += new System.EventHandler(this.MainForm_Resize);
			((System.ComponentModel.ISupportInitialize)(this.statusBarPanel)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.statusBarProperties)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureStatus)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.timer)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBarMasterVol)).EndInit();
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			this.splitContainer2.Panel1.ResumeLayout(false);
			this.splitContainer2.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
			this.splitContainer2.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		static MainForm gMainForm;

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]

		static void Main(string[] args) 
		{
			if (args.Length == 2)
			{
				Directory.SetCurrentDirectory(args[1]);
			}

			gMainForm = new MainForm();
			if (args.Length == 2)
			{
				gMainForm.SetForceLoadFavorites(args[0]);
			}
			Application.Run(gMainForm);
		}

		private void RefreshForm()
		{
			try
			{
				app.fmodManager.StopSoundPlayback();

				var tempFavorites = new FavoritesFile();
				if (this.lnSamples > 0 && app.Library.sampleFavoritesCount[0] > 0)
				{
					this.UpdateFavoritesFileFromFavoriteData(ref tempFavorites, false);
				}

				if (this.bUseCachedSamplesIfPossible && this.PopulateCategoriesWithAudioSamples(true))
				{
					this.bUseCachedSamplesIfPossible = false;
				}
				else
				{
					this.PopulateCategoriesWithAudioSamples(false);
				}

				if (this.lnSamples > 0 && app.Library.sampleFavoritesCount[0] > 0)
				{
					this.UpdateFavoriteDataFromFavoritesFile(ref tempFavorites, false);
					this.UpdateSampleFavorites();
				}

				this.UpdateFormWithConfigurationData();
				this.UpdateAudioSamples();
				this.UpdateTitleBarText();
			}
			catch (Exception e)
			{
				MessageBox.Show("RefreshForm " + e, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		public void SetForceLoadFavorites(string filename)
		{
			try
			{
				this.forceLoadFavoritesName = filename;
				if (this.forceLoadFavoritesName.Length > 0)
				{
					app.Files.FavoritesFile.Load(this.forceLoadFavoritesName);
					if (!this.UpdateFavoriteDataFromFavoritesFile(ref app.Files.FavoritesFile, true))
					{
						this.forceLoadFavoritesName = "";
						app.Files.FavoritesFile.Reset(0);
					}
					this.UpdateSampleFavorites();
					this.UpdateAudioSamples();
					this.lbDirtyFavorites = false;
				}
			}
			catch (Exception e)
			{
				MessageBox.Show("SetForceLoadFavorites " + filename + " " + e, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		public bool SaveFavorites()
		{
			try
			{
				var fdlg = new SaveFileDialog();
				fdlg.Title = "Save Favorites";
				if (configFile.DefaultFavoritesDirectory.Length > 0)
					fdlg.InitialDirectory = configFile.DefaultFavoritesDirectory;
				fdlg.OverwritePrompt = true;
				fdlg.Filter = "Aural Probe Favorites (*.apf)|*.apf|All files (*.*)|*.*";
				fdlg.FileName = app.Files.FavoritesFile.currentFavoritesFilename;
				
				var result = fdlg.ShowDialog();
				if (fdlg.FileName.Length > 0)
				{
					this.UpdateFavoritesFileFromFavoriteData(ref app.Files.FavoritesFile, true);
					app.Files.FavoritesFile.currentFavoritesFilename = fdlg.FileName;
					app.Files.FavoritesFile.Save();
					this.lbDirtyFavorites = false;
					this.UpdateFormWithConfigurationData();
					this.UpdateAudioSamples();
					this.UpdateTitleBarText();
					return true;
				}
			}
			catch (Exception e)
			{
				MessageBox.Show("SaveFavorites " + e, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
			return false;
		}

		private void toolBar1_ButtonClick(object sender, ToolBarButtonClickEventArgs e)
		{
			try
			{
				if ( e.Button == this.toolBarButtonRescanFolders )
				{
					this.RefreshForm();
				} 
				else if (e.Button == this.toolBarButtonPlayStop)
				{
					var bPlaying = false;
					if (this.channel != null)
					{
						this.channel.isPlaying(ref bPlaying);
					}
					if (bPlaying)
					{
						if (this.channel != null)
							this.channel.stop();
						this.toolBarButtonPlayStop.Enabled = this.listSamplesSingleSelectedIndex != -1;
						this.toolBarButtonPlayStop.Text = "Play";
						this.toolBarButtonPlayStop.ImageIndex = 7; // do work here
						this.bAutoPlayNextSample = false;
					}
					else
					{
						this.bAutoPlayNextSample = configFile.Autoplay;
						this.nAutoPlayRepeatsLeft = configFile.AutoplayRepeats;
						this.listSamples_SelectedIndexChanged(sender, e);
					}
				}
				else if (e.Button == this.toolBarButtonFavoritesOnly )
				{
					app.fmodManager.StopSoundPlayback();

					this.lbFavoritesOnly = !this.lbFavoritesOnly;
					this.UpdateFormWithConfigurationData();
					this.UpdateAudioSamples();
				
					var nCurrentCategory = this.categoriesList.SelectedIndex;
					if (nCurrentCategory < 0 || this.sampleIndicesCount[nCurrentCategory] == 0)
						return;
				}
				else if (e.Button == this.toolBarButtonResetFavorites )
				{
					app.fmodManager.StopSoundPlayback();

					if (app.Library.sampleFavoritesCount[0] > 0 && 
						DialogResult.Yes == MessageBox.Show("Are you sure you want to reset the favorites?", "Reset favorites?", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
					{
						app.Files.FavoritesFile.Reset(0);
						this.UpdateFavoriteDataFromFavoritesFile(ref app.Files.FavoritesFile, true);
						this.lbDirtyFavorites = false;
						this.UpdateSampleFavorites();
						this.UpdateAudioSamples();
					}
				}
				else if (e.Button == this.toolBarButtonLoadFavorites)
				{
					app.fmodManager.StopSoundPlayback();

					if ((app.Library.sampleFavoritesCount[0] == 0 || !this.lbDirtyFavorites) || DialogResult.Yes == MessageBox.Show(
						"You will lose all changes made to the current favorites. Are you sure?",
						"Replace favorites?",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Question))
					{
						var fdlg = new OpenFileDialog(); 
						fdlg.Title = "Open Favorites" ; 
						if (configFile.DefaultFavoritesDirectory.Length > 0)
							fdlg.InitialDirectory = configFile.DefaultFavoritesDirectory;
						fdlg.Filter = "Aural Probe Favorites (*.apf)|*.apf|All files (*.*)|*.*"; 
						fdlg.RestoreDirectory = true; 
						if(fdlg.ShowDialog() == DialogResult.OK && fdlg.FileName.Length > 0) 
						{
							app.Files.FavoritesFile.Load(fdlg.FileName);
							this.lbDirtyFavorites = false;
							this.UpdateFavoriteDataFromFavoritesFile(ref app.Files.FavoritesFile, true);
							this.UpdateSampleFavorites();
							this.UpdateAudioSamples();
						}
					}
				}
				else if (e.Button == this.toolBarButtonSaveFavorites )
				{
					app.fmodManager.StopSoundPlayback();

					if (app.Library.sampleFavoritesCount[0] == 0)
					{
						MessageBox.Show("There are no favorites to save.", "Save Favorites", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
					else
					{
						this.SaveFavorites();
					}
				}
				else if (e.Button == this.toolBarButtonConfiguration )
				{
					app.fmodManager.StopSoundPlayback();

					var result = this.configurationForm.ShowDialog(this);
					if (result == DialogResult.Retry)
					{
						// we need to rescan folders!
						this.RefreshForm();
					}
					else if (result == DialogResult.OK)
					{
						// settings have changed but sample data + favorites are still valid
						this.UpdateFormWithConfigurationData();
						this.UpdateAudioSamples();
					}
					else
					{
						// do nothing
					}
					
				}
				else if ( e.Button == this.toolBarButtonHelp )
				{
					app.fmodManager.StopSoundPlayback();

					try
					{
						System.Diagnostics.Process.Start("Aural Probe documentation.chm");
					}
					catch
					{
						MessageBox.Show("Error!  Could not find help.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					}
				
				}
				else if ( e.Button == this.toolBarButtonAbout )
				{
					app.fmodManager.StopSoundPlayback();

					this.aboutForm.ShowDialog();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("toolBar1_ButtonClick " + ex, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);				
			}
		}

		private void categoriesList_SelectedIndexChanged(object sender, EventArgs e)
		{
			try
			{
				this.listSamplesLastSelectedIndices = null;
				this.UpdateStatusBarAndLabel();
				this.Refresh();
				this.UpdateAudioSamples();

				var bIsPlaying = false;
				if (this.channel != null)
					this.channel.isPlaying(ref bIsPlaying);
				if (!bIsPlaying)
				{
					this.toolBarButtonPlayStop.Enabled = false;
					this.toolBarButtonPlayStop.Text = "Play";
					this.toolBarButtonPlayStop.ImageIndex = 7;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("categoriesList_SelectedIndexChanged " + ex, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		public static FMOD.RESULT soundEndedCallback(IntPtr channelraw, FMOD.CHANNEL_CALLBACKTYPE type, IntPtr commanddata1, IntPtr commanddata2) 
		{
			// Stop
			gMainForm.toolBarButtonPlayStop.Enabled = gMainForm.listSamplesSingleSelectedIndex != -1;
			gMainForm.toolBarButtonPlayStop.Text = "Play";
			gMainForm.toolBarButtonPlayStop.ImageIndex = 7;

			return FMOD.RESULT.OK;
		}

		private void listSamples_SelectedIndexChanged(object sender, EventArgs e)
		{
			this.nAutoPlayRepeatsLeft = configFile.AutoplayRepeats;
			this.listSamplesSelectedIndexChanged();
		}

		private void UpdateListSamplesSingleSelectedIndex()
		{
			if (this.listSamples.SelectedIndices.Count == 1)
			{
				this.listSamplesSingleSelectedIndex = this.listSamples.SelectedIndices[0];
			}
			else
			{
				this.listSamplesSingleSelectedIndex = -1; // don't allow an active singular selection if multiple items are selected
			}
		}

		public void listSamplesSelectedIndexChanged()
		{
			try
			{
				this.UpdateListSamplesSingleSelectedIndex();

				// Invalidate last list sample indices
				if (this.listSamplesLastSelectedIndices != null)
				{
					for (var i = 0; i < this.listSamplesLastSelectedIndices.Length; ++i)
					{
						this.listSamples.Invalidate(this.listSamples.GetItemRectangle(this.listSamplesLastSelectedIndices[i]));
					}
				}

				// Copy current list sample indices to last list sample indices
				this.listSamplesLastSelectedIndices = new int[this.listSamples.SelectedIndices.Count];
				for (var i = 0; i < this.listSamples.SelectedIndices.Count; ++i)
				{
					this.listSamplesLastSelectedIndices[i] = this.listSamples.SelectedIndices[i];
				}

				// Invalidate current list sample indices
				for (var i = 0; i < this.listSamples.SelectedIndices.Count; ++i)
				{
					this.listSamples.Invalidate(this.listSamples.GetItemRectangle(this.listSamples.SelectedIndices[i]));
				}

				// Update popup context menu and default Favorites checkbox state
				this.listSamples.ContextMenu = this.listSamples.SelectedIndices.Count > 0 ? this.sampleListMenu : null;
				this.sampleListMenu.MenuItems[3].Checked = false;

				var nCurrentCategory = this.categoriesList.SelectedIndex;
				if (nCurrentCategory < 0 || this.sampleIndicesCount[nCurrentCategory] == 0)
					return;
				var nCurrentSample = this.CalculateRealSampleIndex(this.listSamplesSingleSelectedIndex);
				if (nCurrentSample < 0)
				{
					this.toolBarButtonPlayStop.Enabled = false;
					this.toolBarButtonPlayStop.Text = "Play";
					this.toolBarButtonPlayStop.ImageIndex = 7;
					return;
				}

				var nSampleIndex = this.sampleIndices[nCurrentCategory, nCurrentSample];

				// update favorites checkbox state in popup menu if applicable
				if (this.listSamplesSingleSelectedIndex != -1)
					this.sampleListMenu.MenuItems[3].Checked = app.GetSampleFlag(nSampleIndex, BitFavorite);

				var sampleName = this.sampleList[nSampleIndex];

				this.statusBarPanel.Text = sampleName;
				this.statusBarPanel.ToolTipText = sampleName;

				var sampleFileExists = File.Exists(sampleName);
				app.SetSampleFlag(nSampleIndex, BitMissing, !sampleFileExists);
				if (!sampleFileExists)
					return;

				try
				{
					FMOD.RESULT result;
					if (this.channel != null)
					{
						this.channel.stop();
					}

					if (sound != null)
					{
						result = sound.release();
						fmodUtils.ERRCHECK(result);
						sound = null;
					}

					result = app.fmodManager.SystemFmod.createSound(sampleName, FMOD.MODE.SOFTWARE | FMOD.MODE.CREATESTREAM, ref sound);
					fmodUtils.ERRCHECK(result);
					var createSoundSucceeded = result == FMOD.RESULT.OK;

					if (createSoundSucceeded)
					{
						result = sound.setMode(FMOD.MODE.LOOP_OFF);
						fmodUtils.ERRCHECK(result);
						if (this.lbDontPlayNextSample)
						{
							this.lbDontPlayNextSample = false;
						}
						else
						{
							result = app.fmodManager.SystemFmod.playSound(FMOD.CHANNELINDEX.FREE, sound, false, ref this.channel);
							fmodUtils.ERRCHECK(result);

							if (this.channel != null)
							{
								this.channel.setCallback(this.cbFMOD);
							}

							this.toolBarButtonPlayStop.Enabled = this.listSamplesSingleSelectedIndex != -1;
							this.toolBarButtonPlayStop.Text = "Stop";
							this.toolBarButtonPlayStop.ImageIndex = 8;
							this.bAutoPlayNextSample = configFile.Autoplay;
						}
						FMOD.SOUND_TYPE stype = 0;
						FMOD.SOUND_FORMAT sformat = 0;
						var schannels = 0;
						var sbits = 0;
						float freq = 0;
						float vol = 0;
						float pan = 0;
						var pri = 0;
						uint length = 0;

						result = sound.getFormat(ref stype, ref sformat, ref schannels, ref sbits);
						fmodUtils.ERRCHECK(result);

						result = sound.getDefaults(ref freq, ref vol, ref pan, ref pri);
						fmodUtils.ERRCHECK(result);

						result = sound.getLength(ref length, FMOD.TIMEUNIT.MS);
						fmodUtils.ERRCHECK(result);

						var lengthstr = (length / (float)1000) + "s";
						this.statusBarProperties.Text =
							(freq / 1000) + "KHz " +
							sbits + "-bit " +
							(schannels > 1 ? "Stereo " : "Mono ") +
							"(" + sformat + "), " +
							lengthstr;
					}
					else
					{
						app.SetSampleFlag(nSampleIndex, BitMissing, true);
						this.listSamples.Invalidate(this.listSamples.GetItemRectangle(this.listSamplesSingleSelectedIndex));

						this.statusBarPanel.Text = sampleName;
						this.statusBarProperties.Text = "ERROR: Unable to play sample.";
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show("Error!  Could not play sample. " + ex, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("listSamples_SelectedIndexChanged " + ex, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		private void listSamples_DrawItem(object sender, DrawItemEventArgs e)
		{
			try
			{
				var nRealIndex = this.CalculateRealSampleIndex(e.Index);
				if (nRealIndex < 0)
					return;

				// Set the DrawMode property to draw fixed sized items.
				this.listSamples.DrawMode = DrawMode.OwnerDrawFixed;
				// Draw the background of the ListBox control for each item.
				e.DrawBackground();

				var nCurrentCategory = this.categoriesList.SelectedIndex;
				if (nCurrentCategory < 0 || this.sampleIndicesCount[nCurrentCategory] == 0)
					return;
				var nCurrentSample = nRealIndex;
				if (nCurrentSample < 0)
					return;
				var sampleIndex = this.sampleIndices[nCurrentCategory, nCurrentSample];

				Color color;
				if (this.listSamples.SelectedIndices.Contains(e.Index))
				{
					color = app.colorList[this.sampleColorIndex[sampleIndex],1];
				}
				else
				{
					color = app.colorList[this.sampleColorIndex[sampleIndex],0];
				}

				if (app.GetSampleFlag(sampleIndex, BitMissing))
				{
					color = Color.Red;
				}

				var brush = new SolidBrush(color);
				var favoritePen = new Pen(Color.White, 3);

				var textBrush = Brushes.Black;
				var nBorder = 2;
				Rectangle boundedText;
				boundedText = e.Bounds;
				boundedText.X += nBorder * 2;
				boundedText.Y += nBorder * 2;
				boundedText.Width -= nBorder * 4;
				boundedText.Height -= nBorder * 4;
				e.Graphics.FillRectangle(brush, e.Bounds.X + nBorder, e.Bounds.Y + nBorder, e.Bounds.Width - (nBorder * 2), e.Bounds.Height - (nBorder * 2));

				if (app.GetSampleFlag(sampleIndex, BitFavorite))
				{
					e.Graphics.DrawRectangle(favoritePen, e.Bounds.X + nBorder, e.Bounds.Y + nBorder, e.Bounds.Width - (nBorder * 2), e.Bounds.Height - (nBorder * 2));
				}

				var sampleName = this.sampleList[sampleIndex];
				var split = sampleName.Split('\\');
				var name = split[split.Length - 1];

				e.Graphics.DrawString(name, e.Font, textBrush, boundedText,StringFormat.GenericDefault);
			}
			catch (Exception ex)
			{
				MessageBox.Show("listSamples_DrawItem " + ex, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		private void listSamples_OnKeyDown(object sender, KeyEventArgs e)
		{
			try
			{
				if (e.KeyCode == Keys.Enter)
					app.ExploreSamples(sender, e);
				else if (e.Control && e.Shift && e.KeyCode == Keys.C)
					app.CopySamplesShortcut(sender, e);
				else if (e.Control && e.KeyCode == Keys.C)
					app.CopySamples(sender, e);
				else if (e.KeyCode == Keys.Space)
				{
					app.AddRemoveFromFavorites(sender, e);
					e.SuppressKeyPress = true; // prevents selection from being reset
				}
				if (e.KeyCode == Keys.Delete)
					app.DeleteSamples(sender, e);
			}
			catch (Exception ex)
			{
				MessageBox.Show("listSamples_OnKeyDown " + ex, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		public int CalculateRealSampleIndex(int listBoxIndex)
		{
			try
			{
				if (!this.lbFavoritesOnly)
					return listBoxIndex;
				else
				{
					var nCurrentCategory = this.categoriesList.SelectedIndex;
					if (nCurrentCategory < 0 || this.sampleIndicesCount[nCurrentCategory] == 0)
						return -1;
					var nFavoriteCount = 0;
					for (var i = 0; i < this.sampleIndicesCount[nCurrentCategory]; ++i)
					{
						var nSampleIndex = this.sampleIndices[nCurrentCategory, i];
						if (app.GetSampleFlag(nSampleIndex, BitFavorite))
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
				MessageBox.Show("CalculateRealSampleIndex " + listBoxIndex + " " + ex, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);			
			}
			return -1;
		}

		private void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e) 
		{
			try
			{
				if (!this.lbDirtyFavorites || app.Library.sampleFavoritesCount[0] == 0)
					return;

				// User is trying to log out. Prompt the user with choices
				var dr = MessageBox.Show(  "Do you want to save changes to your favorites before you logout?\n"+
					"Click Yes to save favorites and log out\n"+
					"Click No to logout without saving favorites\n"+
					"Click Cancel to cancel logout and manually close Aural Probe", "Save changes?",
					MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation  );

				// User promises to be good and manually stop the app from now on(yeah right)
				// Cancel the logout request, app continues
				if( dr == DialogResult.Cancel )
				{
					e.Cancel = true;
				}
					// Good user! Santa will bring lots of data this year
					// Save data and logout
				else if( dr == DialogResult.Yes )
				{
					e.Cancel = !this.SaveFavorites(); // if we cancel for some reason, don't abort program, let them save again!

				}
					// Bad user! doesn't care about poor data
				else if( dr == DialogResult.No )
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

		private void SystemEvents_LowMemory(object sender, EventArgs e) 
		{
			try
			{
				if (!this.lbDirtyFavorites || app.Library.sampleFavoritesCount[0] == 0)
					return;

				MessageBox.Show(  "Warning! The system is low on memory. Save your favorites!", "Low memory detected!",
					MessageBoxButtons.OK, MessageBoxIcon.Exclamation  );
			}
			catch (Exception ex)
			{
				MessageBox.Show("SystemEvents_LowMemory " + ex, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			try
			{
				if (app.fmodManager != null && app.fmodManager.SystemFmod != null && app.fmodManager.FmodInitialised && this.channel != null)
				{
					var result = app.fmodManager.SystemFmod.update();
					fmodUtils.ERRCHECK(result);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("timer_Elapsed " + ex, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}

			// handle AutoPlay
			try
			{
				var isPlaying = false;
				if (this.channel != null)
				{
					this.channel.isPlaying(ref isPlaying);
				}

				if (gMainForm != null && gMainForm.bAutoPlayNextSample && gMainForm.listSamplesSingleSelectedIndex != -1 && (gMainForm.nAutoPlayRepeatsLeft > 1 || (gMainForm.listSamplesSingleSelectedIndex < gMainForm.listSamples.Items.Count - 1)) && !isPlaying)
				{
					gMainForm.nAutoPlayRepeatsLeft--;
					if (gMainForm.nAutoPlayRepeatsLeft > 0)
					{
						gMainForm.listSamplesSelectedIndexChanged();
					}
					else
					{
						gMainForm.nAutoPlayRepeatsLeft = configFile.AutoplayRepeats;
						var newSelectedIndex = this.listSamplesSingleSelectedIndex + 1;
						if (newSelectedIndex < gMainForm.listSamples.Items.Count)
						{
							gMainForm.listSamples.SetSelected(this.listSamplesSingleSelectedIndex, false);
							gMainForm.listSamples.SetSelected(newSelectedIndex, true);
						}
					}
				}
				else
				{
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("timer_Elapsed " + ex, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}

		}

		private void listSamples_OnMouseMove(object sender, MouseEventArgs e)
		{
			try
			{
				if (!SamplesListBox.s_allowLeftMouseDownEvent && e.Button == MouseButtons.Left)
				{
					// Handle drag + drop - this is a synchronous event, and will block this thread until DoDragDrop is complete (by failing or succeeding)
					var nCurrentCategory = this.categoriesList.SelectedIndex;
					if (this.listSamples.SelectedIndices.Count == 0)
						return;
					var files = new String[this.listSamples.SelectedIndices.Count];
					for (var i = 0; i < this.listSamples.SelectedIndices.Count; ++i)
					{
						var nCurrentSample = this.CalculateRealSampleIndex(this.listSamples.SelectedIndices[i]);
						if (nCurrentSample < 0)
							continue;
						files[i] = this.sampleList[this.sampleIndices[nCurrentCategory, nCurrentSample]];
					}
					var objData = new DataObject(DataFormats.FileDrop, files);
					this.listSamples.DoDragDrop(objData, DragDropEffects.Copy);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("OnMouseMove " + ex, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		
		}

		private void trackBarMasterVol_Scroll(object sender, EventArgs e)
		{
			this.UpdateVolume();
		}

		private void MainForm_Load(object sender, EventArgs e)
		{
		
		}

		private void MainForm_Resize(object sender, EventArgs e)
		{
			if (FormWindowState.Minimized == this.WindowState)
				this.Hide();
		}

		private void notifyIcon1_DoubleClick(object sender, EventArgs e)
		{
			if (FormWindowState.Minimized == this.WindowState)
			{
				this.Show();
				this.WindowState = FormWindowState.Normal;
			}
		}

		private void contextMenuNotify_Popup(object sender, EventArgs e)
		{
		}

		private void menuItem2_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}

		private void menuItem1_Click(object sender, EventArgs e)
		{
			if (FormWindowState.Minimized == this.WindowState)
			{
				this.Show();
				this.WindowState = FormWindowState.Normal;
			}
		}

		private void listSamples_PreMouseDown(object sender, EventArgs e)
		{
			var point = Cursor.Position;
			point = this.listSamples.PointToClient(point);
			var listBoxSampleIndex = this.listSamples.IndexFromPoint(point);
			if (listBoxSampleIndex != ListBox.NoMatches)
			{
				// only allow left mouse down event to go through to ListBox (which modifies the selection) when clicking on an unselected item, or if CTRL is held down (meaning the user is trying to turn off an actively selected element)
				SamplesListBox.s_allowLeftMouseDownEvent = !this.listSamples.SelectedIndices.Contains(listBoxSampleIndex) || ModifierKeys == Keys.Control;
			}
		}

		private void statusBar_DrawItem(object sender, StatusBarDrawItemEventArgs e)
		{
			// Manually draw the status bar panel text here to avoid 127 character limit.
			// See http://msdn.microsoft.com/en-us/library/vstudio/we893ad3%28v=vs.80%29.aspx

			var textFormat = new StringFormat();
			textFormat.LineAlignment = StringAlignment.Center; // vertical middle
			textFormat.Alignment = StringAlignment.Near; // horizontal left
			
			var text = e.Panel.Text;
			int indexForward, indexBackward, finalIndex;
			while (e.Graphics.MeasureString(text, this.statusBar.Font).Width > e.Bounds.Width)
			{
				indexForward = text.IndexOf('\\');
				indexBackward = text.IndexOf('/');
				if (indexForward < 0 && indexBackward < 0)
					break; // can't strip any more
				finalIndex = indexForward;
				if (finalIndex < 0 || (indexBackward >= 0 && indexBackward < finalIndex))
					finalIndex = indexBackward;
				text = text.Substring(finalIndex + 1);
			}
			e.Graphics.DrawString(text, this.statusBar.Font, Brushes.Black, new RectangleF(e.Bounds.X, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height), textFormat);
		}
	}
}
