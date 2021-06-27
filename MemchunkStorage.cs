using HyoutaUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToGLocInject {
	internal class MemchunkStorage {
		private List<MemChunk> Memchunks;

		public MemchunkStorage(List<MemChunk> memchunks) {
			Memchunks = memchunks;
		}

		internal IEnumerable<MemChunk> GetChunks() {
			return Memchunks;
		}

		internal MemChunk FindInternalAlignedBlock(uint size, uint alignment) {
			return Memchunks.FirstOrDefault(x => x.FreeBytes >= size && x.IsInternal && (x.Mapper.MapRomToRam(x.Address) % alignment) == 0);
		}

		internal MemChunk FindBlock(uint bytecount, bool forceInternal) {
			return Memchunks.FirstOrDefault(x => x.FreeBytes >= bytecount && (!forceInternal || x.IsInternal));
		}

		internal void TakeBytes(MemChunk chunk, uint size) {
			if (chunk.FreeBytes < size) {
				throw new Exception("took too many bytes");
			}

			chunk.Address += size;
			chunk.FreeBytes -= size;
		}
	}
}
