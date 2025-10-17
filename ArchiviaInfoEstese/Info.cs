using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchiviaInfoEstese
{
    public class Info
    {
        public int ID { get; set; }
        public string Titolo { get; set; }
        public string Categoria { get; set; }
        public string Testo { get; set; }

        public override string ToString()
        {
            return Titolo;
        }
    }
}
