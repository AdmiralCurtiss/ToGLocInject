using System;

namespace ToGLocInject {
	internal class MainDolString {
		public uint RomPointerPosition;
		public uint RomTextPosition;
		public string Text;
		public uint StringByteCount; // should include the null terminator at the end if it's a nullterminated string (pretty sure all of them are)

		public MainDolString(uint romPointerPosition, uint romTextPosition, string text, uint stringByteCount) {
			RomPointerPosition = romPointerPosition;
			RomTextPosition = romTextPosition;
			Text = text;
			StringByteCount = stringByteCount;
		}

		public override string ToString() {
			return RomPointerPosition.ToString("x8") + " -> " + RomTextPosition.ToString("x8") + " -> " + Text;
		}
	}
}
