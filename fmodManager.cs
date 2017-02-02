using System;
using System.Text;
using System.Windows.Forms;

namespace Aural_Probe
{
	public class FmodManager
	{
		private App app;
		public FMOD.System systemFMOD  = null;
		public static FMOD.Sound sound  = null;
		public FMOD.Channel channel = null;
		public bool bFMODInitialised = false;
		public bool bAutoPlayNextSample = false;
		public int nAutoPlayRepeatsLeft = 0;
		public FMOD.CHANNEL_CALLBACK cbFMOD = null;

		public FmodManager(App app)
		{
			this.app = app;

			try
			{
				uint version = 0;
				FMOD.RESULT	 result;

				result = FMOD.Factory.System_Create(ref systemFMOD);
				fmodUtils.ERRCHECK(result);

				result = systemFMOD.getVersion(ref version);
				fmodUtils.ERRCHECK(result);
				if (version < FMOD.VERSION.number)
				{
					MessageBox.Show("Error!  You are using an old version of FMOD " + version.ToString("X") + ".  This program requires " + FMOD.VERSION.number.ToString("X") + ".");
					Application.Exit();
				}

				TrySettingOutputDevice();

				result = systemFMOD.init(32, FMOD.INITFLAGS.NORMAL, (IntPtr)null);
				fmodUtils.ERRCHECK(result);

				bFMODInitialised = true;
			}
			catch
			{
				MessageBox.Show("Error! Could not find FMOD DLL.", "FMOD DLL missing!",
					MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				Environment.Exit(-1);
			}
		}

		private void TrySettingOutputDevice()
		{
			int numDrivers = 0;
			StringBuilder driverName = new StringBuilder(256);
			FMOD.RESULT result;
			result = systemFMOD.getNumDrivers(ref numDrivers);
			fmodUtils.ERRCHECK(result);

			for (int count = 0; count < numDrivers; count++)
			{
				FMOD.GUID guid = new FMOD.GUID();
				result = systemFMOD.getDriverInfo(count, driverName, driverName.Capacity, ref guid);
				fmodUtils.ERRCHECK(result);

				if (driverName.ToString() == app.configFile.defaultSoundDevice)
				{
					result = systemFMOD.setDriver(count);
					fmodUtils.ERRCHECK(result);
				}
			}
		}

		public void StopSoundPlayback()
		{
			if (channel != null)
			{
				bool bPlaying = false;
				channel.isPlaying(ref bPlaying);
				if (bPlaying)
				{
					channel.stop();
					bAutoPlayNextSample = false;
				}
			}
			if (sound != null)
			{
				FMOD.RESULT result;
				result = sound.release();
				fmodUtils.ERRCHECK(result);
				sound = null;
			}
		}
	}
}