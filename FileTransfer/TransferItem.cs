using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace OeipCommon.FileTransfer
{
    public class TransferItem : IEquatable<TransferItem>
    {
        public string RelativePath { get; set; } = string.Empty;

        public string Hash { get; set; } = string.Empty;

        public long Size { get; set; } = 0;

        public TransferItem()
        {
        }

        public static bool TryParse(string line, out TransferItem item)
        {
            item = new TransferItem();
            if (string.IsNullOrEmpty(line))
                return false;
            var cleanLine = line.RemoveLineSeparatorsAndNulls();
            var itemElements = cleanLine.Split(':');
            if (itemElements.Length != 3)
                return false;
            item.RelativePath = itemElements[0].Replace('/', '\\');
            item.RelativePath = item.RelativePath.Replace(Path.DirectorySeparatorChar, '/').TrimStart('/');
            if (itemElements[1].Length != 32)
                return false;
            item.Hash = itemElements[1];
            long parsedSize = 0;
            if (!long.TryParse(itemElements[2], out parsedSize))
                return false;
            if (parsedSize < 0)
                return false;
            item.Size = parsedSize;
            return true;
        }

        public override string ToString()
        {
            return $"{RelativePath}:{Hash}:{Size}";
        }

        public bool Equals(TransferItem transfer)
        {
            if (transfer == null)
                return false;
            return this.RelativePath == transfer.RelativePath &&
                string.Equals(Hash, transfer.Hash, StringComparison.InvariantCultureIgnoreCase) &&
                Size == transfer.Size;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TransferItem);
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
    }
}
