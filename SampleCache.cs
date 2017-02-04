using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Aural_Probe
{
	// if we want to make this more useful,
	// we should probably save tag/category info here as well.

	public class SampleCache
	{
		public class CachedSample
		{
			public string Path;
			public int ColorIndex;
		}

		// In the future, save the cache size at the top instead
		// for the sake of progress bars and alike
		// (not that I think that loading even 'bout half a billion samples
		// is going to take that much time).
		public int CacheSize => this.Cache.Count;
		public List<CachedSample> Cache;

		public SampleCache()
		{
			this.Cache = new List<CachedSample>();
		}

		public void Add(Sample sample)
		{
			this.Cache.Add(new CachedSample {
				Path = sample.Path,
				ColorIndex = sample.ColorIndex
			});
		}

		public void Save(string filename)
		{
			File.WriteAllText(filename, JsonConvert.SerializeObject(this));
		}

		public static SampleCache Load(string filename)
		{
			return JsonConvert.DeserializeObject<SampleCache>(File.ReadAllText(filename));
		}
	}
}