using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace ArchiviaInfoEstese
{
    public partial class FormMain : Form
    {
        private int newID = 0;
        private const string MioFileInfo = "info.json";
        private const string MioFileCategorie = "categorie.json";

        private List<Info> elencoInfo = new List<Info>();
        private List<Categoria> elencoCategorie = new List<Categoria>();
        private string dataFolder;
        private string _FilePath;
        private string _CategorieFilePath;

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
        }

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


        private void btnSalva_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitolo.Text) || string.IsNullOrWhiteSpace(txtTesto.Text))
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
                    Testo = txtTesto.Text,
                };
                elencoInfo.Add(info);
                SalvaInfo();
                cmbCategoriaArchivio.SelectedIndex = cmbCategoria.SelectedIndex;
                string nuovaCategoria = cmbCategoria.SelectedItem.ToString();
                AggiornaListBox(nuovaCategoria);
                txtTitolo.Clear();
                txtTesto.Clear();
                txtID.Clear();

            }
            else
            {
                int indice = elencoInfo.FindIndex(x => x.ID == Convert.ToInt32(txtID.Text));
                if (indice >= 0)
                {
                    elencoInfo[indice].Categoria = cmbCategoria.SelectedItem?.ToString();
                    elencoInfo[indice].Titolo = txtTitolo.Text.Trim();
                    elencoInfo[indice].Testo = txtTesto.Text.Trim();
                    SalvaInfo();
                    cmbCategoria.SelectedIndex = cmbCategoriaArchivio.SelectedIndex;
                    string nuovaCategoria = cmbCategoria.SelectedItem.ToString();
                    AggiornaListBox(nuovaCategoria);
                    txtTitolo.Clear();
                    txtTesto.Clear();
                    txtID.Clear();

                }
                    
            }

            
        }

        private void lstInfo_DoubleClick(object sender, EventArgs e)
        {
            if (lstInfo.SelectedItem is Info info)
            {
                txtTitolo.Text = info.Titolo;
                txtTesto.Text = info.Testo;
                cmbCategoria.SelectedItem = info.Categoria;
                txtID.Text = info.ID.ToString();
            }
        }

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

 

        private void cmbCategoriaArchivio_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbCategoriaArchivio.SelectedIndex != -1)
            {
                string nuovaCategoria = cmbCategoriaArchivio.SelectedItem.ToString();

                AggiornaListBox(nuovaCategoria);
            }
        }

        private void btnCancella_Click(object sender, EventArgs e)
        {
            txtID.Text = string.Empty;
            txtTesto.Text = string.Empty;
            txtTitolo.Text = string.Empty;
        }
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


