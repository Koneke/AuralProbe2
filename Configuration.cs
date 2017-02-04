using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Aural_Probe
{
	/// <summary>
	/// Summary description for Configuration.
	/// </summary>

	// for stuff in the configuration menu
	// kinda weird and copied up from ConfigFile, but I guess this is the more "proper" way?
	public class ConfigurationForm : Form
	{
		#region Controls
		// freely floating controls
		private Button buttonOk;
		private Button buttonCancel;

		// groupGeneral
		private GroupBox groupGeneral;
		private Label labelSampleDisplaySize;
		private NumericUpDown numericSampleDisplaySizeW;
		private Label labelSampleDisplaySizeX;
		private NumericUpDown numericSampleDisplaySizeH;
		private CheckBox checkBoxAlwaysOnTop;
		private CheckBox checkBoxRescanPrompt;
		private CheckBox checkBoxIncludeFilePaths;
		private CheckBox checkBoxAutoplay;
		private NumericUpDown numericAutoplayRepeats;
		private Label labelAutoplayTimes;

		// groupCategories
		private GroupBox groupCategories;
		private ListBox listCategories;
		private Label labelName;
		private TextBox textName;
		private Label labelSearchCriteria;
		private TextBox textWildcard;
		private CheckBox checkBoxUseRegularExpressions;
		private LinkLabel linkLabelUseRegularExpressionsHelp;
		private Button buttonMoveUp;
		private Button buttonMoveDown;
		private Button buttonInsert;
		private Button buttonReplace;
		private Button buttonDelete;

		// groupDirectories
		private GroupBox groupDirectories;
		private TextBox textDirectories;

		// groupFileTypes
		private GroupBox groupFileTypes;
		private CheckBox checkBoxWAV;
		private CheckBox checkBoxAIFF;
		private CheckBox checkBoxFLAC;
		private CheckBox checkBoxMP3;
		private CheckBox checkBoxWMA;
		private CheckBox checkBoxOGG;

		// groupFavorites
		private GroupBox groupFavorites;
		private TextBox textBoxDefaultFavorites;

		// groupSoundOutput
		private GroupBox groupSoundOutput;
		private ComboBox comboOutputDevice;
		#endregion

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private readonly System.ComponentModel.Container components = null;

		// config file we're currently working on.
		private ConfigFile configFile;

		private int CategoryIndex => this.listCategories.SelectedIndex;

		// Don't run automatic control -> config stuff when still populating the controls
		private bool loading;

		// Do we have unsaved changes?
		private bool dataIsDirty;

		// Have we changed anything that affects which files/folders are visible?
		private bool needToRescanFolders;
		
		public ConfigurationForm()
		{
			// Required for Windows Form Designer support
			this.InitializeComponent();
		}

		// Set up device output combo box
		private void InitSoundOutputUI()
		{
			MainForm.app.fmodManager.TryPrimarySoundDeviceHack();

			this.comboOutputDevice.Items.Clear();
			foreach (var driver in MainForm.app.fmodManager.GetAvailableDrivers())
			{
				this.comboOutputDevice.Items.Add(driver);
			}

			this.comboOutputDevice.SelectedIndex = MainForm.app.fmodManager.GetCurrentDriver();
		}

		private void ChangeSoundOutput()
		{
			MainForm.app.fmodManager.ChangeSoundOutput(this.comboOutputDevice.SelectedIndex);
		}

		private static string GetCategoryListName(Category category)
		{
			return category.Name + "\t" + string.Join(",", category.SearchStrings);
		}

		private void LoadConfigValuesToControls()
		{
			this.loading = true;

			this.numericSampleDisplaySizeW.Value = this.configFile.SampleDisplaySizeW;
			this.numericSampleDisplaySizeH.Value = this.configFile.SampleDisplaySizeH;
			this.numericAutoplayRepeats.Value = this.configFile.AutoplayRepeats;

			this.UpdateCategoryList();

			this.listCategories.SelectedIndex = this.configFile.Categories.Count > 0 ? 0 : -1;
			this.checkBoxRescanPrompt.Checked = this.configFile.RescanPrompt;
			this.checkBoxIncludeFilePaths.Checked = this.configFile.IncludeFilePaths;
			this.checkBoxAutoplay.Checked = this.configFile.Autoplay;
			this.checkBoxAlwaysOnTop.Checked = this.configFile.AlwaysOnTop;
			this.numericAutoplayRepeats.Enabled = this.checkBoxAutoplay.Checked;

			this.checkBoxWAV.Checked = this.configFile.LoadWav;
			this.checkBoxAIFF.Checked = this.configFile.LoadAiff;
			this.checkBoxFLAC.Checked = this.configFile.LoadFlac;
			this.checkBoxMP3.Checked = this.configFile.LoadMp3;
			this.checkBoxWMA.Checked = this.configFile.LoadWma;
			this.checkBoxOGG.Checked = this.configFile.LoadOgg;

			this.textDirectories.Clear();
			this.textDirectories.Text = string.Join("\n", this.configFile.SearchDirectories);

			this.textBoxDefaultFavorites.Text = this.configFile.DefaultFavoritesDirectory;

			this.InitSoundOutputUI();

			this.loading = false;

			this.UpdateCheckboxes();
			this.UpdateNumerics();
		}

		private void InitFromConfigFile(object sender, EventArgs e)
		{
			this.configFile =
				JsonConvert.DeserializeObject<ConfigFile>(
					JsonConvert.SerializeObject(MainForm.configFile));

			this.LoadConfigValuesToControls();

			this.dataIsDirty = false;
			this.needToRescanFolders = false;
		}

		private void SaveSearchDirectories()
		{
			var searchDirectories = this.textDirectories.Text
				.Split(',', ';', '\n', '\r', '\t')
				.ToList();

			var invalidDirectories = "";

			foreach (var searchDirectory in searchDirectories)
			{
				if (searchDirectory.Length > 0)
				{
					if (!Directory.Exists(searchDirectory))
					{
						invalidDirectories += searchDirectory + "\n";
					}
				}
			}

			if (invalidDirectories != "")
			{
				MessageBox.Show("Warning! The following search folders do not exist:\n\n" + invalidDirectories, "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}

			this.configFile.SearchDirectories = searchDirectories;
		}

		private void SaveToConfigFile()
		{
			if (this.dataIsDirty)
			{
				this.SaveSearchDirectories();
				this.configFile.DefaultFavoritesDirectory = this.textBoxDefaultFavorites.Text;
				this.configFile.DefaultSoundDevice = this.comboOutputDevice.Text;

				this.configFile.Save();
				MainForm.app.Files.ConfigFile = this.configFile;

				this.dataIsDirty = false;
			}
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				this.components?.Dispose();
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
			this.buttonOk = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.groupCategories = new System.Windows.Forms.GroupBox();
			this.buttonMoveDown = new System.Windows.Forms.Button();
			this.buttonMoveUp = new System.Windows.Forms.Button();
			this.linkLabelUseRegularExpressionsHelp = new System.Windows.Forms.LinkLabel();
			this.checkBoxUseRegularExpressions = new System.Windows.Forms.CheckBox();
			this.textName = new System.Windows.Forms.TextBox();
			this.listCategories = new System.Windows.Forms.ListBox();
			this.buttonInsert = new System.Windows.Forms.Button();
			this.buttonDelete = new System.Windows.Forms.Button();
			this.textWildcard = new System.Windows.Forms.TextBox();
			this.labelSearchCriteria = new System.Windows.Forms.Label();
			this.buttonReplace = new System.Windows.Forms.Button();
			this.labelName = new System.Windows.Forms.Label();
			this.groupDirectories = new System.Windows.Forms.GroupBox();
			this.textDirectories = new System.Windows.Forms.TextBox();
			this.groupGeneral = new System.Windows.Forms.GroupBox();
			this.numericSampleDisplaySizeH = new System.Windows.Forms.NumericUpDown();
			this.labelSampleDisplaySize = new System.Windows.Forms.Label();
			this.numericSampleDisplaySizeW = new System.Windows.Forms.NumericUpDown();
			this.checkBoxRescanPrompt = new System.Windows.Forms.CheckBox();
			this.checkBoxIncludeFilePaths = new System.Windows.Forms.CheckBox();
			this.numericAutoplayRepeats = new System.Windows.Forms.NumericUpDown();
			this.checkBoxAutoplay = new System.Windows.Forms.CheckBox();
			this.labelAutoplayTimes = new System.Windows.Forms.Label();
			this.labelSampleDisplaySizeX = new System.Windows.Forms.Label();
			this.checkBoxAlwaysOnTop = new System.Windows.Forms.CheckBox();
			this.checkBoxWAV = new System.Windows.Forms.CheckBox();
			this.checkBoxAIFF = new System.Windows.Forms.CheckBox();
			this.checkBoxFLAC = new System.Windows.Forms.CheckBox();
			this.checkBoxMP3 = new System.Windows.Forms.CheckBox();
			this.checkBoxWMA = new System.Windows.Forms.CheckBox();
			this.checkBoxOGG = new System.Windows.Forms.CheckBox();
			this.groupFileTypes = new System.Windows.Forms.GroupBox();
			this.groupFavorites = new System.Windows.Forms.GroupBox();
			this.textBoxDefaultFavorites = new System.Windows.Forms.TextBox();
			this.groupSoundOutput = new System.Windows.Forms.GroupBox();
			this.comboOutputDevice = new System.Windows.Forms.ComboBox();
			this.groupCategories.SuspendLayout();
			this.groupDirectories.SuspendLayout();
			this.groupGeneral.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.numericSampleDisplaySizeH)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericSampleDisplaySizeW)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericAutoplayRepeats)).BeginInit();
			this.groupFileTypes.SuspendLayout();
			this.groupFavorites.SuspendLayout();
			this.groupSoundOutput.SuspendLayout();
			this.SuspendLayout();
			// 
			// buttonOk
			// 
			this.buttonOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOk.Location = new System.Drawing.Point(408, 16);
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.Size = new System.Drawing.Size(72, 24);
			this.buttonOk.TabIndex = 6;
			this.buttonOk.Text = "&OK";
			this.buttonOk.Click += new System.EventHandler(this.buttonOK_Click);
			// 
			// buttonCancel
			// 
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.Location = new System.Drawing.Point(408, 48);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(72, 24);
			this.buttonCancel.TabIndex = 7;
			this.buttonCancel.Text = "&Cancel";
			this.buttonCancel.Click += new System.EventHandler(this.button1_Click);
			// 
			// groupCategories
			// 
			this.groupCategories.Controls.Add(this.buttonMoveUp);
			this.groupCategories.Controls.Add(this.buttonMoveDown);
			this.groupCategories.Controls.Add(this.linkLabelUseRegularExpressionsHelp);
			this.groupCategories.Controls.Add(this.checkBoxUseRegularExpressions);
			this.groupCategories.Controls.Add(this.textName);
			this.groupCategories.Controls.Add(this.listCategories);
			this.groupCategories.Controls.Add(this.buttonInsert);
			this.groupCategories.Controls.Add(this.buttonDelete);
			this.groupCategories.Controls.Add(this.textWildcard);
			this.groupCategories.Controls.Add(this.labelSearchCriteria);
			this.groupCategories.Controls.Add(this.buttonReplace);
			this.groupCategories.Controls.Add(this.labelName);
			this.groupCategories.Location = new System.Drawing.Point(8, 112);
			this.groupCategories.Name = "groupCategories";
			this.groupCategories.Size = new System.Drawing.Size(472, 170);
			this.groupCategories.TabIndex = 1;
			this.groupCategories.TabStop = false;
			this.groupCategories.Text = "Categories:";
			this.groupCategories.Enter += new System.EventHandler(this.groupCategories_Enter);
			// 
			// buttonMoveDown
			// 
			this.buttonMoveDown.Location = new System.Drawing.Point(248, 138);
			this.buttonMoveDown.Name = "buttonMoveDown";
			this.buttonMoveDown.Size = new System.Drawing.Size(18, 24);
			this.buttonMoveDown.TabIndex = 8;
			this.buttonMoveDown.Text = "↓";
			this.buttonMoveDown.Click += new System.EventHandler(this.buttonMoveDown_Click);
			// 
			// buttonMoveUp
			// 
			this.buttonMoveUp.Location = new System.Drawing.Point(224, 138);
			this.buttonMoveUp.Name = "buttonMoveUp";
			this.buttonMoveUp.Size = new System.Drawing.Size(18, 24);
			this.buttonMoveUp.TabIndex = 7;
			this.buttonMoveUp.Text = "↑";
			this.buttonMoveUp.Click += new System.EventHandler(this.buttonMoveUp_Click);
			// 
			// linkLabelUseRegularExpressionsHelp
			// 
			this.linkLabelUseRegularExpressionsHelp.AutoSize = true;
			this.linkLabelUseRegularExpressionsHelp.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
			this.linkLabelUseRegularExpressionsHelp.Location = new System.Drawing.Point(366, 107);
			this.linkLabelUseRegularExpressionsHelp.Name = "linkLabelUseRegularExpressionsHelp";
			this.linkLabelUseRegularExpressionsHelp.Size = new System.Drawing.Size(13, 13);
			this.linkLabelUseRegularExpressionsHelp.TabIndex = 6;
			this.linkLabelUseRegularExpressionsHelp.TabStop = true;
			this.linkLabelUseRegularExpressionsHelp.Text = "?";
			this.linkLabelUseRegularExpressionsHelp.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelUseRegularExpressionsHelp_LinkClicked);
			// 
			// checkBoxUseRegularExpressions
			// 
			this.checkBoxUseRegularExpressions.Checked = true;
			this.checkBoxUseRegularExpressions.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxUseRegularExpressions.Location = new System.Drawing.Point(224, 106);
			this.checkBoxUseRegularExpressions.Name = "checkBoxUseRegularExpressions";
			this.checkBoxUseRegularExpressions.Size = new System.Drawing.Size(152, 16);
			this.checkBoxUseRegularExpressions.TabIndex = 5;
			this.checkBoxUseRegularExpressions.Text = "Use regular expressions";
			// 
			// textName
			// 
			this.textName.Location = new System.Drawing.Point(224, 32);
			this.textName.Name = "textName";
			this.textName.Size = new System.Drawing.Size(240, 20);
			this.textName.TabIndex = 2;
			// 
			// listCategories
			// 
			this.listCategories.HorizontalScrollbar = true;
			this.listCategories.IntegralHeight = false;
			this.listCategories.Location = new System.Drawing.Point(8, 16);
			this.listCategories.Name = "listCategories";
			this.listCategories.Size = new System.Drawing.Size(208, 146);
			this.listCategories.TabIndex = 0;
			this.listCategories.SelectedIndexChanged += new System.EventHandler(this.listCategories_SelectedIndexChanged);
			// 
			// buttonInsert
			// 
			this.buttonInsert.Location = new System.Drawing.Point(272, 138);
			this.buttonInsert.Name = "buttonInsert";
			this.buttonInsert.Size = new System.Drawing.Size(60, 24);
			this.buttonInsert.TabIndex = 9;
			this.buttonInsert.Text = "&Insert";
			this.buttonInsert.Click += new System.EventHandler(this.buttonInsert_Click);
			// 
			// buttonDelete
			// 
			this.buttonDelete.Location = new System.Drawing.Point(404, 138);
			this.buttonDelete.Name = "buttonDelete";
			this.buttonDelete.Size = new System.Drawing.Size(60, 24);
			this.buttonDelete.TabIndex = 11;
			this.buttonDelete.Text = "&Delete";
			this.buttonDelete.Click += new System.EventHandler(this.buttonDelete_Click);
			// 
			// textWildcard
			// 
			this.textWildcard.Location = new System.Drawing.Point(224, 80);
			this.textWildcard.Name = "textWildcard";
			this.textWildcard.Size = new System.Drawing.Size(240, 20);
			this.textWildcard.TabIndex = 4;
			// 
			// labelSearchCriteria
			// 
			this.labelSearchCriteria.Location = new System.Drawing.Point(224, 64);
			this.labelSearchCriteria.Name = "labelSearchCriteria";
			this.labelSearchCriteria.Size = new System.Drawing.Size(152, 16);
			this.labelSearchCriteria.TabIndex = 3;
			this.labelSearchCriteria.Text = "Search Criteria:";
			// 
			// buttonReplace
			// 
			this.buttonReplace.Location = new System.Drawing.Point(338, 138);
			this.buttonReplace.Name = "buttonReplace";
			this.buttonReplace.Size = new System.Drawing.Size(60, 24);
			this.buttonReplace.TabIndex = 10;
			this.buttonReplace.Text = "&Replace";
			this.buttonReplace.Click += new System.EventHandler(this.buttonReplace_Click);
			// 
			// labelName
			// 
			this.labelName.Location = new System.Drawing.Point(224, 16);
			this.labelName.Name = "labelName";
			this.labelName.Size = new System.Drawing.Size(152, 16);
			this.labelName.TabIndex = 1;
			this.labelName.Text = "Name:";
			// 
			// groupDirectories
			// 
			this.groupDirectories.Controls.Add(this.textDirectories);
			this.groupDirectories.Location = new System.Drawing.Point(8, 288);
			this.groupDirectories.Name = "groupDirectories";
			this.groupDirectories.Size = new System.Drawing.Size(384, 112);
			this.groupDirectories.TabIndex = 2;
			this.groupDirectories.TabStop = false;
			this.groupDirectories.Text = "Search Folders:";
			// 
			// textDirectories
			// 
			this.textDirectories.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textDirectories.Location = new System.Drawing.Point(8, 16);
			this.textDirectories.Multiline = true;
			this.textDirectories.Name = "textDirectories";
			this.textDirectories.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.textDirectories.Size = new System.Drawing.Size(368, 88);
			this.textDirectories.TabIndex = 0;
			this.textDirectories.TextChanged += new System.EventHandler(this.OnSearchFoldersChanged);
			// 
			// groupGeneral
			// 
			this.groupGeneral.Controls.Add(this.labelSampleDisplaySize);
			this.groupGeneral.Controls.Add(this.numericSampleDisplaySizeH);
			this.groupGeneral.Controls.Add(this.labelSampleDisplaySizeX);
			this.groupGeneral.Controls.Add(this.numericSampleDisplaySizeW);
			this.groupGeneral.Controls.Add(this.checkBoxAlwaysOnTop);
			this.groupGeneral.Controls.Add(this.checkBoxRescanPrompt);
			this.groupGeneral.Controls.Add(this.checkBoxIncludeFilePaths);
			this.groupGeneral.Controls.Add(this.numericAutoplayRepeats);
			this.groupGeneral.Controls.Add(this.checkBoxAutoplay);
			this.groupGeneral.Controls.Add(this.labelAutoplayTimes);
			this.groupGeneral.Location = new System.Drawing.Point(8, 8);
			this.groupGeneral.Name = "groupGeneral";
			this.groupGeneral.Size = new System.Drawing.Size(384, 96);
			this.groupGeneral.TabIndex = 0;
			this.groupGeneral.TabStop = false;
			this.groupGeneral.Text = "General:";
			// 
			// numericSampleDisplaySizeH
			// 
			this.numericSampleDisplaySizeH.Location = new System.Drawing.Point(79, 34);
			this.numericSampleDisplaySizeH.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
			this.numericSampleDisplaySizeH.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.numericSampleDisplaySizeH.Name = "numericSampleDisplaySizeH";
			this.numericSampleDisplaySizeH.Size = new System.Drawing.Size(48, 20);
			this.numericSampleDisplaySizeH.TabIndex = 3;
			this.numericSampleDisplaySizeH.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.numericSampleDisplaySizeH.ValueChanged += new EventHandler(this.NumericChanged);
			this.numericSampleDisplaySizeH.KeyPress += new KeyPressEventHandler(this.NumericChanged);
			this.numericSampleDisplaySizeH.Leave += new EventHandler(this.NumericChanged);
			// 
			// labelSampleDisplaySize
			// 
			this.labelSampleDisplaySize.Location = new System.Drawing.Point(16, 16);
			this.labelSampleDisplaySize.Name = "labelSampleDisplaySize";
			this.labelSampleDisplaySize.Size = new System.Drawing.Size(112, 16);
			this.labelSampleDisplaySize.TabIndex = 0;
			this.labelSampleDisplaySize.Text = "Sample Display Size:";
			// 
			// numericSampleDisplaySizeW
			// 
			this.numericSampleDisplaySizeW.Location = new System.Drawing.Point(17, 34);
			this.numericSampleDisplaySizeW.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
			this.numericSampleDisplaySizeW.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.numericSampleDisplaySizeW.Name = "numericSampleDisplaySizeW";
			this.numericSampleDisplaySizeW.Size = new System.Drawing.Size(48, 20);
			this.numericSampleDisplaySizeW.TabIndex = 1;
			this.numericSampleDisplaySizeW.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.numericSampleDisplaySizeW.ValueChanged += new EventHandler(this.NumericChanged);
			this.numericSampleDisplaySizeW.KeyPress += new KeyPressEventHandler(this.NumericChanged);
			this.numericSampleDisplaySizeW.Leave += new EventHandler(this.NumericChanged);
			// 
			// checkBoxRescanPrompt
			// 
			this.checkBoxRescanPrompt.Checked = true;
			this.checkBoxRescanPrompt.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxRescanPrompt.Location = new System.Drawing.Point(184, 16);
			this.checkBoxRescanPrompt.Name = "checkBoxRescanPrompt";
			this.checkBoxRescanPrompt.Size = new System.Drawing.Size(192, 16);
			this.checkBoxRescanPrompt.TabIndex = 5;
			this.checkBoxRescanPrompt.Text = "Prompt when rescanning folders";
			this.checkBoxRescanPrompt.CheckedChanged += new System.EventHandler(this.checkBoxRescanPrompt_CheckedChanged);
			// 
			// checkBoxIncludeFilePaths
			// 
			this.checkBoxIncludeFilePaths.Checked = true;
			this.checkBoxIncludeFilePaths.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxIncludeFilePaths.Location = new System.Drawing.Point(184, 40);
			this.checkBoxIncludeFilePaths.Name = "checkBoxIncludeFilePaths";
			this.checkBoxIncludeFilePaths.Size = new System.Drawing.Size(192, 16);
			this.checkBoxIncludeFilePaths.TabIndex = 6;
			this.checkBoxIncludeFilePaths.Text = "Include file paths when searching";
			this.checkBoxIncludeFilePaths.CheckedChanged += new System.EventHandler(this.checkBoxIncludeFilePaths_CheckedChanged);
			// 
			// numericAutoplayRepeats
			// 
			this.numericAutoplayRepeats.Location = new System.Drawing.Point(251, 62);
			this.numericAutoplayRepeats.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.numericAutoplayRepeats.Name = "numericAutoplayRepeats";
			this.numericAutoplayRepeats.Size = new System.Drawing.Size(48, 20);
			this.numericAutoplayRepeats.TabIndex = 8;
			this.numericAutoplayRepeats.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.numericAutoplayRepeats.ValueChanged += new System.EventHandler(this.numericAutoplayRepeats_ValueChanged);
			this.numericAutoplayRepeats.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.numericAutoplayRepeats_ValueChanged);
			this.numericAutoplayRepeats.Leave += new System.EventHandler(this.numericAutoplayRepeats_ValueChanged);
			// 
			// checkBoxAutoplay
			// 
			this.checkBoxAutoplay.Checked = true;
			this.checkBoxAutoplay.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxAutoplay.Location = new System.Drawing.Point(184, 64);
			this.checkBoxAutoplay.Name = "checkBoxAutoplay";
			this.checkBoxAutoplay.Size = new System.Drawing.Size(72, 16);
			this.checkBoxAutoplay.TabIndex = 7;
			this.checkBoxAutoplay.Text = "Autoplay";
			this.checkBoxAutoplay.CheckedChanged += new System.EventHandler(this.checkBoxAutoplay_CheckedChanged);
			// 
			// labelAutoplayTimes
			// 
			this.labelAutoplayTimes.Location = new System.Drawing.Point(304, 65);
			this.labelAutoplayTimes.Name = "labelAutoplayTimes";
			this.labelAutoplayTimes.Size = new System.Drawing.Size(56, 16);
			this.labelAutoplayTimes.TabIndex = 9;
			this.labelAutoplayTimes.Text = "time(s)";
			// 
			// labelSampleDisplaySizeX
			// 
			this.labelSampleDisplaySizeX.Location = new System.Drawing.Point(66, 36);
			this.labelSampleDisplaySizeX.Name = "labelSampleDisplaySizeX";
			this.labelSampleDisplaySizeX.Size = new System.Drawing.Size(16, 16);
			this.labelSampleDisplaySizeX.TabIndex = 2;
			this.labelSampleDisplaySizeX.Text = "x";
			// 
			// checkBoxAlwaysOnTop
			// 
			this.checkBoxAlwaysOnTop.Checked = true;
			this.checkBoxAlwaysOnTop.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxAlwaysOnTop.Location = new System.Drawing.Point(16, 64);
			this.checkBoxAlwaysOnTop.Name = "checkBoxAlwaysOnTop";
			this.checkBoxAlwaysOnTop.Size = new System.Drawing.Size(152, 16);
			this.checkBoxAlwaysOnTop.TabIndex = 4;
			this.checkBoxAlwaysOnTop.Text = "Always on top";
			this.checkBoxAlwaysOnTop.CheckedChanged += new System.EventHandler(this.checkBoxAlwaysOnTop_CheckedChanged);
			// 
			// checkBoxWAV
			// 
			this.checkBoxWAV.Checked = true;
			this.checkBoxWAV.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxWAV.Location = new System.Drawing.Point(16, 24);
			this.checkBoxWAV.Name = "checkBoxWAV";
			this.checkBoxWAV.Size = new System.Drawing.Size(56, 16);
			this.checkBoxWAV.TabIndex = 0;
			this.checkBoxWAV.Text = "WAV";
			this.checkBoxWAV.CheckedChanged += new System.EventHandler(this.FileTypeCheckBoxChanged);
			// 
			// checkBoxAIFF
			// 
			this.checkBoxAIFF.Checked = true;
			this.checkBoxAIFF.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxAIFF.Location = new System.Drawing.Point(16, 48);
			this.checkBoxAIFF.Name = "checkBoxAIFF";
			this.checkBoxAIFF.Size = new System.Drawing.Size(56, 16);
			this.checkBoxAIFF.TabIndex = 1;
			this.checkBoxAIFF.Text = "AIFF";
			this.checkBoxAIFF.CheckedChanged += new System.EventHandler(this.FileTypeCheckBoxChanged);
			// 
			// checkBoxFLAC
			// 
			this.checkBoxFLAC.Checked = true;
			this.checkBoxFLAC.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxFLAC.Location = new System.Drawing.Point(16, 72);
			this.checkBoxFLAC.Name = "checkBoxFLAC";
			this.checkBoxFLAC.Size = new System.Drawing.Size(56, 16);
			this.checkBoxFLAC.TabIndex = 2;
			this.checkBoxFLAC.Text = "FLAC";
			this.checkBoxFLAC.CheckedChanged += new System.EventHandler(this.FileTypeCheckBoxChanged);
			// 
			// checkBoxMP3
			// 
			this.checkBoxMP3.Checked = true;
			this.checkBoxMP3.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxMP3.Location = new System.Drawing.Point(16, 96);
			this.checkBoxMP3.Name = "checkBoxMP3";
			this.checkBoxMP3.Size = new System.Drawing.Size(56, 16);
			this.checkBoxMP3.TabIndex = 3;
			this.checkBoxMP3.Text = "MP3";
			this.checkBoxMP3.CheckedChanged += new System.EventHandler(this.FileTypeCheckBoxChanged);
			// 
			// checkBoxWMA
			// 
			this.checkBoxWMA.Checked = true;
			this.checkBoxWMA.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxWMA.Location = new System.Drawing.Point(16, 120);
			this.checkBoxWMA.Name = "checkBoxWMA";
			this.checkBoxWMA.Size = new System.Drawing.Size(56, 16);
			this.checkBoxWMA.TabIndex = 4;
			this.checkBoxWMA.Text = "WMA";
			this.checkBoxWMA.CheckedChanged += new System.EventHandler(this.FileTypeCheckBoxChanged);
			// 
			// checkBoxOGG
			// 
			this.checkBoxOGG.Checked = true;
			this.checkBoxOGG.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxOGG.Location = new System.Drawing.Point(16, 144);
			this.checkBoxOGG.Name = "checkBoxOGG";
			this.checkBoxOGG.Size = new System.Drawing.Size(56, 16);
			this.checkBoxOGG.TabIndex = 5;
			this.checkBoxOGG.Text = "OGG";
			this.checkBoxOGG.CheckedChanged += new System.EventHandler(this.FileTypeCheckBoxChanged);
			// 
			// groupFileTypes
			// 
			this.groupFileTypes.Controls.Add(this.checkBoxOGG);
			this.groupFileTypes.Controls.Add(this.checkBoxMP3);
			this.groupFileTypes.Controls.Add(this.checkBoxWAV);
			this.groupFileTypes.Controls.Add(this.checkBoxFLAC);
			this.groupFileTypes.Controls.Add(this.checkBoxWMA);
			this.groupFileTypes.Controls.Add(this.checkBoxAIFF);
			this.groupFileTypes.Location = new System.Drawing.Point(400, 288);
			this.groupFileTypes.Name = "groupFileTypes";
			this.groupFileTypes.Size = new System.Drawing.Size(80, 168);
			this.groupFileTypes.TabIndex = 5;
			this.groupFileTypes.TabStop = false;
			this.groupFileTypes.Text = "File Types:";
			this.groupFileTypes.Enter += new System.EventHandler(this.groupBox1_Enter);
			// 
			// groupFavorites
			// 
			this.groupFavorites.Controls.Add(this.textBoxDefaultFavorites);
			this.groupFavorites.Location = new System.Drawing.Point(8, 408);
			this.groupFavorites.Name = "groupFavorites";
			this.groupFavorites.Size = new System.Drawing.Size(384, 48);
			this.groupFavorites.TabIndex = 3;
			this.groupFavorites.TabStop = false;
			this.groupFavorites.Text = "Default Favorites Folder:";
			// 
			// textBoxDefaultFavorites
			// 
			this.textBoxDefaultFavorites.Location = new System.Drawing.Point(8, 17);
			this.textBoxDefaultFavorites.Name = "textBoxDefaultFavorites";
			this.textBoxDefaultFavorites.Size = new System.Drawing.Size(368, 20);
			this.textBoxDefaultFavorites.TabIndex = 0;
			this.textBoxDefaultFavorites.TextChanged += new System.EventHandler(this.textBoxDefaultFavorites_TextChanged);
			// 
			// groupSoundOutput
			// 
			this.groupSoundOutput.Controls.Add(this.comboOutputDevice);
			this.groupSoundOutput.Location = new System.Drawing.Point(8, 464);
			this.groupSoundOutput.Name = "groupSoundOutput";
			this.groupSoundOutput.Size = new System.Drawing.Size(472, 48);
			this.groupSoundOutput.TabIndex = 4;
			this.groupSoundOutput.TabStop = false;
			this.groupSoundOutput.Text = "Sound Output:";
			// 
			// comboOutputDevice
			// 
			this.comboOutputDevice.Location = new System.Drawing.Point(8, 16);
			this.comboOutputDevice.Name = "comboOutputDevice";
			this.comboOutputDevice.Size = new System.Drawing.Size(456, 21);
			this.comboOutputDevice.TabIndex = 0;
			this.comboOutputDevice.SelectedIndexChanged += new System.EventHandler(this.comboOutputDevice_SelectedIndexChanged);
			// 
			// ConfigurationForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(488, 540);
			this.Controls.Add(this.groupSoundOutput);
			this.Controls.Add(this.groupFavorites);
			this.Controls.Add(this.groupFileTypes);
			this.Controls.Add(this.groupGeneral);
			this.Controls.Add(this.groupDirectories);
			this.Controls.Add(this.groupCategories);
			this.Controls.Add(this.buttonOk);
			this.Controls.Add(this.buttonCancel);
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ConfigurationForm";
			this.ShowInTaskbar = false;
			this.StartPosition = FormStartPosition.CenterParent;
			this.Text = "Configuration";
			this.Load += new System.EventHandler(this.InitFromConfigFile);
			this.TextChanged += new System.EventHandler(this.OnSearchFoldersChanged);
			this.groupCategories.ResumeLayout(false);
			this.groupCategories.PerformLayout();
			this.groupDirectories.ResumeLayout(false);
			this.groupDirectories.PerformLayout();
			this.groupGeneral.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.numericSampleDisplaySizeH)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericSampleDisplaySizeW)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericAutoplayRepeats)).EndInit();
			this.groupFileTypes.ResumeLayout(false);
			this.groupFavorites.ResumeLayout(false);
			this.groupFavorites.PerformLayout();
			this.groupSoundOutput.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		private void button1_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}

		private void buttonOK_Click(object sender, EventArgs e)
		{
			if (this.needToRescanFolders)
			{
				this.DialogResult = DialogResult.Retry;
			}
			else if (this.dataIsDirty)
			{
				this.DialogResult = DialogResult.OK;
			}
			else
			{
				this.DialogResult = DialogResult.Cancel;
			}

			this.SaveToConfigFile();
			this.Close();
		}

		private static void ValidateRegex(string regex)
		{
			try
			{
				// ReSharper disable once ReturnValueOfPureMethodIsNotUsed
				// Just checking if it's valid regex
				new Regex(regex).Match("test");
			}
			catch (ArgumentException)
			{
				MessageBox.Show("Invalid regular expression. Please see the documentation for regular expression syntax.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
		}

//==================== DECONTAMINATED ZONE, START ==================================================

		private static Category CreateCategory(
			string name,
			string wildcard,
			bool useRegex
		) {
			return MainForm.app.Library.CreateCategory(
				 name,
				useRegex
					? null
					: wildcard
						.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
						.ToList(),
				useRegex,
				useRegex
					? wildcard
					: null
			);
		}

		private void EmptyCategoryCreationControls()
		{
			this.textName.Text = "";
			this.textWildcard.Text = "";
			this.checkBoxUseRegularExpressions.Checked = false;
		}

		private void buttonInsert_Click(object sender, EventArgs e)
		{
			var name = this.textName.Text;
			var wildcard = this.textWildcard.Text;
			var useRegularExpressions = this.checkBoxUseRegularExpressions.Checked;

			if (useRegularExpressions)
			{
				ValidateRegex(wildcard);
			}

			if (name.Length > 0)
			{
				var category = CreateCategory(name, wildcard, useRegularExpressions);
				this.configFile.Categories.Add(category);

				this.EmptyCategoryCreationControls();

				//this.listCategories.Items.Add(GetCategoryListName(category));
				this.UpdateCategoryList();
			}

			this.dataIsDirty = true;
			this.needToRescanFolders = true;
		}

		private void buttonDelete_Click(object sender, EventArgs e)
		{
			// Delete selected item from list!
			this.configFile.Categories.RemoveAt(this.listCategories.SelectedIndex);
			this.UpdateCategoryList();

			this.dataIsDirty = true;
			this.needToRescanFolders = true;
		}

		private void OnSearchFoldersChanged(object sender, EventArgs e)
		{
			this.dataIsDirty = true;
			this.needToRescanFolders = true;
		}

		private void groupCategories_Enter(object sender, EventArgs e)
		{
		}

		private void buttonReplace_Click(object sender, EventArgs e)
		{
			// Replace selected item in list!
			var index = this.listCategories.SelectedIndex;

			if (index <= 0)
			{
				return; // should never get here
			}

			var name = this.textName.Text;
			var wildcard = this.textWildcard.Text;
			var useRegularExpressions = this.checkBoxUseRegularExpressions.Checked;

			if (useRegularExpressions)
			{
				ValidateRegex(wildcard);
			}

			if (name.Length > 0)
			{
				var category = CreateCategory(name, wildcard, useRegularExpressions);
				this.configFile.Categories[index] = category;

				this.textName.Text = "";
				this.textWildcard.Text = "";
				this.checkBoxUseRegularExpressions.Checked = false;

				this.listCategories.Items[index] = ""; // clear the value before setting it, otherwise it won't update for case changes (eg. a to A)
				this.listCategories.Items[index] = GetCategoryListName(category);
			}

			this.dataIsDirty = true;
			this.needToRescanFolders = true;
		}

		private void listCategories_SelectedIndexChanged(object sender, EventArgs e)
		{
			// Replace selected item in list!
			var index = this.listCategories.SelectedIndex;
			if (index == -1)
				return; // should never get here

			// disable replace/delete buttons for All Samples and don't copy text to edit fields
			this.buttonReplace.Enabled = index != 0;
			this.buttonDelete.Enabled = index != 0;
			this.buttonMoveUp.Enabled = index > 1;
			this.buttonMoveDown.Enabled = index > 0 && index < this.listCategories.Items.Count - 1;

			if (index == 0)
			{
				this.textName.Text = "";
				this.textWildcard.Text = "";
				this.checkBoxUseRegularExpressions.Checked = false;
				return;
			}

			this.textName.Text = this.configFile.Categories[index].Name;
			this.textWildcard.Text = string.Join(",", this.configFile.Categories[index].SearchStrings);
			this.checkBoxUseRegularExpressions.Checked = this.configFile.Categories[index].UseRegex;
		}

		private void groupBox1_Enter(object sender, EventArgs e)
		{
		}

		private void textBoxDefaultFavorites_TextChanged(object sender, EventArgs e)
		{
			this.dataIsDirty = true;
		}

		private void numericAutoplayRepeats_ValueChanged(object sender, EventArgs e)
		{
			this.dataIsDirty = true;
		}

		private void FileTypeCheckBoxChanged(object sender, EventArgs e)
		{
			this.dataIsDirty = true;
			this.needToRescanFolders = true;
			this.UpdateCheckboxes();
		}

		private void UpdateCheckboxes()
		{
			if (this.loading)
			{
				return;
			}

			this.configFile.RescanPrompt = this.checkBoxRescanPrompt.Checked;
			this.configFile.IncludeFilePaths = this.checkBoxIncludeFilePaths.Checked;
			this.configFile.Autoplay = this.checkBoxAutoplay.Checked;
			this.configFile.AlwaysOnTop = this.checkBoxAlwaysOnTop.Checked;

			this.configFile.LoadWav = this.checkBoxWAV.Checked;
			this.configFile.LoadAiff = this.checkBoxAIFF.Checked;
			this.configFile.LoadFlac = this.checkBoxFLAC.Checked;
			this.configFile.LoadMp3 = this.checkBoxMP3.Checked;
			this.configFile.LoadWma = this.checkBoxWMA.Checked;
			this.configFile.LoadOgg = this.checkBoxOGG.Checked;
		}

		private void UpdateCategoryList()
		{
			var selectedIndex = this.CategoryIndex;

			this.listCategories.Items.Clear();
			foreach (var category in this.configFile.Categories)
			{
				this.listCategories.Items.Add(GetCategoryListName(category));
			}

			// Restore selected item
			this.listCategories.SelectedIndex = selectedIndex;
		}

		private void NumericChanged(object sender, EventArgs e)
		{
			this.dataIsDirty = true;
			this.UpdateNumerics();
		}

		private void UpdateNumerics()
		{
			if (this.loading)
			{
				return;
			}

			this.configFile.SampleDisplaySizeW = (int)this.numericSampleDisplaySizeW.Value;
			this.configFile.SampleDisplaySizeH = (int)this.numericSampleDisplaySizeH.Value;
			this.configFile.AutoplayRepeats = (int)this.numericAutoplayRepeats.Value;
		}

//==================== DECONTAMINATED ZONE, END ====================================================

		private void checkBoxAlwaysOnTop_CheckedChanged(object sender, EventArgs e)
		{
			this.dataIsDirty = true;
			this.UpdateCheckboxes();
		}

		private void checkBoxRescanPrompt_CheckedChanged(object sender, EventArgs e)
		{
			this.dataIsDirty = true;
			this.UpdateCheckboxes();
		}

		private void checkBoxIncludeFilePaths_CheckedChanged(object sender, EventArgs e)
		{
			this.dataIsDirty = true;
			this.needToRescanFolders = true;
			this.UpdateCheckboxes();
		}

		private void checkBoxAutoplay_CheckedChanged(object sender, EventArgs e)
		{
			this.dataIsDirty = true;
			this.numericAutoplayRepeats.Enabled = this.checkBoxAutoplay.Checked;
			this.UpdateCheckboxes();
		}

		private void comboOutputDevice_SelectedIndexChanged(object sender, EventArgs e)
		{
			this.ChangeSoundOutput();
			this.dataIsDirty = true;
		}

		private void linkLabelUseRegularExpressionsHelp_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start("https://msdn.microsoft.com/en-us/library/az24scfc%28v=vs.110%29.aspx");
		}

		private void ButtonMove(bool bUp)
		{
			// Move selected item in list up or down one
			var index = this.listCategories.SelectedIndex;

			if (bUp && index <= 1)
			{
				return; // should never get here
			}

			if (!bUp && index >= this.listCategories.Items.Count - 1)
			{
				return; // should never get here
			}

			var targetIndex = bUp ? index - 1 : index + 1;
			var bottomIndex = Math.Min(index, targetIndex);
			var topIndex = Math.Max(index, targetIndex);
			var bottomCategory = this.configFile.Categories[bottomIndex];
			this.configFile.Categories.RemoveAt(bottomIndex);
			this.configFile.Categories.Insert(topIndex, bottomCategory);

			this.listCategories.SelectedIndex = targetIndex;

			this.UpdateCategoryList();

			this.dataIsDirty = true;
			this.needToRescanFolders = true;
		}

		private void buttonMoveUp_Click(object sender, EventArgs e)
		{
			this.ButtonMove(true);
		}

		private void buttonMoveDown_Click(object sender, EventArgs e)
		{
			this.ButtonMove(false);
		}
	}
}