//------------------------------------------------------------------------------
// <copyright file="DemCodec.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <createdby>bretm</createdby><creationdate>2005-11-29</creationdate>
//------------------------------------------------------------------------------

using System;
using System.IO;
using Microsoft.MapPoint.Data.VirtualEarthTileDataStore.ElevationData.Compression;

namespace Microsoft.Maps.ElevationAdjustmentService.HDPhoto
{
	/// <summary>
	/// Provides serialization and deserialization of digital elevation maps.
	/// Based on $\VirtualEarth\Main\Metropolis\Applications\DEMPipeline\DemCompress\DemCodec\DemCodec.cs
	/// </summary>
	internal sealed class DemCodec
	{
		#region Fields

		const byte DemStyleWDP = 4;
		#endregion

		#region Internal Members

		/// <summary>
        /// Decompress a stream into a Dem tile.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
		internal static DemTile Decompress(Stream input)
		{
			// validate input arguments
			if (input == null || !input.CanRead)
				return null;

			try
			{
				if (input.ReadByte() == DemStyleWDP)
				{
					return Decode(input);
				}
			}
			catch (IOException)
			{}

			return null;
		}
		#endregion

		#region Private Members

		private static DemTile Decode(Stream input)
		{
			// read meta info 
			var b = new byte[4 * sizeof(int)];
			input.Read(b, 0, b.Length);

			var width = BitConverter.ToInt32(b, 0);
			if (width < 1 || width > 1000)
				return null;

			var height = BitConverter.ToInt32(b, sizeof(int));
			if (height < 1 || height > 1000)
				return null;

			var avg = BitConverter.ToInt32(b, 2 * sizeof(int));
			if (avg < short.MinValue || avg > short.MaxValue)
				return null;

			var length = BitConverter.ToInt32(b, 3 * sizeof(int));
			if (length < 0 || length > 200000)
				return null;

			// read compressed data
			var data = new byte[length];
			var offset = 0;
			while (offset < length)
			{
				var actual = input.Read(data, offset, length - offset);
				if (actual <= 0 || actual > length - offset)
				{
					return null;
				}
				offset += actual;
			}

			var pixels = new HDPhotoDecoder().Decode(data);

			if (pixels == null)
			{
				return null;
			}

            try
            {
                // read error-corrections
                var r = new BitReader(input);
                for (var row = 0; row < height; row++)
                    for (var col = 0; col < width; col++)
                        if ((row & 0xF) == 0 || (col & 0xF) == 0)
                        {
                            // offset = row * width + col;
                            var err = ReadUnaryCorrection(r);
                            pixels[row, col] += err;
                        }
            }
            catch (EndOfStreamException)
            {
                return null;
            }

			// create tile
			for (var row = 0; row < height; row++)
				for (var col = 0; col < width; col++)
					pixels[row, col] += (short)avg;
			var tile = new DemTile(pixels);

			return tile;
		}

        private static short ReadUnaryCorrection(BitReader r)
        {
            if (!r.ReadBit())
                return 0;

            short err = 1;
            while (true)
            {
                if (!r.ReadBit())
                    break;
                err++;
            }

            if (r.ReadBit())
                err = (short)-err;

            return err;
        }
		#endregion
	}
}
