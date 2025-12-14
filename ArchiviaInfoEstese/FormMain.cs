using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ArchiviaInfoEstese
{
    public partial class FormMain : Form
    {
        #region DICHIARAZIONI
        private int newID = 0;
        private const string MioFileInfo = "info.json";
        private const string MioFileCategorie = "categorie.json";

        private List<Info> elencoInfo = new List<Info>();
        private List<Categoria> elencoCategorie = new List<Categoria>();
        private string dataFolder;
        private string _FilePath;
        private string _CategorieFilePath;

        #region STAMPA
        private PrintDocument printDocument;
        private int printCharIndex = 0;


        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CHARRANGE
        {
            public int cpMin, cpMax;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FORMATRANGE
        {
            public IntPtr hdc;
            public IntPtr hdcTarget;
            public RECT rc;
            public RECT rcPage;
            public CHARRANGE chrg;
        }

        private const int EM_FORMATRANGE = 0x439;

        [DllImport("user32.dll")]
         private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);

        private int HundredthInchToTwips(int n) => n * 1440 / 100;

        #endregion STAMPA
        private bool IsRichTextBoxEmpty(RichTextBox rtb) => string.IsNullOrWhiteSpace(rtb.Text);

        #endregion DICHIARAZIONI

        #region Class
        public FormMain()
        {
            InitializeComponent();

            dataFolder = Path.Combine(Application.StartupPath, "Data");

            // Se la cartella non esiste, creala
            if (!Directory.Exists(dataFolder))
                Directory.CreateDirectory(dataFolder);

            // Percorsi completi dei file JSON
            _FilePath = Path.Combine(dataFolder, MioFileInfo);
            _CategorieFilePath = Path.Combine(dataFolder, MioFileCategorie);
        }


        private void FormMain_Load(object sender, EventArgs e)
        {
            CaricaInfo();
            CaricaCategorie();
            PopulateFontCombo(tscFont);
            PopulateSizeCombo(tscSize);

            richTextBox.Font = new Font("Segoe UI", 12);
            tscFont.SelectedItem = "Segoe UI";  // deve esistere già nella combo
            tscSize.SelectedItem = 12;          // idem
        }
        #endregion Class

        #region f()

        #region CATEGORIE
        private void CaricaCategorie()
        {
            if (File.Exists(_CategorieFilePath))
            {
                var json = File.ReadAllText(_CategorieFilePath);
                elencoCategorie = JsonConvert.DeserializeObject<List<Categoria>>(json) ?? new List<Categoria>();
            }

            if (!elencoCategorie.Any())
            {
                
                SalvaCategorie();
            }

            cmbCategoria.Items.Clear();
            cmbCategoria.Items.AddRange(elencoCategorie.Select(c => c.Nome).ToArray());
            cmbCategoria.SelectedIndex = -1;

            cmbCategoriaArchivio.Items.Clear();
            cmbCategoriaArchivio.Items.AddRange(elencoCategorie.Select(c => c.Nome).ToArray());
            cmbCategoriaArchivio.SelectedIndex = -1;
        }
        private void SalvaCategorie()
        {
            File.WriteAllText(_CategorieFilePath, JsonConvert.SerializeObject(elencoCategorie, Formatting.Indented));
        }
        #endregion CATEGORIE

        #region INFO
        private void CaricaInfo()
        {
            if (File.Exists(_FilePath))
            {
                var json = File.ReadAllText(_FilePath);
                elencoInfo = JsonConvert.DeserializeObject<List<Info>>(json) ?? new List<Info>();

                newID = 0;
                foreach (Info i in elencoInfo)
                {
                    if(i.ID > newID)
                    {
                        newID = i.ID;
                    }
                }


            }

        }

        private void SalvaInfo()
        {
            File.WriteAllText(_FilePath, JsonConvert.SerializeObject(elencoInfo, Formatting.Indented));
        }
        #endregion INFO

        private void AggiornaListBox(string pCategoria)
        {
            lstInfo.Items.Clear();

            foreach (var info in elencoInfo)
            {
                if (info.Categoria.Equals(pCategoria))
                    lstInfo.Items.Add(info);
            }

            // Mostra solo i titoli nella lista
            lstInfo.DisplayMember = "Titolo";
        }

        #region Stampa
        private int PrintRichText(int charFrom, int charTo, PrintPageEventArgs e)
        {
            FORMATRANGE fmtRange;
            fmtRange.chrg.cpMin = charFrom;
            fmtRange.chrg.cpMax = charTo;

            fmtRange.hdc = e.Graphics.GetHdc();
            fmtRange.hdcTarget = e.Graphics.GetHdc();

            fmtRange.rc = new RECT
            {
                Top = HundredthInchToTwips(e.MarginBounds.Top),
                Bottom = HundredthInchToTwips(e.MarginBounds.Bottom),
                Left = HundredthInchToTwips(e.MarginBounds.Left),
                Right = HundredthInchToTwips(e.MarginBounds.Right)
            };

            fmtRange.rcPage = new RECT
            {
                Top = HundredthInchToTwips(e.PageBounds.Top),
                Bottom = HundredthInchToTwips(e.PageBounds.Bottom),
                Left = HundredthInchToTwips(e.PageBounds.Left),
                Right = HundredthInchToTwips(e.PageBounds.Right)
            };

            IntPtr wParam = new IntPtr(1);
            IntPtr lParam = Marshal.AllocCoTaskMem(Marshal.SizeOf(fmtRange));
            Marshal.StructureToPtr(fmtRange, lParam, false);

            IntPtr res = SendMessage(richTextBox.Handle, EM_FORMATRANGE, wParam, lParam);

            Marshal.FreeCoTaskMem(lParam);
            e.Graphics.ReleaseHdc(fmtRange.hdc);

            return res.ToInt32();
        }

        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            printCharIndex = PrintRichText(
                printCharIndex,
                richTextBox.TextLength,
                e);

            e.HasMorePages = printCharIndex < richTextBox.TextLength;
        }

        #endregion Stampa

        private void ChangeSelectionFont(string fontName, float fontSize, FontStyle style = FontStyle.Regular)
        {
            if (richTextBox.SelectionLength == 0)
            {
                // Nessuna selezione: cambia il font corrente che verrà digitato
                richTextBox.SelectionFont = new Font(fontName, fontSize, style);
            }
            else
            {
                // Cambia il font solo della selezione
                richTextBox.SelectionFont = new Font(fontName, fontSize, style);
            }
        }
        
        private void SetFontSafe(RichTextBox rtb, string fontName, float? fontSize = null, FontStyle? fontStyle = null, bool applyToAll = false)
        {
            if (applyToAll)
                rtb.SelectAll();

            int selStart = rtb.SelectionStart;
            int selLength = rtb.SelectionLength;

            // Scorri carattere per carattere
            for (int i = 0; i < selLength; i++)
            {
                rtb.Select(selStart + i, 1);
                Font current = rtb.SelectionFont ?? rtb.Font;

                string name = fontName ?? current.FontFamily.Name;
                float size = fontSize ?? current.Size;
                FontStyle style = fontStyle ?? current.Style;

                rtb.SelectionFont = new Font(name, size, style);
            }

            rtb.Select(selStart, selLength);
        }

        private void PopulateFontCombo(ToolStripComboBox cmbFont)
        {
            cmbFont.Items.Clear();
            foreach (FontFamily f in FontFamily.Families)
                cmbFont.Items.Add(f.Name);

            cmbFont.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            cmbFont.AutoCompleteSource = AutoCompleteSource.ListItems;
        }

        private void PopulateSizeCombo(ToolStripComboBox cmbSize)
        {
            cmbSize.Items.Clear();
            int[] sizes = new int[] { 8, 9, 10, 11, 12, 14, 16, 18, 20, 24, 28, 32 };
            foreach (var s in sizes)
                cmbSize.Items.Add(s);
        }

        private void ToggleFont(FontStyle style)
        {
            Font current = richTextBox.SelectionFont ?? richTextBox.Font;
            FontStyle newStyle = current.Style ^ style;
            SetFontSafe(richTextBox, null, null, newStyle);
        }
  
        #endregion f()

        #region RITCHETBOX

        #endregion RITCHETBOX

        #region ZONA DESTRA

        private void cmbCategoriaArchivio_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbCategoriaArchivio.SelectedIndex != -1)
            {
                string nuovaCategoria = cmbCategoriaArchivio.SelectedItem.ToString();

                AggiornaListBox(nuovaCategoria);
            }
        }

        private void lstInfo_DoubleClick(object sender, EventArgs e)
        {
            if (lstInfo.SelectedItem is Info info)
            {
                txtTitolo.Text = info.Titolo;
                try
                {
                    richTextBox.Rtf = info.Testo;

                }
                catch (Exception)
                {
                    richTextBox.Text = info.Testo;
                }
                cmbCategoria.SelectedItem = info.Categoria;
                txtID.Text = info.ID.ToString();
            }
        }


        #endregion ZONA DESTRA

        #region ZONA SINISTRA

        private void lblCategoria_Click(object sender, EventArgs e)
        {
            string input = InputBox.Show("Inserisci una nuova categoria:", "Nuova Categoria");
            if (!string.IsNullOrEmpty(input))
            {
                bool esiste = elencoCategorie.Any(c => c.Nome.Equals(input, StringComparison.OrdinalIgnoreCase));

                // Controlla se la categoria esiste già
                if (!esiste)
                {
                    // Aggiungi il nuovo item
                    cmbCategoria.Items.Add(input);

                    // Riordina alfabeticamente ignorando maiuscole/minuscole
                    List<string> sorted = cmbCategoria.Items.Cast<string>()
                        .OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase)
                        .ToList();

                    cmbCategoria.Items.Clear();
                    cmbCategoria.Items.AddRange(sorted.ToArray());


                    elencoCategorie = new List<Categoria>();
                    foreach (string s in sorted)
                    {
                        Categoria c = new Categoria();
                        c.Nome = s;

                        elencoCategorie.Add(c);
                    }


                    // Salva nel JSON aggiornando la lista 'categorie'
                    SalvaCategorie();

                    // Aggiorna anche la colonna Categoria della griglia
                    CaricaCategorie();

                    cmbCategoria.SelectedIndex = cmbCategoria.Items.IndexOf(input);

                }
                else
                {
                    // Se già esiste, selezionalo semplicemente
                    cmbCategoria.SelectedItem = input;

                    MessageBox.Show("Categoria già presente.",
                                    "Attenzione",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                }

            }
        }

        #region PULSANTERIA
        private void btnCancella_Click(object sender, EventArgs e)
        {
            txtID.Text = string.Empty;
            richTextBox.Clear(); 
            txtTitolo.Text = string.Empty;
        }
        private void btnSalva_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitolo.Text) || string.IsNullOrWhiteSpace(richTextBox.Text))
            {
                MessageBox.Show("Inserisci titolo e testo.");
                return;
            }

            if (cmbCategoria.SelectedIndex == -1)
            {
                MessageBox.Show("Inserisci una categoria.");
                return;
            }

            if (string.IsNullOrEmpty(txtID.Text))
            {
                newID++;
                var info = new Info
                {
                    ID = newID,
                    Titolo = txtTitolo.Text.Trim(),
                    Categoria = cmbCategoria.SelectedItem?.ToString() ?? "Generale",
                    Testo = richTextBox.Rtf,
                };
                elencoInfo.Add(info);
                SalvaInfo();
                cmbCategoriaArchivio.SelectedIndex = cmbCategoria.SelectedIndex;
                string nuovaCategoria = cmbCategoria.SelectedItem.ToString();
                AggiornaListBox(nuovaCategoria);
                txtTitolo.Clear();
                richTextBox.Clear();
                txtID.Clear();

            }
            else
            {
                int indice = elencoInfo.FindIndex(x => x.ID == Convert.ToInt32(txtID.Text));
                if (indice >= 0)
                {
                    elencoInfo[indice].Categoria = cmbCategoria.SelectedItem?.ToString();
                    elencoInfo[indice].Titolo = txtTitolo.Text.Trim();
                    elencoInfo[indice].Testo = richTextBox.Rtf;
                    SalvaInfo();
                    cmbCategoria.SelectedIndex = cmbCategoriaArchivio.SelectedIndex;
                    string nuovaCategoria = cmbCategoria.SelectedItem.ToString();
                    AggiornaListBox(nuovaCategoria);
                    txtTitolo.Clear();
                    richTextBox.Clear();
                    txtID.Clear();

                }

            }


        }
        #endregion PULSANTERIA

        #region TOOLSTRIP
        private void tsbBold_Click(object sender, EventArgs e)
        {
            ToggleFont(FontStyle.Bold);
        }

        private void tsbItalic_Click(object sender, EventArgs e)
        {
            ToggleFont(FontStyle.Italic);
        }

        private void tsbUnderline_Click(object sender, EventArgs e)
        {
            ToggleFont(FontStyle.Underline);
        }

        private void tsbUndo_Click(object sender, EventArgs e)
        {
            if (richTextBox.CanUndo) richTextBox.Undo();
        }

        private void tsbRedo_Click(object sender, EventArgs e)
        {
            if (richTextBox.CanRedo) richTextBox.Redo();
        }

        private void tsLeft_Click(object sender, EventArgs e)
        {
            richTextBox.SelectionAlignment = HorizontalAlignment.Left;
        }

        private void tsCenter_Click(object sender, EventArgs e)
        {
            richTextBox.SelectionAlignment = HorizontalAlignment.Center;
        }

        private void tsbRight_Click(object sender, EventArgs e)
        {
            richTextBox.SelectionAlignment = HorizontalAlignment.Right;

        }

        private void tsbBullet_Click(object sender, EventArgs e)
        {
            richTextBox.SelectionBullet = !richTextBox.SelectionBullet;
        }


        private void tsbColor_Click(object sender, EventArgs e)
        {
            using (ColorDialog dlg = new ColorDialog())
                if (dlg.ShowDialog() == DialogResult.OK)
                    richTextBox.SelectionColor = dlg.Color;
        }

        private void tsbPrint_Click(object sender, EventArgs e)
        {
            printDocument = new PrintDocument();
            printDocument.PrintPage += PrintDocument_PrintPage;
            printCharIndex = 0;

            PrintPreviewDialog preview = new PrintPreviewDialog
            {
                Document = printDocument,
                Width = 800,
                Height = 600
            };
            preview.ShowDialog();
        }

        private void tscFont_SelectedIndexChanged(object sender, EventArgs e)
        {
            string fontName = tscFont.SelectedItem.ToString();
            float fontSize = richTextBox.SelectionFont?.Size ?? 12;
            FontStyle style = richTextBox.SelectionFont?.Style ?? FontStyle.Regular;
            ChangeSelectionFont(fontName, fontSize, style);
        }

        private void tscSize_SelectedIndexChanged(object sender, EventArgs e)
        {
 

            float fontSize = float.Parse(tscSize.SelectedItem.ToString());
            string fontName = richTextBox.SelectionFont?.FontFamily.Name ?? "Microsoft Sans Serif";
            FontStyle style = richTextBox.SelectionFont?.Style ?? FontStyle.Regular;
            ChangeSelectionFont(fontName, fontSize, style);
        }
        #endregion TOOLSTRIP

        private void richTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Control) return;

            if (!e.Control) return;
            switch (e.KeyCode)
            {
                case Keys.B: ToggleFont(FontStyle.Bold); break;
                case Keys.I: ToggleFont(FontStyle.Italic); break;
                case Keys.U: ToggleFont(FontStyle.Underline); break;
                case Keys.Z: if (richTextBox.CanUndo) richTextBox.Undo(); break;
                case Keys.Y: if (richTextBox.CanRedo) richTextBox.Redo(); break;
                case Keys.L: richTextBox.SelectionAlignment = HorizontalAlignment.Left; break;
                case Keys.E: richTextBox.SelectionAlignment = HorizontalAlignment.Center; break;
                case Keys.R: richTextBox.SelectionAlignment = HorizontalAlignment.Right; break;
            }
            e.SuppressKeyPress = true;
 
        }


        #endregion ZONA SINISTRA

  
    }

    public static class InputBox
    {
        public static string Show(string prompt, string title = "")
        {
            Form form = new Form()
            {
                Width = 400,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = title,
                StartPosition = FormStartPosition.CenterScreen,
                MinimizeBox = false,
                MaximizeBox = false
            };

            Label lbl = new Label() { Left = 10, Top = 10, Text = prompt, AutoSize = true };
            TextBox txt = new TextBox() { Left = 10, Top = 40, Width = 360 };
            Button btnOk = new Button() { Text = "OK", Left = 200, Width = 80, Top = 70, DialogResult = DialogResult.OK };
            Button btnCancel = new Button() { Text = "Cancel", Left = 290, Width = 80, Top = 70, DialogResult = DialogResult.Cancel };

            form.Controls.Add(lbl);
            form.Controls.Add(txt);
            form.Controls.Add(btnOk);
            form.Controls.Add(btnCancel);

            form.AcceptButton = btnOk;
            form.CancelButton = btnCancel;

            DialogResult result = form.ShowDialog();
            if (result == DialogResult.OK)
                if (!string.IsNullOrEmpty(txt.Text.Trim()))
                    return txt.Text.Trim();
                else
                    return null;
            else
                return null;
        }
    }
}


/*
 * aggiunte
 * =========
 * menuStrip = new MenuStrip();
            var file = new ToolStripMenuItem("File");
            file.DropDownItems.Add("Apri RTF", null, MnuOpenRtf_Click);
            file.DropDownItems.Add("Salva RTF", null, MnuSaveRtf_Click);
            file.DropDownItems.Add(new ToolStripSeparator());
            file.DropDownItems.Add("Import HTML", null, MnuImportHtml_Click);
            file.DropDownItems.Add("Export HTML", null, MnuExportHtml_Click);
            file.DropDownItems.Add(new ToolStripSeparator());
            file.DropDownItems.Add("Stampa", null, MnuPrint_Click);
            file.DropDownItems.Add("Esci", null, (s, e) => Close());
            menuStrip.Items.Add(file);

      // ================= FILE =================

        private void MnuOpenRtf_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "RTF (*.rtf)|*.rtf";
                if (dlg.ShowDialog() == DialogResult.OK)
                    richTextBox.LoadFile(dlg.FileName);
            }
        }

        private void MnuSaveRtf_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Filter = "RTF (*.rtf)|*.rtf";
                if (dlg.ShowDialog() == DialogResult.OK)
                    richTextBox.SaveFile(dlg.FileName);
            }
        }

        // ================= HTML (BASE) =================

        private void MnuExportHtml_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Filter = "HTML (*.html)|*.html";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    string html = "<html><body><pre>" +
                        System.Net.WebUtility.HtmlEncode(richTextBox.Text) +
                        "</pre></body></html>";
                    File.WriteAllText(dlg.FileName, html, Encoding.UTF8);
                }
            }
        }

        private void MnuImportHtml_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "HTML (*.html)|*.html";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    string html = File.ReadAllText(dlg.FileName);
                    richTextBox.Text = System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
                }
            }
        }

        // ================= PRINT =================

        private void MnuPrint_Click(object sender, EventArgs e)
        {
        printDocument = new PrintDocument();
            printDocument.PrintPage += PrintDocument_PrintPage;
            printCharIndex = 0;

            PrintPreviewDialog preview = new PrintPreviewDialog
            {
                Document = printDocument,
                Width = 800,
                Height = 600
            };
            preview.ShowDialog();
        }

        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            printCharIndex = e.Graphics.MeasureString(
                richTextBox.Text.Substring(printCharIndex),
                richTextBox.Font,
                e.MarginBounds.Size,
                StringFormat.GenericTypographic,
                out int chars,
                out int lines).ToString().Length;

            e.Graphics.DrawString(
                richTextBox.Text.Substring(0),
                richTextBox.Font,
                Brushes.Black,
                e.MarginBounds,
                StringFormat.GenericTypographic);

            e.HasMorePages = false;
        }
 * 
*/


