using System;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;

namespace Aural_Probe
{
	/// <summary>
	/// Class emulates long process which runs in worker thread
	/// and makes synchronous user UI operations.
	/// </summary>
	public class ScanFoldersProcess
	{
		private static readonly string[] FormatStrings = { "*.wav", "*.mp3", "*.wma", "*.ogg", "*.aif", "*.flac" };
		private static readonly bool[] FormatFlag = { true, true, true, true, true, true, };

		// Main thread sets this event to stop worker thread:
		private ManualResetEvent m_EventStop;

		// Worker thread sets this event when it is stopped:
		private ManualResetEvent m_EventStopped;

		// Reference to progress bar
		private ProgressBar m_progressBar;

		bool m_bUseCache;

		// Reference to progress bar
		private MainForm m_mainForm;

		bool bCancelled;

		int nFileCount;
		int nDirectoryCount;

		public ScanFoldersProcess(
			ManualResetEvent eventStop, 
			ManualResetEvent eventStopped,
			bool bUseCache,
			ProgressBar progressBar,
			MainForm mainForm)
		{
			this.bCancelled = false;
			this.m_bUseCache = bUseCache;
			this.m_EventStop = eventStop;
			this.m_EventStopped = eventStopped;
			this.m_mainForm = mainForm;
			this.m_progressBar = progressBar;
		}

		public bool IsDirectoryHidden(string dir)
		{
			var info = new DirectoryInfo(dir);
			return
				info.Parent != null &&
				(info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden; // ignore root directories, which may show up as hidden
		}

		public int CountDirectoriesInDirectory(string dir)
		{
			try
			{
				if (this.IsDirectoryHidden(dir))
				{
					return 0;
				}

				if (!Directory.Exists(dir))
				{
					return 0;
				}

				// check if thread is cancelled
				if (this.m_EventStop.WaitOne(0, true))
				{
					this.bCancelled = true;
					return 0;
				}

				this.nDirectoryCount++;
				if (!this.m_progressBar.IsDisposed && this.m_progressBar.IsHandleCreated)
				{
					this.m_progressBar.Invoke(this.m_progressBar.m_DelegateUpdateLabel, this.nDirectoryCount + " folder(s) found");
				}

				var nSubDirectories = 1;
				foreach (var s in Directory.GetDirectories(dir))
				{
					if (!this.bCancelled)
						nSubDirectories += this.CountDirectoriesInDirectory(s);
				}

				return nSubDirectories;
			}
			catch
			{
				// something went wrong at the OS level - perhaps the user doesn't have permissions.
				return 0;
			}
		}

		public int CountFilesInDirectory(string dir)
		{
			try
			{
				if (this.IsDirectoryHidden(dir))
				{
					return 0;
				}

				if (!Directory.Exists(dir))
				{
					return 0;
				}

				// check if thread is cancelled
				if (this.m_EventStop.WaitOne(0, true))
				{
					this.bCancelled = true;
					return 0;
				}

				var nFiles = Directory.GetFiles(dir).Length;
				foreach (var s in Directory.GetDirectories(dir))
				{
					if (!this.bCancelled)
					{
						nFiles += this.CountFilesInDirectory(s);
					}
				}

				this.nFileCount += nFiles;
				if (!this.m_progressBar.IsDisposed && this.m_progressBar.IsHandleCreated)
				{
					this.m_progressBar.Invoke(this.m_progressBar.m_DelegateUpdateLabel, this.nFileCount + " file(s) found");
				}
				
				return nFiles;
			}
			catch
			{
				// something went wrong at the OS level - perhaps the user doesn't have permissions.
				return 0;
			}
		}

		public bool AddSample(string s, int nColorIndex)
		{
			try
			{
				if (this.bCancelled)
				{
					// inform main thread that this thread stopped
					this.m_EventStopped.Set();
					return false;
				}

				// check if thread is cancelled
				if (this.m_EventStop.WaitOne(0, true))
				{
					this.bCancelled = true;
					// clean up
					this.m_mainForm.ClearSamples();

					// inform main thread that this thread stopped
					this.m_EventStopped.Set();
					return false;
				}

				this.m_mainForm.sampleList[this.m_mainForm.lnSamples] = s;
				this.m_mainForm.sampleColorIndex[this.m_mainForm.lnSamples] = nColorIndex;
				
				if (!MainForm.configFile.IncludeFilePaths)
				{
					var sSplit = s.Split('\\');
					s = sSplit[sSplit.Length - 1];
				}

				for (var i = 0; i < MainForm.configFile.Categories.Count; ++i)
				{
					if (MainForm.configFile.Categories[i].SearchStrings.Count == 0)
					{
						// Special case for "Everything" category
						this.m_mainForm.sampleIndices[i, this.m_mainForm.sampleIndicesCount[i]] = this.m_mainForm.lnSamples;
						++this.m_mainForm.sampleIndicesCount[i];
					}
					else
					{
						if (MainForm.configFile.Categories[i].UseRegex)
						{
							var regex = new Regex(MainForm.configFile.Categories[i].SearchStrings[0], RegexOptions.IgnoreCase);
							var match = regex.Match(s);
							if (match.Success)
							{
								this.m_mainForm.sampleIndices[i, this.m_mainForm.sampleIndicesCount[i]] = this.m_mainForm.lnSamples;
								this.m_mainForm.sampleIndicesCount[i]++;
							}
						}
						else
						{
							foreach (var searchString in MainForm.configFile.Categories[i].SearchStrings)
							{
								if (s.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) != -1)
								{
									this.m_mainForm.sampleIndices[i, this.m_mainForm.sampleIndicesCount[i]] = this.m_mainForm.lnSamples;
									this.m_mainForm.sampleIndicesCount[i]++;
									break;
								}
							}
						}
					}
				}

				this.m_mainForm.lnSamples++;
				return true;
			}
			catch (Exception e)
			{
				MessageBox.Show("AddSample " + s + " " + e, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return false;
			}
		}

		public bool PopulateSamplesFromCache()
		{
			if (File.Exists(MainForm.GetSampleCacheFilepath()))
			{
				try
				{
					using (Stream myFileStream = File.OpenRead(MainForm.GetSampleCacheFilepath()))
					{
						var deserializer = new BinaryFormatter();
					
						var nCacheVersion = (int)deserializer.Deserialize(myFileStream);
						if (nCacheVersion == 1)
						{
							var nSamples = (int)deserializer.Deserialize(myFileStream);
							MainForm.app.Library.AllocateSampleData(nSamples);

							if (!this.m_progressBar.IsDisposed && this.m_progressBar.IsHandleCreated)
							{
								this.m_progressBar.Invoke(this.m_progressBar.m_DelegateUpdateMaximumAndStep, nSamples, nSamples / 20);
							}

							for (var i = 0; i < nSamples; ++i)
							{
								var sampleName = (string)deserializer.Deserialize(myFileStream);
								var sampleColorIndex = (int)deserializer.Deserialize(myFileStream);

								if (!this.AddSample(sampleName, sampleColorIndex))
								{
									return false;
								}

								if (!this.m_progressBar.IsDisposed && this.m_progressBar.IsHandleCreated && i %this.m_progressBar.progressBar1.Step == 0)
								{
									this.m_progressBar.Invoke(this.m_progressBar.m_DelegateUpdateForm, null);
								}
							}
						}
					}

					return true;
				}
				catch (Exception ex)
				{
					MessageBox.Show(
						"Error! Could not populate sample list from cache! " + ex,
						"Error!",
						MessageBoxButtons.OK,
						MessageBoxIcon.Exclamation);

					try
					{
						File.Delete(MainForm.GetSampleCacheFilepath());
					} 
					catch (Exception ex2)
					{
						MessageBox.Show(
							"Could not delete sample cache! " + ex2,
							"Error!",
							MessageBoxButtons.OK,
							MessageBoxIcon.Exclamation);
					}

					this.m_mainForm.ClearSamples();
					return false;
				}
			}
			return false;
		}

		public bool PopulateSamplesFromDirectory(string dir)
		{
			try
			{
				if (this.IsDirectoryHidden(dir))
				{
					return true;
				}

				if (!Directory.Exists(dir))
				{
					return false;
				}

				if (this.bCancelled)
				{
					return false;
				}

				this.m_mainForm.nColorInc = (this.m_mainForm.nColorInc + 1) % MainForm.knMaxColors;
				var i = 0;

				foreach (var formatString in FormatStrings)
				{
					if (FormatFlag[i])
					{
						foreach (var s in Directory.GetFiles(dir, formatString))
						{
							if (!this.AddSample(s, this.m_mainForm.nColorInc))
							{
								return false;
							}
						}
					}
					i++;
				}

				foreach (var s in Directory.GetDirectories(dir))
				{
					if (!this.m_progressBar.IsDisposed && this.m_progressBar.IsHandleCreated)
					{
						this.m_progressBar.Invoke(this.m_progressBar.m_DelegateUpdateForm, null);
					}

					var bResult = this.PopulateSamplesFromDirectory(s);
					if (!bResult)
					{
						return false;
					}
				}
			}
			catch
			{
				// allow graceful fallthrough if we cannot access a protected folder
			}

			return true;
		}

		// Function runs in worker thread
		public void Run()
		{
			while (!this.m_progressBar.IsHandleCreated)
			{
			}

			try
			{
				this.bCancelled = false;

				FormatFlag[0] = MainForm.configFile.LoadWav;
				FormatFlag[1] = MainForm.configFile.LoadMp3;
				FormatFlag[2] = MainForm.configFile.LoadWma;
				FormatFlag[3] = MainForm.configFile.LoadOgg;
				FormatFlag[4] = MainForm.configFile.LoadAiff;
				FormatFlag[5] = MainForm.configFile.LoadFlac;

				this.m_mainForm.ClearSamples();
				if (this.m_bUseCache)
				{
					var bResult = this.PopulateSamplesFromCache();
					if (!bResult)
					{
						if (!this.bCancelled)
						{
							MessageBox.Show(
								"Could not load all samples.",
								"Not all samples loaded!",
								MessageBoxButtons.OK,
								MessageBoxIcon.Exclamation);
						}

						if (!this.m_progressBar.IsDisposed && this.m_progressBar.IsHandleCreated)
						{
							this.m_progressBar.Invoke(this.m_progressBar.m_DelegateThreadFinished, null);
						}

						if (this.bCancelled)
						{
							this.m_mainForm.ClearSamples();
						}

						return;
					}
				}
				else
				{
					this.nDirectoryCount = 0; // reset
					this.nFileCount = 0; // reset

					foreach (var dir in MainForm.configFile.SearchDirectoriesScrubbed)
					{
						this.nDirectoryCount += this.CountDirectoriesInDirectory(dir);
						this.nFileCount += this.CountFilesInDirectory(dir);
					}

					if (!this.bCancelled)
					{
						MainForm.app.Library.AllocateSampleData(this.nFileCount);
					}

					if (!this.m_progressBar.IsDisposed && this.m_progressBar.IsHandleCreated)
					{
						this.m_progressBar.Invoke(this.m_progressBar.m_DelegateUpdateMaximumAndStep, this.nDirectoryCount, -1);
					}

					foreach (var dir in MainForm.configFile.SearchDirectoriesScrubbed)
					{
						if (dir.Length == 0)
						{
							continue;
						}

						var bResult = this.PopulateSamplesFromDirectory(dir);
						if (!bResult)
						{
							if (!this.bCancelled)
							{
								MessageBox.Show(
									"Could not load all samples.",
									"Not all samples loaded!",
									MessageBoxButtons.OK,
									MessageBoxIcon.Exclamation);
							}

							if (!this.m_progressBar.IsDisposed && this.m_progressBar.IsHandleCreated)
							{
								this.m_progressBar.Invoke(this.m_progressBar.m_DelegateThreadFinished, null);
							}

							if (this.bCancelled)
							{
								this.m_mainForm.ClearSamples();
							}
							
							return;
						}
					}
				}
				if (!this.m_progressBar.IsDisposed && this.m_progressBar.IsHandleCreated)
				{
					this.m_progressBar.Invoke(this.m_progressBar.m_DelegateThreadFinished, null);
				}
			}
			catch (Exception e)
			{
				MessageBox.Show(
					"Run " + e,
					"Error!",
					MessageBoxButtons.OK,
					MessageBoxIcon.Exclamation);
			}
		}
	}
}