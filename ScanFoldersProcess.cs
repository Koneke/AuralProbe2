using System;
using System.IO;
using System.Linq;
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
		private ProgressBar progressBar;

		bool useCache;

		// Reference to progress bar
		private MainForm mainForm;

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
			this.useCache = bUseCache;
			this.m_EventStop = eventStop;
			this.m_EventStopped = eventStopped;
			this.mainForm = mainForm;
			this.progressBar = progressBar;
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
				if (!this.progressBar.IsDisposed && this.progressBar.IsHandleCreated)
				{
					this.progressBar.Invoke(this.progressBar.m_DelegateUpdateLabel, this.nDirectoryCount + " folder(s) found");
				}

				var nSubDirectories = 1;
				foreach (var s in Directory.GetDirectories(dir))
				{
					if (!this.bCancelled)
					{
						nSubDirectories += this.CountDirectoriesInDirectory(s);
					}
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
				if (!this.progressBar.IsDisposed && this.progressBar.IsHandleCreated)
				{
					this.progressBar.Invoke(this.progressBar.m_DelegateUpdateLabel, this.nFileCount + " file(s) found");
				}
				
				return nFiles;
			}
			catch
			{
				// something went wrong at the OS level - perhaps the user doesn't have permissions.
				return 0;
			}
		}

		public bool AddSample(string path, int nColorIndex)
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
					this.mainForm.ClearSamples();

					// inform main thread that this thread stopped
					this.m_EventStopped.Set();
					return false;
				}

				var sample = new Sample(path)
				{
					ColorIndex = nColorIndex
				};
				
				if (!MainForm.configFile.IncludeFilePaths)
				{
					var sSplit = path.Split('\\');
					path = sSplit[sSplit.Length - 1];
				}

				foreach (var category in MainForm.configFile.Categories)
				{
					// if this is the "All samples" category
					if (category == MainForm.configFile.Categories.First())
					{
						category.Samples.Add(sample);
					}
					else
					{
						if (category.UseRegex)
						{
							var regex = new Regex(category.SearchStrings[0], RegexOptions.IgnoreCase);
							if (regex.Match(path).Success)
							{
								category.Samples.Add(sample);
							}
						}
						else
						{
							foreach (var searchString in category.SearchStrings)
							{
								if (path.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) != -1)
								{
									category.Samples.Add(sample);
									break;
								}
							}
						}
					}
				}

				return true;
			}
			catch (Exception e)
			{
				MessageBox.Show("AddSample " + path + " " + e, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return false;
			}
		}

		public bool PopulateSamplesFromCache()
		{
			if (File.Exists(MainForm.GetSampleCacheFilepath()))
			{
				try
				{
					var sampleCache = SampleCache.Load(MainForm.GetSampleCacheFilepath());
					var cacheSize = sampleCache.CacheSize;

					if (!this.progressBar.IsDisposed && this.progressBar.IsHandleCreated)
					{
						this.progressBar.Invoke(
							this.progressBar.m_DelegateUpdateMaximumAndStep,
							cacheSize,
							cacheSize / 20);
					}

					var i = 0;
					foreach (var cachedSample in sampleCache.Cache)
					{
						if (!this.AddSample(cachedSample.Path, cachedSample.ColorIndex))
						{
							return false;
						}

						if (!this.progressBar.IsDisposed &&
							this.progressBar.IsHandleCreated &&
							i++ % this.progressBar.progressBar1.Step == 0)
						{
							this.progressBar.Invoke(this.progressBar.m_DelegateUpdateForm, null);
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

					this.mainForm.ClearSamples();
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

				this.mainForm.nColorInc = (this.mainForm.nColorInc + 1) % MainForm.knMaxColors;
				var i = 0;

				foreach (var formatString in FormatStrings)
				{
					if (FormatFlag[i])
					{
						foreach (var s in Directory.GetFiles(dir, formatString))
						{
							if (!this.AddSample(s, this.mainForm.nColorInc))
							{
								return false;
							}
						}
					}
					i++;
				}

				foreach (var s in Directory.GetDirectories(dir))
				{
					if (!this.progressBar.IsDisposed && this.progressBar.IsHandleCreated)
					{
						this.progressBar.Invoke(this.progressBar.m_DelegateUpdateForm, null);
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
			while (!this.progressBar.IsHandleCreated)
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

				this.mainForm.ClearSamples();
				if (this.useCache)
				{
					var result = this.PopulateSamplesFromCache();

					if (!result)
					{
						if (!this.bCancelled)
						{
							MessageBox.Show(
								"Could not load all samples.",
								"Not all samples loaded!",
								MessageBoxButtons.OK,
								MessageBoxIcon.Exclamation);
						}

						if (!this.progressBar.IsDisposed && this.progressBar.IsHandleCreated)
						{
							this.progressBar.Invoke(this.progressBar.m_DelegateThreadFinished, null);
						}

						if (this.bCancelled)
						{
							this.mainForm.ClearSamples();
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

					if (!this.progressBar.IsDisposed && this.progressBar.IsHandleCreated)
					{
						this.progressBar.Invoke(this.progressBar.m_DelegateUpdateMaximumAndStep, this.nDirectoryCount, -1);
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

							if (!this.progressBar.IsDisposed && this.progressBar.IsHandleCreated)
							{
								this.progressBar.Invoke(this.progressBar.m_DelegateThreadFinished, null);
							}

							if (this.bCancelled)
							{
								this.mainForm.ClearSamples();
							}
							
							return;
						}
					}
				}
				if (!this.progressBar.IsDisposed && this.progressBar.IsHandleCreated)
				{
					this.progressBar.Invoke(this.progressBar.m_DelegateThreadFinished, null);
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