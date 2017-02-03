using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using FMOD;

namespace Aural_Probe
{
	public class FmodManager
	{
		private App app;
		public readonly FMOD.System SystemFmod;
		private static Sound sound;
		private readonly Channel channel = null;
		public readonly bool FmodInitialised;
		private bool bAutoPlayNextSample;
		public int nAutoPlayRepeatsLeft = 0;
		public FMOD.CHANNEL_CALLBACK cbFMOD = null;

		public FmodManager(App app)
		{
			this.app = app;

			try
			{
				uint version = 0;
				var result = Factory.System_Create(ref this.SystemFmod);

				fmodUtils.ERRCHECK(result);

				result = this.SystemFmod.getVersion(ref version);
				fmodUtils.ERRCHECK(result);

				if (version < VERSION.number)
				{
					MessageBox.Show("Error!  You are using an old version of FMOD " + version.ToString("X") + ".  This program requires " + VERSION.number.ToString("X") + ".");
					Application.Exit();
				}

				this.TrySettingOutputDevice();

				result = this.SystemFmod.init(32, INITFLAGS.NORMAL, (IntPtr)null);
				fmodUtils.ERRCHECK(result);

				this.FmodInitialised = true;
			}
			catch
			{
				MessageBox.Show(
					"Error! Could not find FMOD DLL.",
					"FMOD DLL missing!",
					MessageBoxButtons.OK,
					MessageBoxIcon.Exclamation);
				Environment.Exit(-1);
			}
		}

		private int GetNumberOfDrivers()
		{
			var numDrivers = -1;

			try
			{
				var result = MainForm.app.fmodManager.SystemFmod.getNumDrivers(ref numDrivers);
				fmodUtils.ERRCHECK(result);
			}
			catch (Exception e)
			{
				MessageBox.Show(
					"Failed to get number of audio drivers! \n" + e.Message,
					"FMOD error",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
			}

			return numDrivers;
		}

		public void TryPrimarySoundDeviceHack()
		{
			try
			{
				var currentDriver = this.GetCurrentDriver();
				var result = MainForm.app.fmodManager.SystemFmod.getDriver(ref currentDriver);
				fmodUtils.ERRCHECK(result);
				
				// hack to select primary sound device - will this work???
				if (currentDriver == -1)
				{
					this.SetCurrentDriver(0);
				}
			}
			catch (Exception e)
			{
				MessageBox.Show(
					"Failed to set current driver to primary audio device! \n" + e.Message,
					"FMOD error",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
			}
		}

		public void ChangeSoundOutput(int newDriver)
		{
			try
			{
			if (newDriver == 0)
			{
				newDriver = -1;
			}

			if (newDriver < this.GetNumberOfDrivers())
			{
				if (MainForm.sound != null)
				{
					MainForm.sound.release();
				}

				MainForm.sound = null;

				MainForm.app.fmodManager.SystemFmod.close();
				MainForm.app.fmodManager.SystemFmod.setDriver(newDriver);
				var result = MainForm.app.fmodManager.SystemFmod.init(32, INITFLAGS.NORMAL, (IntPtr)null);
				fmodUtils.ERRCHECK(result);
			}
			}
			catch (Exception e)
			{
				MessageBox.Show(
					"Failed to change sound output device! \n" + e.Message,
					"FMOD error",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
			}
		}

		// convert to getter/setter pair
		private void SetCurrentDriver(int driver)
		{
			try
			{
				var result = this.SystemFmod.setDriver(driver);
				fmodUtils.ERRCHECK(result);
			}
			catch (Exception e)
			{
				MessageBox.Show(
					"Failed to set current driver! \n" + e.Message,
					"FMOD error",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
			}
		}

		public int GetCurrentDriver()
		{
			try
			{
				var currentDriver = 0;
				var result = this.SystemFmod.getDriver(ref currentDriver);
				fmodUtils.ERRCHECK(result);

				return currentDriver;
			}
			catch (Exception e)
			{
				MessageBox.Show(
					"Failed to get current driver! \n" + e.Message,
					"FMOD error",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
			}

			return -1;
		}

		public List<string> GetAvailableDrivers()
		{
			try
			{
				var driverNames = new List<string>();
				var driverName = new StringBuilder(256);

				for (var count = 0; count < this.GetNumberOfDrivers(); count++)
				{
					var guid = new GUID();
					var result = MainForm.app.fmodManager.SystemFmod.getDriverInfo(
						count, driverName, driverName.Capacity, ref guid);

					fmodUtils.ERRCHECK(result);
					driverNames.Add(driverName.ToString());
				}

				return driverNames;
			}
			catch (Exception)
			{
				MessageBox.Show(
					"Failed to get number of audio drivers!",
					"FMOD error",
					MessageBoxButtons.OK,
					MessageBoxIcon.Error);
			}

			return null;
		}

		private void TrySettingOutputDevice()
		{
			var numDrivers = 0;
			var driverName = new StringBuilder(256);

			var result = this.SystemFmod.getNumDrivers(ref numDrivers);
			fmodUtils.ERRCHECK(result);

			for (var count = 0; count < numDrivers; count++)
			{
				var guid = new GUID();
				result = this.SystemFmod.getDriverInfo(count, driverName, driverName.Capacity, ref guid);
				fmodUtils.ERRCHECK(result);

				if (driverName.ToString() == this.app.Files.ConfigFile.DefaultSoundDevice)
				{
					result = this.SystemFmod.setDriver(count);
					fmodUtils.ERRCHECK(result);
				}
			}
		}

		public void StopSoundPlayback()
		{
			if (this.channel != null)
			{
				var bPlaying = false;
				this.channel.isPlaying(ref bPlaying);
				if (bPlaying)
				{
					this.channel.stop();
					this.bAutoPlayNextSample = false;
				}
			}
			if (sound != null)
			{
				var result = sound.release();
				fmodUtils.ERRCHECK(result);
				sound = null;
			}
		}
	}
}