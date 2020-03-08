﻿using System;
using System.Collections.Generic;
using System.IO;
using HyoutaTools.GameCube;
using HyoutaTools.Generic;
using HyoutaUtils;
using HyoutaUtils.Streams;

namespace ToGLocInject {
	internal static class CodePatches {
		public static (uint high, uint low) GenerateHighLowImmediatesFor32BitLoad(uint addr) {
			ushort high = (ushort)(addr >> 16);
			ushort low = (ushort)(addr & 0xFFFF);
			ushort highwrite = (ushort)(low >= 0x8000 ? high + 1 : high);
			return (highwrite, low);
		}

		private static List<(uint where, uint count, uint skip)> GetChunksForTexPointersFix(IRomMapper mapper) {
			List<(uint where, uint count, uint skip)> patches = new List<(uint where, uint count, uint skip)>();
			patches.Add((mapper.MapRomToRam(0x5742A8u), 35, 0x04));
			patches.Add((mapper.MapRomToRam(0x574338u), 218, 0x0C));
			patches.Add((mapper.MapRomToRam(0x585F18u), 252, 0x24));
			patches.Add((mapper.MapRomToRam(0x585F1Cu), 252, 0x24));
			return patches;
		}

		public static uint CodeSizeForFontTexPointerFix() {
			uint codeSizeRepointingSnippet = 0x30;
			uint chunksToFix = 4;
			uint codeSizePerCall = 0x18;
			uint prologue = 0x14;
			uint epilogue = 0x1C;
			return codeSizeRepointingSnippet + prologue + chunksToFix * codeSizePerCall + epilogue;
		}

		public static void ApplyFontTexPointerFix(MemoryStream ms, Dol dol, ReservedMemchunk chunk) {
			// code snippet for actually repointing
			// looks at all pointers that start with bitpattern 1110 (0xE) and readjusts them to actually point at the allocated memory
			// input:
			// - r3: address of first pointer to fix
			// - r4: amount of pointers to fix
			// - r5: bytes to increment per iteration to get to the next pointer
			// - r6: address font texture has been allocated at
			uint repointingEntryPoint = chunk.AddressRam;
			ms.Position = chunk.AddressRom;
			ms.WriteUInt32(0x7c8903a6u.ToEndian(EndianUtils.Endianness.BigEndian)); // mtctr r4
			ms.WriteUInt32(0x3d00e000u.ToEndian(EndianUtils.Endianness.BigEndian)); // lis r8, 0xE000
			ms.WriteUInt32(0x81430000u.ToEndian(EndianUtils.Endianness.BigEndian)); // lwz r10, 0 (r3)
			ms.WriteUInt32(0x55490006u.ToEndian(EndianUtils.Endianness.BigEndian)); // rlwinm r9, r10, 0, 0, 3 (f0000000)
			ms.WriteUInt32(0x7f894000u.ToEndian(EndianUtils.Endianness.BigEndian)); // cmpw cr7, r9, r8
			ms.WriteUInt32(0x40be0010u.ToEndian(EndianUtils.Endianness.BigEndian)); // bne+ cr7 --> add r3, r3, r5
			ms.WriteUInt32(0x554a013eu.ToEndian(EndianUtils.Endianness.BigEndian)); // rlwinm r10, r10, 0, 4, 31, (0fffffff)
			ms.WriteUInt32(0x7d4a3214u.ToEndian(EndianUtils.Endianness.BigEndian)); // add r10, r10, r6
			ms.WriteUInt32(0x91430000u.ToEndian(EndianUtils.Endianness.BigEndian)); // stw r10, 0 (r3)
			ms.WriteUInt32(0x7c632a14u.ToEndian(EndianUtils.Endianness.BigEndian)); // add r3, r3, r5
			ms.WriteUInt32(0x4200ffe0u.ToEndian(EndianUtils.Endianness.BigEndian)); // bdnz+ --> lwz r10, 0 (r3)
			ms.WriteUInt32(0x4e800020u.ToEndian(EndianUtils.Endianness.BigEndian)); // blr

			uint pointerFixCodeEntryPoint = dol.MapRomToRam((uint)ms.Position);
			ms.WriteUInt32(0x9421ffb0u.ToEndian(EndianUtils.Endianness.BigEndian)); // stwu sp, -0x50 (sp)
			ms.WriteUInt32(0x7c0802a6u.ToEndian(EndianUtils.Endianness.BigEndian)); // mflr r0
			ms.WriteUInt32(0x91010014u.ToEndian(EndianUtils.Endianness.BigEndian)); // stw r8,  0x0014 (sp)
			ms.WriteUInt32(0x91210018u.ToEndian(EndianUtils.Endianness.BigEndian)); // stw r9,  0x0018 (sp)
			ms.WriteUInt32(0x9141001Cu.ToEndian(EndianUtils.Endianness.BigEndian)); // stw r10, 0x001C (sp)

			foreach (var data in GetChunksForTexPointersFix(dol)) {
				var imm = GenerateHighLowImmediatesFor32BitLoad(data.where);
				ms.WriteUInt32((0x3c600000u | (imm.high & 0xffffu)).ToEndian(EndianUtils.Endianness.BigEndian)); // lis r3, imm.high
				ms.WriteUInt32((0x38630000u | (imm.low & 0xffffu)).ToEndian(EndianUtils.Endianness.BigEndian)); // addi r3, r3, imm.low
				ms.WriteUInt32((0x38800000u | (data.count & 0xffffu)).ToEndian(EndianUtils.Endianness.BigEndian)); // li r4, data.count
				ms.WriteUInt32((0x38a00000u | (data.skip & 0xffffu)).ToEndian(EndianUtils.Endianness.BigEndian)); // li r5, data.skip
				ms.WriteUInt32(0x80de0954u.ToEndian(EndianUtils.Endianness.BigEndian)); // lwz r6, 0x0954 (r30)
				uint here = dol.MapRomToRam((uint)ms.Position);
				uint diff = repointingEntryPoint - here;
				ms.WriteUInt32((0x48000001u | (diff & 0x3fffffcu)).ToEndian(EndianUtils.Endianness.BigEndian)); // bl --> repointingEntryPoint
			}

			ms.WriteUInt32(0x8141001Cu.ToEndian(EndianUtils.Endianness.BigEndian)); // lwz r10, 0x001C (sp)
			ms.WriteUInt32(0x81210018u.ToEndian(EndianUtils.Endianness.BigEndian)); // lwz r9,  0x0018 (sp)
			ms.WriteUInt32(0x81010014u.ToEndian(EndianUtils.Endianness.BigEndian)); // lwz r8,  0x0014 (sp)
			ms.WriteUInt32(0x7c0803a6u.ToEndian(EndianUtils.Endianness.BigEndian)); // mtlr r0
			ms.WriteUInt32(0x38210050u.ToEndian(EndianUtils.Endianness.BigEndian)); // addi sp, sp, 0x50
			long fixupPos = ms.Position;
			ms.WriteUInt32(0u); // placeholder for the instruction we overwrite to jump to our code
			ms.WriteUInt32(0x4e800020u.ToEndian(EndianUtils.Endianness.BigEndian)); // blr

			// and finally, actually hook code
			ms.Position = dol.MapRamToRom(0x8006be48u); // right after font texture is loaded into memory
			uint replacedInstruction = ms.PeekUInt32();
			{
				uint here = dol.MapRomToRam((uint)ms.Position);
				uint diff = pointerFixCodeEntryPoint - here;
				ms.WriteUInt32((0x48000001u | (diff & 0x3fffffcu)).ToEndian(EndianUtils.Endianness.BigEndian)); // bl --> pointerFixCodeEntryPoint
			}
			ms.Position = fixupPos;
			ms.WriteUInt32(replacedInstruction);
		}
	}
}