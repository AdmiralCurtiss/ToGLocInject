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

		public static Dictionary<string, MappingData> GetFileMappings(FileFetcher _fc) {
			var files = new Dictionary<string, MappingData>();
			GetFileMappingsMap0(_fc, files);
			GetFileMappingsMap1(_fc, files);
			GetFileMappingsRoot(_fc, files);
			files.Add("boot.elf", new MappingData(c: true));
			GetFileMappingsWiiV0(files);
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
	}
}
