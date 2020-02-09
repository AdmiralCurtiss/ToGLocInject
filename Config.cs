﻿using HyoutaPluginBase.FileContainer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyoutaTools.Tales.Graces.TranslationPort {
	public class Config {
		public IContainer GamefileContainerWiiV2;
		public IContainer GamefileContainerPS3JP;
		public IContainer GamefileContainerPS3US;
		public IFile MainDolWiiV2;
		public IFile EbootBinPS3JP;
		public IFile EbootBinPS3US;
		public string PatchedFileOutputPath;

		public string CachePath;
		public IContainer GamefileContainerWiiV0;
		public IContainer GamefileContainerPS3EU;
		public IFile EbootBinPS3EU;

		public string PS3CompareCsvOutputPath;
		public string WiiCompareCsvOutputPath;
		public bool WiiCompareCsvWriteOnlyUnmatched;
		public string DebugFontOutputPath;
		public string DebugTextOutputPath;
	}
}
