using System;
using System.IO;
using System.Text;
using HyoutaTools.Tales.CPK;
using HyoutaUtils;

namespace ToGLocInject {
	internal class FileInjectorV0V2 {
		private FileInjector Injector0;
		private FileInjector Injector2;

		public FileInjectorV0V2(CpkContainer cpkv2, CpkContainer cpkv0, string outpathv2, string outpathv0, long injectionOffset) {
			Injector0 = cpkv0 == null ? null : new FileInjector(cpkv0, outpathv0, injectionOffset);
			Injector2 = cpkv2 == null ? null : new FileInjector(cpkv2, outpathv2, injectionOffset);
		}

		public void InjectFile(Stream generatedFile, string relativePath) {
			if (Injector0 != null) {
				Injector0.InjectFile(generatedFile, relativePath);
			}
			if (Injector2 != null) {
				Injector2.InjectFile(generatedFile, relativePath);
			}
		}

		public void InjectFileSubcpk(Stream generatedFile, string subcpkPath, string relativePath) {
			if (Injector0 != null) {
				Injector0.InjectFileSubcpk(generatedFile, subcpkPath, relativePath);
			}
			if (Injector2 != null) {
				Injector2.InjectFileSubcpk(generatedFile, subcpkPath, relativePath);
			}
		}

		public void GenerateRiivolutionData(StringBuilder xml, string outputPath, string fileOnDisc, bool isV2) {
			if (!isV2 && Injector0 != null) {
				Injector0.GenerateRiivolutionData(xml, outputPath, fileOnDisc, false);
			}
			if (isV2 && Injector2 != null) {
				Injector2.GenerateRiivolutionData(xml, outputPath, fileOnDisc, true);
			}
		}

		public void Close() {
			if (Injector0 != null) {
				Injector0.Close();
			}
			if (Injector2 != null) {
				Injector2.Close();
			}
		}
	}
}
