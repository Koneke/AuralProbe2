using System;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Aural_Probe
{
	/// <summary>
	/// Summary description for Configuration.
	/// </summary>

	// for stuff in the configuration menu
	// kinda weird and copied up from ConfigFile, but I guess this is the more "proper" way?
	public class ConfigurationForm : Form
	{
		private Button buttonOK;
		private Button buttonCancel;
		private ListBox listCategories;
		private TextBox textName;
		private TextBox textWildcard;
		private Label label2;
		private GroupBox groupCategories;
		private GroupBox groupDirectories;
		private GroupBox groupGeneral;
		private TextBox textDirectories;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public string[] categoryName;
		public string[,] categorySearchStrings;
		public bool[] categoryUseRegularExpressions;
		public string[] searchDirectories;
		public int lnNumCategories;
		public int[] lnNumCategorySearchStrings;
		public int lnNumSearchDirectories;
		private Button buttonDelete;
		private Button buttonInsert;
		private Button buttonReplace;
		public bool bDataDirty;
		private GroupBox groupBox1;
		private CheckBox checkBoxWAV;
		private CheckBox checkBoxAIFF;
		private CheckBox checkBoxFLAC;
		private CheckBox checkBoxMP3;
		private CheckBox checkBoxWMA;
		private CheckBox checkBoxOGG;
		private Label label1;
		private FolderBrowserDialog folderBrowserDialog1;
		private GroupBox groupBox2;
		private TextBox textBoxDefaultFavorites;
		private CheckBox checkBoxAutoplay;
		private NumericUpDown numericAutoplayRepeats;
		private Label label4;
		private CheckBox checkBoxRescanPrompt;
		private CheckBox checkBoxIncludeFilePaths;
		private Label label3;
		private NumericUpDown numericSampleDisplaySizeW;
		private Label label5;
		private NumericUpDown numericSampleDisplaySizeH;
		private GroupBox groupBox3;
		private ComboBox comboOutputDevice;
		private CheckBox checkBoxAlwaysOnTop;
		private CheckBox checkBoxUseRegularExpressions;
		private LinkLabel linkLabelUseRegularExpressionsHelp;
		private Button buttonMoveUp;
		private Button buttonMoveDown;
		public bool bNeedToRescanFolders;
		
		public ConfigurationForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			categoryName = new string[ConfigFile.MaxCategories];
			categorySearchStrings = new string[ConfigFile.MaxCategories, ConfigFile.MaxSearchStringsPerCategory];
			categoryUseRegularExpressions = new bool[ConfigFile.MaxCategories];
			searchDirectories = new string[ConfigFile.MaxDirectories];
			lnNumCategorySearchStrings = new int[ConfigFile.MaxCategories];
		}

		public void InitSoundOutputUI()
		{
			// Set up device output combo box
			FMOD.RESULT result;
			comboOutputDevice.Items.Clear();
			var nCurrentDriver = 0;
			var numDrivers = 0;
			var driverName = new StringBuilder(256);
			result = MainForm.app.fmodManager.systemFMOD.getDriver(ref nCurrentDriver);
			fmodUtils.ERRCHECK(result);
			
			// hack to select primary sound device - will this work???
			if (nCurrentDriver == -1)
			{
				nCurrentDriver = 0;
			}

			result = MainForm.app.fmodManager.systemFMOD.getNumDrivers(ref numDrivers);
			fmodUtils.ERRCHECK(result);
			for (var count = 0; count < numDrivers; count++)
			{
				var guid = new FMOD.GUID();
				result = MainForm.app.fmodManager.systemFMOD.getDriverInfo(count, driverName, driverName.Capacity, ref guid);
				fmodUtils.ERRCHECK(result);
				comboOutputDevice.Items.Add(driverName.ToString());
			}
			comboOutputDevice.SelectedIndex = nCurrentDriver;

		}

		public void ChangeSoundOutput()
		{
			var nSelectedOutputDevice = comboOutputDevice.SelectedIndex;

			if (nSelectedOutputDevice == 0)
			{
				nSelectedOutputDevice = -1;
			}

			if (nSelectedOutputDevice < comboOutputDevice.Items.Count)
			{
				if (MainForm.sound != null)
				{
					MainForm.sound.release();
				}

				MainForm.sound = null;

				MainForm.app.fmodManager.systemFMOD.close();
				MainForm.app.fmodManager.systemFMOD.setDriver(nSelectedOutputDevice);
				var result = MainForm.app.fmodManager.systemFMOD.init(32, FMOD.INITFLAGS.NORMAL, (IntPtr)null);
				fmodUtils.ERRCHECK(result);
			}
		}

		private void InitFromConfigFile(object sender, System.EventArgs e)
		{
			this.numericSampleDisplaySizeW.Value = MainForm.configFile.SampleDisplaySizeW;
			this.numericSampleDisplaySizeH.Value = MainForm.configFile.SampleDisplaySizeH;
			this.numericAutoplayRepeats.Value = MainForm.configFile.AutoplayRepeats;

			this.listCategories.Items.Clear();

			this.lnNumCategories = MainForm.configFile.Categories.Count;
			for (var i = 0; i < MainForm.configFile.Categories.Count; ++i)
			{
				this.categoryName[i] = MainForm.configFile.Categories[i].Name;
				this.lnNumCategorySearchStrings[i] = MainForm.configFile.Categories[i].SearchStrings.Count;
				var categoryListName = "";
				categoryListName += MainForm.configFile.Categories[i].Name + "\t";
				categoryListName += string.Join(",", MainForm.configFile.Categories[i].SearchStrings);

				this.categoryUseRegularExpressions[i] = MainForm.configFile.Categories[i].UseRegex;
				this.listCategories.Items.Add(categoryListName);
			}

			this.listCategories.SelectedIndex = MainForm.configFile.Categories.Count > 0 ? 0 : -1;
			this.checkBoxRescanPrompt.Checked = MainForm.configFile.RescanPrompt;
			this.checkBoxIncludeFilePaths.Checked = MainForm.configFile.IncludeFilePaths;
			this.checkBoxAutoplay.Checked = MainForm.configFile.Autoplay;
			this.checkBoxAlwaysOnTop.Checked = MainForm.configFile.AlwaysOnTop;
			this.numericAutoplayRepeats.Enabled = checkBoxAutoplay.Checked;

			this.checkBoxWAV.Checked = MainForm.configFile.LoadWav;
			this.checkBoxAIFF.Checked = MainForm.configFile.LoadAiff;
			this.checkBoxFLAC.Checked = MainForm.configFile.LoadFlac;
			this.checkBoxMP3.Checked = MainForm.configFile.LoadMp3;
			this.checkBoxWMA.Checked = MainForm.configFile.LoadWma;
			this.checkBoxOGG.Checked = MainForm.configFile.LoadOgg;

			this.textDirectories.Clear();
			this.lnNumSearchDirectories = MainForm.configFile.SearchDirectories.Count;

			var dirsTemp = new string[this.lnNumSearchDirectories];
			for (var i = 0; i < MainForm.configFile.SearchDirectories.Count; ++i)
			{
				dirsTemp[i] = this.searchDirectories[i] = MainForm.configFile.SearchDirectories[i];
			}
			this.textDirectories.Lines = dirsTemp;

			this.textBoxDefaultFavorites.Text = MainForm.configFile.DefaultFavoritesDirectory;

			this.InitSoundOutputUI();

			this.bDataDirty = false;
			this.bNeedToRescanFolders = false;
		}

		public void SaveToConfigFile()
		{
			if (bDataDirty)
			{
				MainForm.configFile.SampleDisplaySizeW = (int)numericSampleDisplaySizeW.Value;
				MainForm.configFile.SampleDisplaySizeH = (int)numericSampleDisplaySizeH.Value;
				MainForm.configFile.AutoplayRepeats = (int)numericAutoplayRepeats.Value;

				for (var i = 0; i < MainForm.configFile.Categories.Count; ++i)
				{
					MainForm.configFile.Categories[i].Name = categoryName[i];

					for (var j = 0; j < lnNumCategorySearchStrings[i]; ++j)
					{
						MainForm.configFile.Categories[i].SearchStrings[j] = categorySearchStrings[i,j];
					}

					MainForm.configFile.Categories[i].UseRegex = categoryUseRegularExpressions[i];
				}

				// do directories
				var directories = textDirectories.Text;
				var delimStr = ",;\n\t\r";
				var delimiter = delimStr.ToCharArray();
				string[] split = null;
				split = directories.Split(delimiter, ConfigFile.MaxSearchStringsPerCategory);
				lnNumSearchDirectories = 0;
				
				// update new search directories
				var invalidDirectories = "";
				for (var i = 0; i < split.Length; ++i)
				{
					if (split[i].Length > 0)
					{
						if (!Directory.Exists(split[i]))
							invalidDirectories += split[i] + "\n";

						MainForm.configFile.SearchDirectories[lnNumSearchDirectories] = searchDirectories[lnNumSearchDirectories] = split[i];
						lnNumSearchDirectories++;
					}
				}
				if (invalidDirectories != "")
					MessageBox.Show("Warning! The following search folders do not exist:\n\n" + invalidDirectories, "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);

				MainForm.configFile.RescanPrompt = checkBoxRescanPrompt.Checked;
				MainForm.configFile.IncludeFilePaths = checkBoxIncludeFilePaths.Checked;
				MainForm.configFile.Autoplay = checkBoxAutoplay.Checked;
				MainForm.configFile.AlwaysOnTop = checkBoxAlwaysOnTop.Checked;
				MainForm.configFile.DefaultFavoritesDirectory = textBoxDefaultFavorites.Text;

				MainForm.configFile.LoadWav = MainForm.configFile.LoadWav = checkBoxWAV.Checked;	
				MainForm.configFile.LoadAiff = MainForm.configFile.LoadAiff = checkBoxAIFF.Checked;	
				MainForm.configFile.LoadFlac = MainForm.configFile.LoadFlac = checkBoxFLAC.Checked;	
				MainForm.configFile.LoadMp3 = MainForm.configFile.LoadMp3 = checkBoxMP3.Checked;	
				MainForm.configFile.LoadWma = MainForm.configFile.LoadWma = checkBoxWMA.Checked;	
				MainForm.configFile.LoadOgg = MainForm.configFile.LoadOgg = checkBoxOGG.Checked;	

				MainForm.configFile.DefaultSoundDevice = comboOutputDevice.Text;

				MainForm.configFile.Save();

				this.bDataDirty = false;
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
			this.buttonOK = new System.Windows.Forms.Button();
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
			this.label2 = new System.Windows.Forms.Label();
			this.buttonReplace = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.groupDirectories = new System.Windows.Forms.GroupBox();
			this.textDirectories = new System.Windows.Forms.TextBox();
			this.groupGeneral = new System.Windows.Forms.GroupBox();
			this.numericSampleDisplaySizeH = new System.Windows.Forms.NumericUpDown();
			this.label3 = new System.Windows.Forms.Label();
			this.numericSampleDisplaySizeW = new System.Windows.Forms.NumericUpDown();
			this.checkBoxRescanPrompt = new System.Windows.Forms.CheckBox();
			this.checkBoxIncludeFilePaths = new System.Windows.Forms.CheckBox();
			this.numericAutoplayRepeats = new System.Windows.Forms.NumericUpDown();
			this.checkBoxAutoplay = new System.Windows.Forms.CheckBox();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.checkBoxAlwaysOnTop = new System.Windows.Forms.CheckBox();
			this.checkBoxWAV = new System.Windows.Forms.CheckBox();
			this.checkBoxAIFF = new System.Windows.Forms.CheckBox();
			this.checkBoxFLAC = new System.Windows.Forms.CheckBox();
			this.checkBoxMP3 = new System.Windows.Forms.CheckBox();
			this.checkBoxWMA = new System.Windows.Forms.CheckBox();
			this.checkBoxOGG = new System.Windows.Forms.CheckBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.textBoxDefaultFavorites = new System.Windows.Forms.TextBox();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.comboOutputDevice = new System.Windows.Forms.ComboBox();
			this.groupCategories.SuspendLayout();
			this.groupDirectories.SuspendLayout();
			this.groupGeneral.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.numericSampleDisplaySizeH)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericSampleDisplaySizeW)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericAutoplayRepeats)).BeginInit();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.groupBox3.SuspendLayout();
			this.SuspendLayout();
			// 
			// buttonOK
			// 
			this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOK.Location = new System.Drawing.Point(408, 16);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(72, 24);
			this.buttonOK.TabIndex = 6;
			this.buttonOK.Text = "&OK";
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
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
			this.groupCategories.Controls.Add(this.buttonMoveDown);
			this.groupCategories.Controls.Add(this.buttonMoveUp);
			this.groupCategories.Controls.Add(this.linkLabelUseRegularExpressionsHelp);
			this.groupCategories.Controls.Add(this.checkBoxUseRegularExpressions);
			this.groupCategories.Controls.Add(this.textName);
			this.groupCategories.Controls.Add(this.listCategories);
			this.groupCategories.Controls.Add(this.buttonInsert);
			this.groupCategories.Controls.Add(this.buttonDelete);
			this.groupCategories.Controls.Add(this.textWildcard);
			this.groupCategories.Controls.Add(this.label2);
			this.groupCategories.Controls.Add(this.buttonReplace);
			this.groupCategories.Controls.Add(this.label1);
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
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(224, 64);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(152, 16);
			this.label2.TabIndex = 3;
			this.label2.Text = "Search Criteria:";
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
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(224, 16);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(152, 16);
			this.label1.TabIndex = 1;
			this.label1.Text = "Name:";
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
			this.textDirectories.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textDirectories_KeyDown);
			// 
			// groupGeneral
			// 
			this.groupGeneral.Controls.Add(this.numericSampleDisplaySizeH);
			this.groupGeneral.Controls.Add(this.label3);
			this.groupGeneral.Controls.Add(this.numericSampleDisplaySizeW);
			this.groupGeneral.Controls.Add(this.checkBoxRescanPrompt);
			this.groupGeneral.Controls.Add(this.checkBoxIncludeFilePaths);
			this.groupGeneral.Controls.Add(this.numericAutoplayRepeats);
			this.groupGeneral.Controls.Add(this.checkBoxAutoplay);
			this.groupGeneral.Controls.Add(this.label4);
			this.groupGeneral.Controls.Add(this.label5);
			this.groupGeneral.Controls.Add(this.checkBoxAlwaysOnTop);
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
			this.numericSampleDisplaySizeH.ValueChanged += new System.EventHandler(this.numericSampleDisplaySizeH_ValueChanged);
			this.numericSampleDisplaySizeH.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.numericSampleDisplaySizeH_ValueChanged);
			this.numericSampleDisplaySizeH.Leave += new System.EventHandler(this.numericSampleDisplaySizeH_ValueChanged);
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(16, 16);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(112, 16);
			this.label3.TabIndex = 0;
			this.label3.Text = "Sample Display Size:";
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
			this.numericSampleDisplaySizeW.ValueChanged += new System.EventHandler(this.numericSampleDisplaySizeW_ValueChanged);
			this.numericSampleDisplaySizeW.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.numericSampleDisplaySizeW_ValueChanged);
			this.numericSampleDisplaySizeW.Leave += new System.EventHandler(this.numericSampleDisplaySizeW_ValueChanged);
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
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(304, 65);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(56, 16);
			this.label4.TabIndex = 9;
			this.label4.Text = "time(s)";
			// 
			// label5
			// 
			this.label5.Location = new System.Drawing.Point(66, 36);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(16, 16);
			this.label5.TabIndex = 2;
			this.label5.Text = "x";
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
			this.checkBoxWAV.CheckedChanged += new System.EventHandler(this.checkBoxWAV_CheckedChanged);
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
			this.checkBoxAIFF.CheckedChanged += new System.EventHandler(this.checkBoxAIFF_CheckedChanged);
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
			this.checkBoxFLAC.CheckedChanged += new System.EventHandler(this.checkBoxFLAC_CheckedChanged);
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
			this.checkBoxMP3.CheckedChanged += new System.EventHandler(this.checkBoxMP3_CheckedChanged);
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
			this.checkBoxWMA.CheckedChanged += new System.EventHandler(this.checkBoxWMA_CheckedChanged);
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
			this.checkBoxOGG.CheckedChanged += new System.EventHandler(this.checkBoxOGG_CheckedChanged);
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.checkBoxOGG);
			this.groupBox1.Controls.Add(this.checkBoxMP3);
			this.groupBox1.Controls.Add(this.checkBoxWAV);
			this.groupBox1.Controls.Add(this.checkBoxFLAC);
			this.groupBox1.Controls.Add(this.checkBoxWMA);
			this.groupBox1.Controls.Add(this.checkBoxAIFF);
			this.groupBox1.Location = new System.Drawing.Point(400, 288);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(80, 168);
			this.groupBox1.TabIndex = 5;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "File Types:";
			this.groupBox1.Enter += new System.EventHandler(this.groupBox1_Enter);
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.textBoxDefaultFavorites);
			this.groupBox2.Location = new System.Drawing.Point(8, 408);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(384, 48);
			this.groupBox2.TabIndex = 3;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Default Favorites Folder:";
			// 
			// textBoxDefaultFavorites
			// 
			this.textBoxDefaultFavorites.Location = new System.Drawing.Point(8, 17);
			this.textBoxDefaultFavorites.Name = "textBoxDefaultFavorites";
			this.textBoxDefaultFavorites.Size = new System.Drawing.Size(368, 20);
			this.textBoxDefaultFavorites.TabIndex = 0;
			this.textBoxDefaultFavorites.TextChanged += new System.EventHandler(this.textBoxDefaultFavorites_TextChanged);
			// 
			// groupBox3
			// 
			this.groupBox3.Controls.Add(this.comboOutputDevice);
			this.groupBox3.Location = new System.Drawing.Point(8, 464);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(472, 48);
			this.groupBox3.TabIndex = 4;
			this.groupBox3.TabStop = false;
			this.groupBox3.Text = "Sound Output:";
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
			this.Controls.Add(this.groupBox3);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.groupGeneral);
			this.Controls.Add(this.groupDirectories);
			this.Controls.Add(this.groupCategories);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.buttonCancel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ConfigurationForm";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
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
			this.groupBox1.ResumeLayout(false);
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.groupBox3.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		private void button1_Click(object sender, System.EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}

		private void textBox1_TextChanged(object sender, System.EventArgs e)
		{
		}

		private void buttonOK_Click(object sender, System.EventArgs e)
		{
			if (this.bNeedToRescanFolders)
			{
				this.DialogResult = DialogResult.Retry;
			}
			else if (this.bDataDirty)
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

		private void buttonInsert_Click(object sender, System.EventArgs e)
		{
			var name = this.textName.Text;
			var wildcard = this.textWildcard.Text;
			var useRegularExpressions = this.checkBoxUseRegularExpressions.Checked;

			if (useRegularExpressions)
			{
				try
				{
					var r = new Regex(wildcard);
					var m = r.Match("Test");
				}
				catch (ArgumentException)
				{
					MessageBox.Show("Invalid regular expression. Please see the documentation for regular expression syntax.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return;
				}
			}

			if (name.Length > 0)
			{
				categoryName[lnNumCategories] = name;
				categoryUseRegularExpressions[lnNumCategories] = useRegularExpressions;
				if (useRegularExpressions)
				{
					categorySearchStrings[lnNumCategories, 0] = wildcard;
					lnNumCategorySearchStrings[lnNumCategories] = 1;
				}
				else 
				{
					var delimStr = ",;";
					var delimiter = delimStr.ToCharArray();
					string[] split = null;
					split = wildcard.Split(delimiter, ConfigFile.MaxSearchStringsPerCategory);
					var i = 0;
					foreach (var s in split)
					{
						if (s.Length > 0)
						{
							categorySearchStrings[lnNumCategories, i] = s;
							i++;
						}
					}
					lnNumCategorySearchStrings[lnNumCategories] = i;
				}
				textName.Text = "";
				textWildcard.Text = "";
				checkBoxUseRegularExpressions.Checked = false;

				var categoryListName = "";
				categoryListName += categoryName[lnNumCategories] + "\t";
				for (int j = 0, k = lnNumCategorySearchStrings[lnNumCategories]; j < k; j++)
				{
					categorySearchStrings[lnNumCategories, j] = categorySearchStrings[lnNumCategories, j];
					categoryListName += categorySearchStrings[lnNumCategories, j];
					if (j < k - 1)
						categoryListName += ",";
				}
				listCategories.Items.Add(categoryListName);
				
				lnNumCategories++;
			}

			bDataDirty = true;
			bNeedToRescanFolders = true;
		}

		private void buttonDelete_Click(object sender, System.EventArgs e)
		{
			// Delete selected item from list!
			var index = listCategories.SelectedIndex;
			if (index > 0)
			{
				listCategories.Items.RemoveAt(index);
				lnNumCategorySearchStrings[index] = 0;
				lnNumCategories--;
				for (var i = index; i < lnNumCategories; i++)
				{
					lnNumCategorySearchStrings[i] = lnNumCategorySearchStrings[i+1];
					for (var j = 0; j < ConfigFile.MaxSearchStringsPerCategory; ++j)
						categorySearchStrings[i,j] = categorySearchStrings[i+1,j];
					categoryName[i] = categoryName[i+1];
					categoryUseRegularExpressions[i] = categoryUseRegularExpressions[i + 1];
				}
			}

			bDataDirty = true;
			bNeedToRescanFolders = true;
		}

		private void OnSearchFoldersChanged(object sender, System.EventArgs e)
		{
			bDataDirty = true;
			bNeedToRescanFolders = true;
		}

		private void OnSampleSizeChanged(object sender, System.EventArgs e)
		{
			bDataDirty = true;
		}

		private void groupCategories_Enter(object sender, System.EventArgs e)
		{
		
		}

		private void buttonReplace_Click(object sender, System.EventArgs e)
		{
			// Replace selected item in list!
			var index = listCategories.SelectedIndex;
			if (index <= 0)
				return; // should never get here

			var name = textName.Text.ToString();
			var wildcard = textWildcard.Text.ToString();
			var useRegularExpressions = checkBoxUseRegularExpressions.Checked;

			if (useRegularExpressions)
			{
				try
				{
					var r = new Regex(wildcard);
					var m = r.Match("Test");
				}
				catch (ArgumentException)
				{
					MessageBox.Show("Invalid regular expression. Please see the documentation for regular expression syntax.", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					return;
				}
			}

			if (name.Length > 0)
			{
				categoryName[index] = name;
				categoryUseRegularExpressions[index] = useRegularExpressions;
				if (useRegularExpressions)
				{
					categorySearchStrings[index, 0] = wildcard;
					lnNumCategorySearchStrings[index] = 1;
				}
				else
				{
					var delimStr = ",;";
					var delimiter = delimStr.ToCharArray();
					string[] split = null;
					split = wildcard.Split(delimiter, ConfigFile.MaxSearchStringsPerCategory);
					var i = 0;
					foreach (var s in split)
					{
						if (s.Length > 0)
						{
							categorySearchStrings[index, i] = s;
							i++;
						}
					}
					lnNumCategorySearchStrings[index] = i;
				}
				textName.Text = "";
				textWildcard.Text = "";
				checkBoxUseRegularExpressions.Checked = false;

				var categoryListName = "";
				categoryListName += categoryName[index] + "\t";
				for (int j = 0, k = lnNumCategorySearchStrings[index]; j < k; j++)
				{
					categorySearchStrings[index, j] = categorySearchStrings[index, j];
					categoryListName += categorySearchStrings[index, j];
					if (j < k - 1)
						categoryListName += ",";
				}
				listCategories.Items[index] = ""; // clear the value before setting it, otherwise it won't update for case changes (eg. a to A)
				listCategories.Items[index] = categoryListName;
			}

			bDataDirty = true;
			bNeedToRescanFolders = true;
		}

		private void listCategories_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			// Replace selected item in list!
			var index = listCategories.SelectedIndex;
			if (index == -1)
				return; // should never get here

			// disable replace/delete buttons for All Samples and don't copy text to edit fields
			buttonReplace.Enabled = index != 0;
			buttonDelete.Enabled = index != 0;
			buttonMoveUp.Enabled = index > 1;
			buttonMoveDown.Enabled = index > 0 && index < listCategories.Items.Count - 1;

			if (index == 0)
			{
				textName.Text = "";
				textWildcard.Text = "";
				checkBoxUseRegularExpressions.Checked = false;
				return;
			}

			var wildcard = "";
			for (int j = 0, k = lnNumCategorySearchStrings[index]; j < k; j++)
			{
				wildcard += categorySearchStrings[index, j];
				if (j < k - 1)
					wildcard += ",";
			}

			textName.Text = categoryName[index];
			textWildcard.Text = wildcard;
			checkBoxUseRegularExpressions.Checked = categoryUseRegularExpressions[index];
		}

		private void OnSampleSizeKeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			OnSampleSizeChanged(sender, e);
		}

		private void checkBoxRescanPrompt_CheckedChanged(object sender, System.EventArgs e)
		{
			bDataDirty = true;
		}

		private void checkBoxIncludeFilePaths_CheckedChanged(object sender, System.EventArgs e)
		{
			bDataDirty = true;
			bNeedToRescanFolders = true;
		}

		private void groupBox1_Enter(object sender, System.EventArgs e)
		{
		
		}

		private void textBoxDefaultFavorites_TextChanged(object sender, System.EventArgs e)
		{
			bDataDirty = true;
		}

		private void checkBoxAutoplay_CheckedChanged(object sender, System.EventArgs e)
		{
			bDataDirty = true;
			numericAutoplayRepeats.Enabled = checkBoxAutoplay.Checked;
		}

		private void numericAutoplayRepeats_ValueChanged(object sender, System.EventArgs e)
		{
			bDataDirty = true;
		}

		private void checkBoxWAV_CheckedChanged(object sender, System.EventArgs e)
		{
			bDataDirty = true;
			bNeedToRescanFolders = true;
		}

		private void checkBoxAIFF_CheckedChanged(object sender, System.EventArgs e)
		{
			bDataDirty = true;
			bNeedToRescanFolders = true;
		}

		private void checkBoxFLAC_CheckedChanged(object sender, System.EventArgs e)
		{
			bDataDirty = true;
			bNeedToRescanFolders = true;
		}

		private void checkBoxMP3_CheckedChanged(object sender, System.EventArgs e)
		{
			bDataDirty = true;
			bNeedToRescanFolders = true;
		}

		private void checkBoxWMA_CheckedChanged(object sender, System.EventArgs e)
		{
			bDataDirty = true;
			bNeedToRescanFolders = true;
		}

		private void checkBoxOGG_CheckedChanged(object sender, System.EventArgs e)
		{
			bDataDirty = true;
		}

		private void numericSampleDisplaySizeW_ValueChanged(object sender, System.EventArgs e)
		{
			bDataDirty = true;
		}

		private void checkBoxAlwaysOnTop_CheckedChanged(object sender, System.EventArgs e)
		{
			bDataDirty = true;
		}

		private void comboOutputDevice_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			ChangeSoundOutput();
			bDataDirty = true;
		}

		private void numericSampleDisplaySizeH_ValueChanged(object sender, System.Windows.Forms.KeyPressEventArgs e)
		{
		
		}

		private void numericAutoplayRepeats_ValueChanged(object sender, System.Windows.Forms.KeyPressEventArgs e)
		{
		
		}

		private void numericSampleDisplaySizeH_ValueChanged(object sender, System.EventArgs e)
		{
			bDataDirty = true;
		}

		private void numericSampleDisplaySizeW_ValueChanged(object sender, System.Windows.Forms.KeyPressEventArgs e)
		{
		
		}

		private void textDirectories_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Control && e.KeyCode == Keys.A)
			{
				((TextBox)sender).SelectAll();
				e.Handled = true;
			}
		}

		private void linkLabelUseRegularExpressionsHelp_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start("https://msdn.microsoft.com/en-us/library/az24scfc%28v=vs.110%29.aspx");
		}

		public static void Swap<T>(ref T lhs, ref T rhs)
		{
			var temp = lhs;
			lhs = rhs;
			rhs = temp;
		}

		private void buttonMove(bool bUp)
		{
			// Move selected item in list up or down one
			var index = listCategories.SelectedIndex;
			if (bUp && index <= 1)
				return; // should never get here
			if (!bUp && index >= listCategories.Items.Count - 1)
				return; // should never get here

			var targetIndex = bUp ? index - 1 : index + 1;

			Swap(ref categoryName[targetIndex], ref categoryName[index]);
			Swap(ref categoryUseRegularExpressions[targetIndex], ref categoryUseRegularExpressions[index]);
			for (var i = 0; i < ConfigFile.MaxSearchStringsPerCategory; ++i)
				Swap(ref categorySearchStrings[targetIndex, i], ref categorySearchStrings[index, i]);
			Swap(ref lnNumCategorySearchStrings[targetIndex], ref lnNumCategorySearchStrings[index]);

			var tmp = (string)listCategories.Items[targetIndex];
			listCategories.Items[targetIndex] = listCategories.Items[index]; // clear the value before setting it, otherwise it won't update for case changes (eg. a to A)
			listCategories.Items[index] = tmp;

			listCategories.SelectedIndex = targetIndex;

			bDataDirty = true;
			bNeedToRescanFolders = true;
		}
		private void buttonMoveUp_Click(object sender, EventArgs e)
		{
			buttonMove(true);
		}

		private void buttonMoveDown_Click(object sender, EventArgs e)
		{
			buttonMove(false);
		}
	}
}