using System.Collections.Generic;

namespace ToGLocInject {
	internal static partial class Mappings {
		private static M GenerateCommonU(int idx) {
			int o = 721 - idx;
			return new M().A(60, 36).A(81, 11).A(721 - o, 716 - o).A(795 - o, 793 - o).A(807 - o, new List<int>() { 766 - o, 766 - o, 766 - o, 766 - o, 766 - o, 766 - o, 766 - o }).A(817 - o, 766 - o).A(819 - o, 805 - o).A(823 - o, 822 - o).A(824 - o, 822 - o).A(839 - o, 815 - o, 815 - o).A(844 - o, new List<int>() { 843 - o, 843 - o, 843 - o, 843 - o, 843 - o, 843 - o }).A(845 - o, 844 - o).A(846 - o, 845 - o);
		}

		private static M GenerateCommonJ(int idx) {
			int o = 721 - idx + 2;
			return new M().A(810 - o, 751 - o).A(814 - o, 771 - o).A(853 - o, 818 - o, 826 - o, 827 - o).A(855 - o, 830 - o);
		}

		public static M GenerateDefault() {
			return new M(new List<E> { new E(60, 36), new E(81, 11) });
		}

		public static Dictionary<string, MappingData> GetFileMappings(FileFetcher _fc, bool patchVoices) {
			var files = new Dictionary<string, MappingData>();
			files.Add(@"rootR.cpk/str/ja/CharName.bin", new MappingData(c: true));
			GetFileMappingsMap0(_fc, files);
			GetFileMappingsMap1(_fc, files);
			GetFileMappingsRoot(_fc, files);
			files.Add("boot.elf", new MappingData(c: true));
			GetFileMappingsWiiV0(files);
			GetFileMappingsAreaNameTextures(files);
			if (patchVoices) {
				GetFileMappingsVoices(files);
			}
			return files;
		}

		private static void GetFileMappingsWiiV0(Dictionary<string, MappingData> files) {
			// files that were changed between Wii V0 and Wii V2 but wouldn't be patched otherwise
			files.Add(@"map0R.cpk/mapfile_basiR.cpk/map/sce/R/basi_d13.so", new MappingData(c: true, skipTextMapping: true, replaceInWiiV0: true));
			files.Add(@"map0R.cpk/mapfile_kotR.cpk/map/sce/R/kot2_d17.so", new MappingData(c: true, skipTextMapping: true, replaceInWiiV0: true));
			files.Add(@"map0R.cpk/mapfile_rockR.cpk/map/sce/R/rock_d01.so", new MappingData(c: true, skipTextMapping: true, replaceInWiiV0: true));
			files.Add(@"map0R.cpk/mapfile_rockR.cpk/map/sce/R/rock_d02.so", new MappingData(c: true, skipTextMapping: true, replaceInWiiV0: true));
			files.Add(@"map1R.cpk/mapfile_systemR.cpk/map/sce/R/mg02_e01.so", new MappingData(c: true, skipTextMapping: true, replaceInWiiV0: true));
			files.Add(@"rootR.cpk/module/mainRR.sel", new MappingData(c: true, skipTextMapping: true, replaceInWiiV0: true));
		}

		private static void GetFileMappingsAreaNameTextures(Dictionary<string, MappingData> files) {
			// files that contain the fancy names displayed when first entering an area
			files.Add(@"map0R.cpk/mapfile_basiR.cpk/map/chr/R/e731_090.ani", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"map0R.cpk/mapfile_briaR.cpk/map/chr/R/bria_d01.ani", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"map0R.cpk/mapfile_bridR.cpk/map/chr/R/brid_d10.ani", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"map0R.cpk/mapfile_caveR.cpk/map/chr/R/e210_020.ani", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"map0R.cpk/mapfile_foreR.cpk/map/chr/R/fore_d01.ani", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"map0R.cpk/mapfile_icebR.cpk/map/chr/R/iceb_d01.ani", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"map0R.cpk/mapfile_ironR.cpk/map/chr/R/e525_030.ani", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"map0R.cpk/mapfile_koneR.cpk/map/chr/R/kone_d02.ani", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"map0R.cpk/mapfile_kotR.cpk/map/chr/R/e526_060.ani", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"map0R.cpk/mapfile_kotR.cpk/map/chr/R/e833_010.ani", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"map0R.cpk/mapfile_lasR.cpk/map/chr/R/e835_010.ani", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"map0R.cpk/mapfile_montR.cpk/map/chr/R/e101_010.ani", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"map0R.cpk/mapfile_sandR.cpk/map/chr/R/sand_d01.ani", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"map0R.cpk/mapfile_sneeR.cpk/map/chr/R/e524_040.ani", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"map0R.cpk/mapfile_snowR.cpk/map/chr/R/snow_d01.ani", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"map0R.cpk/mapfile_stdaR.cpk/map/chr/R/e420_080.ani", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"map0R.cpk/mapfile_varoR.cpk/map/chr/R/varo_d01.ani", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"map0R.cpk/mapfile_wincR.cpk/map/chr/R/winc_d01.ani", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"map0R.cpk/mapfile_zoneR.cpk/map/chr/R/zone_d02.ani", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"map1R.cpk/mapfile_anmaR.cpk/map/chr/R/e524_010.ani", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"map1R.cpk/mapfile_beraR.cpk/map/chr/R/e523_030.ani", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"map1R.cpk/mapfile_fendR.cpk/map/chr/R/e523_090.ani", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"map1R.cpk/mapfile_lakeR.cpk/map/chr/R/e313_010.ani", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"map1R.cpk/mapfile_lanR.cpk/map/chr/R/lan1_t01.ani", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"map1R.cpk/mapfile_nekoR.cpk/map/chr/R/neko_t01.ani", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"map1R.cpk/mapfile_olleR.cpk/map/chr/R/olle_t01.ani", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"map1R.cpk/mapfile_otheR.cpk/map/chr/R/e730_030.ani", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"map1R.cpk/mapfile_riotR.cpk/map/chr/R/e522_030.ani", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"map1R.cpk/mapfile_sablR.cpk/map/chr/R/e419_020.ani", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"map1R.cpk/mapfile_strtR.cpk/map/chr/R/e420_010.ani", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"map1R.cpk/mapfile_supaR.cpk/map/chr/R/supa_r01.ani", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"map1R.cpk/mapfile_winR.cpk/map/chr/R/e104_010.ani", new MappingData(c: true, skipTextMapping: true));
		}

		private static void GetFileMappingsVoices(Dictionary<string, MappingData> files) {
			files.Add(@"rootR.cpk/snd/strpck/VOBTL.nub", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"rootR.cpk/snd/strpck/VOCHT.nub", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"rootR.cpk/snd/strpck/VOSCE01.nub", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"rootR.cpk/snd/strpck/VOSCE02.nub", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"rootR.cpk/snd/strpck/VOSCE03.nub", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"rootR.cpk/snd/strpck/VOSCE04.nub", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"rootR.cpk/snd/strpck/VOSCE05.nub", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"rootR.cpk/snd/strpck/VOSCE06.nub", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"rootR.cpk/snd/strpck/VOSCE07.nub", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"rootR.cpk/snd/strpck/VOSCE08.nub", new MappingData(c: true, skipTextMapping: true));
			files.Add(@"rootR.cpk/snd/strpck/VOSCE16.nub", new MappingData(c: true, skipTextMapping: true));

			foreach (var cvi in VoiceInject.ContainedVoices) {
				for (int i = cvi.StartNumber; i <= cvi.EndNumber; ++i) {
					string path = string.Format(cvi.BaseName, i);
					files.Add(@"rootR.cpk/" + path, new MappingData(c: true, skipTextMapping: true, voiceInject: cvi));
				}
			}
		}
	}
}
