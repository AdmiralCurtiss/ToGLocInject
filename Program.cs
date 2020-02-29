using HyoutaTools.FileContainer;
using System;

namespace ToGLocInject {
	public static class Program {
		public static int Main() {
			// TODO:
			// - see if we have a realistic possibility of modifying the actual scenario string pointers to inject the strings where J is identical but U is different
			//   - did this for skits, would be nice for map files too but probably a lot of work
			// - dualize result message is slightly broken, see if we can fix that
			// - request reward message is super broken, suspected culprit is the printf string at 0x5327A6, dunno if this is easily fixable
			// - there might be more buffer overflows for sprintf'd strings
			// - password treasure chests don't work cause the menu doesn't allow letters to be entered
			// - text in card minigame, is in module/Mg1RR.rso and/or module/Mg2RR.rso
			// - text in music player


			// note: if we need more space in main.dol, there's 1800 unused bytes at 0x8062B770 now (cause we remapped them elsewhere)
			// but to use them we need to change the dol section mappings


			// note: all files must be decrypted. that means:
			// - for the wii games:
			//   - dump with CleanRip
			//   - rightclick the game in Dolphin's game list -> Properties
			//   - switch to the Filesystem tab, rightclick on Partition 1 -> Extract Entire Partition...
			//   - point the GamefileContainer at the generated 'files' directory
			//   - point the MainDol at the 'main.dol' file in the 'sys' directory
			// - for the PS3 games:
			//   - has only been tested with the disc releases, dunno if the PSN version works
			//   - dump with whatever method that decrypts the disc on dumping, multiMAN works
			//   - point the GamefileContainer at the 'PS3_GAME/USRDIR'
			//   - the EBOOT.BIN has an additional layer of encryption, decrypt that too
			//     - easiest way to do this is with revision bc27f5f75c159c8844f26c663fc68f1282d102d8 of rpcs3 (build 0.0.5-7715)
			//     - just start emulating the game, then search in the emulator's 'data' directory for a 'boot.elf' in the subdirectory of the game's serial
			//   - point the EbootBin at the decrypted eboot

			var config = new Config();

			// all of these are needed for generating the patched data; this will generate patched files for v2 of the Wii disc
			config.GamefileContainerWiiV2 = new DirectoryOnDisk(@"c:\_graces\wii-jp-v2\files\");
			config.GamefileContainerPS3JP = new DirectoryOnDisk(@"c:\_graces\ps3-jp\PS3_GAME\USRDIR\");
			config.GamefileContainerPS3US = new DirectoryOnDisk(@"c:\_graces\ps3-us\PS3_GAME\USRDIR\");
			config.MainDolWiiV2 = new FileOnDisk(@"c:\_graces\wii-jp-v2\sys\main.dol");
			config.EbootBinPS3JP = new FileOnDisk(@"c:\_graces\ps3-jp\boot.elf");
			config.EbootBinPS3US = new FileOnDisk(@"c:\_graces\ps3-us\boot.elf");
			config.PatchedFileOutputPath = @"c:\_graces\wii-en-patched";

			// can be set to speed up multiple runs of the tool by caching decompressed files
			//config.CachePath = @"c:\_graces\_cache";

			// can be set to also generate patched files for v0 of the Wii disc; note that you cannot generate files for v0 only!
			//config.GamefileContainerWiiV0 = new DirectoryOnDisk(@"c:\_graces\wii-jp-v0\files\");

			// only used as a reference for generating script dumps
			//config.GamefileContainerPS3EU = new DirectoryOnDisk(@"c:\_graces\ps3-eu\PS3_GAME\USRDIR\");
			//config.EbootBinPS3EU = new FileOnDisk(@"c:\_graces\ps3-eu\boot.elf");

			// only used for debug output
			//config.PS3CompareCsvOutputPath = @"c:\_graces\_ps3-us-jp-compare";
			//config.WiiCompareCsvOutputPath = @"c:\_graces\_wii-replaced-strings";
			//config.WiiCompareCsvWriteOnlyUnmatched = false;
			//config.DebugFontOutputPath = @"c:\_graces\_font";
			//config.DebugTextOutputPath = @"c:\_graces\_debug";

			FileProcessing.GenerateTranslatedFiles(config);

			return 0;
		}
	}
}
