using HyoutaTools.FileContainer;
using System;

namespace ToGLocInject {
	public static class Program {
		public static int Main(string[] args) {
			if (args.Length == 3 && args[0] == "--prepare-raw-bnsf-from-wav") {
				VoiceInject.PrepareRawBnsfFromWav(args[1], int.Parse(args[2]));
				return 0;
			}

			// TODO:
			// - see if we have a realistic possibility of modifying the actual scenario string pointers to inject the strings where J is identical but U is different
			//   - did this for skits, would be nice for map files too but probably a lot of work
			// - dualize result message is slightly broken, see if we can fix that
			// - request reward message is super broken, suspected culprit is the printf string at 0x5327A6, dunno if this is easily fixable
			// - there might be more buffer overflows for sprintf'd strings
			// - password treasure chests don't work cause the menu doesn't allow letters to be entered
			// - text in card minigame, is in module/Mg1RR.rso and/or module/Mg2RR.rso
			// - text in music player
			// - don't inject the v0-inject files into the v2 archives
			// - mainRR.sel for riivolution in v0


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


			// to get english voices working you'll need:
			// - grab the vgmstream test.zip from https://github.com/losnoco/vgmstream/releases
			//   - I used r1050-3086-gc9dc860c but the most recent is probably good
			// - grab the bnsf encoder from https://www.itu.int/rec/T-REC-G.722.1-200505-I/en and compile Software/Fixed-200505-Rel.2.1/encode
			//   - you'll need to fix some compile errors here in VS2019 at least, rename all calls to 'round' to 'custom_round' or whatever
			// then do:
			// - run "ToGLocInject --setup-voices tempfolder" to extract the english voices from the US PS3 version and generate a bunch of batch files
			// - copy the contents of test.zip to the tempfolder
			// - copy the compiled encode.exe to the tempfolder
			// - copy this tool (ToGLocInject and its dlls) to the tempfolder
			// - run the generated convert_voices.bat in the tempfolder

			var config = new Config();

			// all of these are needed for generating the patched data; this will generate patched files for v2 of the Wii disc
			config.GamefileContainerWiiV2 = new DirectoryOnDisk(@"c:\_graces\wii-jp-v2\files\");
			config.GamefileContainerPS3JP = new DirectoryOnDisk(@"c:\_graces\ps3-jp\PS3_GAME\USRDIR\");
			config.GamefileContainerPS3US = new DirectoryOnDisk(@"c:\_graces\ps3-us\PS3_GAME\USRDIR\");
			config.MainDolWiiV2 = new FileOnDisk(@"c:\_graces\wii-jp-v2\sys\main.dol");
			config.EbootBinPS3JP = new FileOnDisk(@"c:\_graces\ps3-jp\boot.elf");
			config.EbootBinPS3US = new FileOnDisk(@"c:\_graces\ps3-us\boot.elf");
			config.PatchedFileOutputPath = @"c:\_graces\wii-en-patched";
			config.RiivolutionOutputPath = @"c:\_graces\wii-en-patched\riivolution";

			// can be set to get transfer the english voice clips as well, see comment above for how to set this up
			config.EnglishVoiceProcessingDir = @"c:\_graces\voiceworkdir\";

			// can be set to speed up multiple runs of the tool by caching decompressed files
			config.CachePath = @"c:\_graces\_cache";

			// can be set to also generate patched files for v0 of the Wii disc; note that you cannot generate files for v0 only!
			//config.GamefileContainerWiiV0 = new DirectoryOnDisk(@"c:\_graces\wii-jp-v0\files\");

			// only used as a reference for generating script dumps
			//config.GamefileContainerPS3EU = new DirectoryOnDisk(@"c:\_graces\ps3-eu\PS3_GAME\USRDIR\");
			//config.EbootBinPS3EU = new FileOnDisk(@"c:\_graces\ps3-eu\boot.elf");

			// only used for debug output
			//config.PS3CompareCsvOutputPath = @"c:\_graces\_ps3-us-jp-compare";
			//config.WiiCompareCsvOutputPath = @"c:\_graces\_wii-replaced-strings";
			//config.WiiCompareCsvWriteOnlyUnmatched = false;
			//config.WiiRawCsvOutputPath = @"c:\_graces\_wii-out-csv";
			//config.DebugFontOutputPath = @"c:\_graces\_font";
			//config.DebugTextOutputPath = @"c:\_graces\_debug";

			if (args.Length >= 1 && args[0] == "--setup-voices" && config.EnglishVoiceProcessingDir != null) {
				VoiceInject.Setup(config, config.EnglishVoiceProcessingDir);
				return 0;
			}

			FileProcessing.GenerateTranslatedFiles(config);

			return 0;
		}
	}
}
