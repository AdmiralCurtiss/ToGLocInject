using HyoutaTools.Tales.Graces;
using HyoutaTools.Tales.Graces.SCS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToGLocInject {
	public class CharNameMapping {
		private Dictionary<int, List<int>> Ps3ToWiiMap;

		public CharNameMapping(Dictionary<int, List<int>> ps3ToWiiMap) {
			Ps3ToWiiMap = ps3ToWiiMap;
		}

		public int MapPs3ToWii(int ps3id) {
			List<int> tmp;
			if (Ps3ToWiiMap.TryGetValue(ps3id, out tmp)) {
				if (tmp.Count > 1) {
					Console.WriteLine("Warning: PS3 id " + ps3id + "matches to multiple Wii ids");
				}
				return tmp[0];
			}
			return ps3id + 900000; // this entry doesn't exist in Wii, mark it as a huge key so we can detect it in the output and make sure it's not actually matched
		}

		private class CharNameData {
			public int Section;
			public int Id;
			public bool Used = false;
		}

		private static int TakeNext(int preferredSection, int preferredId, List<CharNameData> datas) {
			foreach (CharNameData d in datas) {
				if (!d.Used && d.Id == preferredId) {
					d.Used = true;
					return d.Id;
				}
			}

			foreach (CharNameData d in datas) {
				if (!d.Used && d.Section == preferredSection) {
					d.Used = true;
					return d.Id;
				}
			}

			foreach (CharNameData d in datas) {
				if (!d.Used) {
					d.Used = true;
					return d.Id;
				}
			}

			foreach (CharNameData d in datas) {
				d.Used = false;
			}

			return TakeNext(preferredSection, preferredId, datas);
		}

		public static CharNameMapping BuildPs3ToWiiCharNameIdMapping(CharNameBin namesW, CharNameBin namesJ) {
			// TODO: cross-check J with U against J-match-but-U-different, might be important
			Dictionary<string, List<CharNameData>> dict = new Dictionary<string, List<CharNameData>>();
			for (int sec = 0; sec < namesJ.Sections.Count; ++sec) {
				for (int n = namesJ.Sections[sec].NumberStart; n < namesJ.Sections[sec].NumberStart + namesJ.Sections[sec].NumberCount; ++n) {
					var j = namesJ.IdToScsMappings[n];
					if (j.reg != 0) {
						string name = namesJ.Scs.Entries[(int)(j.reg - 1)];
						if (j.alt != 0 && n != 1020 /* special case for entry that got an alt string added in PS3 */) {
							string altname = namesJ.Scs.Entries[(int)(j.alt - 1)]; ;
							name = name + "_____" + altname;
						}
						var cnd = new CharNameData() { Section = sec, Id = n };
						if (!dict.ContainsKey(name)) {
							dict.Add(name, new List<CharNameData>() { cnd });
						} else {
							dict[name].Add(cnd);
						}
					}
				}
			}

			Dictionary<int, List<int>> ps3ToWiiMap = new Dictionary<int, List<int>>();
			HashSet<int> wiiNotYetMapped = new HashSet<int>();
			for (int sec = 0; sec < namesW.Sections.Count; ++sec) {
				for (int n = namesW.Sections[sec].NumberStart; n < namesW.Sections[sec].NumberStart + namesW.Sections[sec].NumberCount; ++n) {
					wiiNotYetMapped.Add(n);
				}
			}

			foreach (int n in new List<int>(wiiNotYetMapped)) {
				var w = namesW.IdToScsMappings[n];
				if (w.reg != 0) {
					string name = namesW.Scs.Entries[(int)(w.reg - 1)];
					if (w.alt != 0) {
						string altname = namesW.Scs.Entries[(int)(w.alt - 1)]; ;
						name = name + "_____" + altname;
					}
					if (dict.ContainsKey(name)) {
						var cnd = dict[name].Where(x => x.Id == n).FirstOrDefault();
						if (cnd != null) {
							cnd.Used = true;
							ps3ToWiiMap.Add(cnd.Id, new List<int>() { n });
							wiiNotYetMapped.Remove(n);
						}
					}
				}
			}

			for (int sec = 0; sec < namesW.Sections.Count; ++sec) {
				for (int n = namesW.Sections[sec].NumberStart; n < namesW.Sections[sec].NumberStart + namesW.Sections[sec].NumberCount; ++n) {
					if (!wiiNotYetMapped.Contains(n)) {
						continue;
					}

					var w = namesW.IdToScsMappings[n];
					if (w.reg != 0) {
						string name = namesW.Scs.Entries[(int)(w.reg - 1)];
						if (w.alt != 0) {
							string altname = namesW.Scs.Entries[(int)(w.alt - 1)]; ;
							name = name + "_____" + altname;
						}
						if (dict.ContainsKey(name)) {
							int ps3id = TakeNext(sec, n, dict[name]);
							Console.WriteLine("mapping ps3 " + ps3id + " to wii " + n);
							if (!ps3ToWiiMap.ContainsKey(ps3id)) {
								ps3ToWiiMap.Add(ps3id, new List<int>() { n });
							} else {
								ps3ToWiiMap[ps3id].Add(n);
							}
						} else {
							Console.WriteLine("didn't find " + name + " in ps3 ver");
						}
					}
				}
			}

			return new CharNameMapping(ps3ToWiiMap);
		}

		public List<(int index, string entry)> MapAllPs3ToWii(List<(int index, string entry)> j) {
			if (j == null) {
				return j;
			}
			List<(int index, string entry)> result = new List<(int index, string entry)>(j);

			int total = 0;
			for (int i = 0; i < j.Count; ++i) {
				if (j[i].entry != null) {
					var r = MapPs3ToWii(j[i].entry);
					if (r.replacements != 0) {
						result[i] = (j[i].index, r.replacedstring);
						total += r.replacements;
					}
				}
			}
			return result;
		}

		public (int replacements, string replacedstring) MapPs3ToWii(string input) {
			if (!input.Contains('\u0004')) {
				return (0, input);
			}

			string s = input;
			int replacementCounter = 0;
			StringBuilder sb = new StringBuilder();
			while (true) {
				int idx = s.IndexOf('\u0004');
				if (idx == -1) {
					sb.Append(s);
					break;
				}

				string s0 = s.Substring(0, idx);
				string s1 = s.Substring(idx + 1);
				sb.Append(s0);

				if (s1[0] == '(') {
					int braceclose = s1.IndexOf(')');
					if (braceclose == -1) {
						Console.WriteLine("WARNING: Found charname control code without closing brace.");
						s = s1;
					} else {
						string inbrace = s1.Substring(1, braceclose - 1);
						string postbrace = s1.Substring(braceclose + 1);
						int decodednumber = SCS.DecodeNumber(inbrace);
						int mappednumber = MapPs3ToWii(decodednumber);
						string reencodednumber = SCS.EncodeNumber(mappednumber);
						if (inbrace != reencodednumber) {
							++replacementCounter;
						}
						sb.Append("\u0004(").Append(reencodednumber).Append(")");
						s = postbrace;
					}
				} else {
					Console.WriteLine("WARNING: Found charname control code without ID.");
					s = s1;
				}
			}
			return (replacementCounter, sb.ToString());
		}
	}
}
