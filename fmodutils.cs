using System;
using System.Windows.Forms;

namespace Aural_Probe
{
	class fmodUtils
	{
		public static void ERRCHECK(FMOD.RESULT result)
		{
			if (result != FMOD.RESULT.OK && result != FMOD.RESULT.ERR_INVALID_PARAM && result != FMOD.RESULT.ERR_FORMAT && result != FMOD.RESULT.ERR_FILE_BAD && result != FMOD.RESULT.ERR_TOOMANYCHANNELS)
			{
				MessageBox.Show("FMOD error! " + result + " - " + FMOD.Error.String(result));
				Environment.Exit(-1);
			}
		}
	}
}