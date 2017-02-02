using System.Windows.Forms;

namespace Aural_Probe
{
	public class ColorUtils
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
}