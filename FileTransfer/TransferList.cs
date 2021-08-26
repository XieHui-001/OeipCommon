using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OeipCommon.FileTransfer
{
    public class TransferList
    {
        private List<TransferItem> transferItems = new List<TransferItem>();

        public IReadOnlyList<TransferItem> TransferItems
        {
            get
            {
                return transferItems;
            }
        }

        public void LoadTransferItems(string path)
        {
            try { 
            using (var fileStream = File.OpenRead(path))
            {
                LoadTransferItems(fileStream);
            }
            }
            catch { }
        }

        public void LoadTransferItems(Stream stream)
        {
            var rawManifest = new List<string>();
            using (var sr = new StreamReader(stream))
            {
                string line = string.Empty;
                while ((line = sr.ReadLine()) != null)
                {
                    rawManifest.Add(line);
                }
            }
            transferItems.Clear();
            foreach (var rawEntry in rawManifest)
            {
                TransferItem newEntry = null;
                if (TransferItem.TryParse(rawEntry, out newEntry))
                {
                    transferItems.Add(newEntry);
                }
            }
        }
    }
}
