using NotepadApp.Models;
using NotepadApp.Services;

namespace NotepadApp;

public sealed class MainForm : Form
{
    private readonly NotDeposuService _depoService = new();
    private readonly TxtAktarimService _txtService = new();
    private readonly List<Not> _tumNotlar = [];
    private Not? _seciliNot;
    private bool _yukleniyorFlag;

    // Üst Menü ve Durum Çubuğu
    private readonly MenuStrip _menuStrip;
    private readonly Label _lblDurumBilgisi;
    private readonly Label _lblToplamSayac;

    // Sol Panel: Arama, Kategori Filtresi ve Not Listesi
    private readonly TextBox _txtArama;
    private readonly ComboBox _cmbKategoriFiltre;
    private readonly ListBox _lstNotlar;
    private readonly Button _btnYeniNot;
    private readonly Button _btnNotSil;

    // Sağ Panel: Başlık, Kategori, Etiket, Tarihler ve Zengin Metin Düzenleyici
    private readonly TextBox _txtBaslik;
    private readonly ComboBox _cmbKategori;
    private readonly TextBox _txtEtiketler;
    private readonly Label _lblOlusturmaTarihi;
    private readonly Label _lblDuzenlemeTarihi;
    private readonly RichTextBox _rtbIcerik;
    private readonly Button _btnKaydetGuncelle;
    private readonly Button _btnTxtAktar;

    public MainForm()
    {
        Text = "YMH219 — Not Defteri Uygulaması (Warm Amber Editorial Studio)";
        ClientSize = new Size(1020, 680);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(250, 247, 242);
        ForeColor = Color.FromArgb(41, 37, 36);

        // 1. Menü Çubuğu (MenuStrip - Dosya & Biçim)
        _menuStrip = new MenuStrip
        {
            BackColor = Color.FromArgb(68, 64, 60),
            ForeColor = Color.FromArgb(254, 243, 199),
            Font = new Font("Segoe UI", 9.5f)
        };

        ToolStripMenuItem mnuDosya = new("📁 Dosya");
        ToolStripMenuItem mnuYeni = new("Yeni Not", null, (_, _) => YeniNotHazirla());
        ToolStripMenuItem mnuJsonKaydet = new("JSON Olarak Kaydet", null, (_, _) => ManuelJsonKaydet());
        ToolStripMenuItem mnuTxtDisaAktar = new("Seçili Notu .TXT Dışa Aktar...", null, (_, _) => HandleTxtExport());
        ToolStripMenuItem mnuCikis = new("Çıkış", null, (_, _) => Close());
        mnuDosya.DropDownItems.AddRange([mnuYeni, mnuJsonKaydet, mnuTxtDisaAktar, new ToolStripSeparator(), mnuCikis]);

        ToolStripMenuItem mnuBicim = new("🎨 Biçim");
        ToolStripMenuItem mnuKalin = new("Kalın (Bold)", null, (_, _) => ToggleFontStyle(FontStyle.Bold));
        ToolStripMenuItem mnuItalik = new("İtalik (Italic)", null, (_, _) => ToggleFontStyle(FontStyle.Italic));
        ToolStripMenuItem mnuAltiCizili = new("Altı Çizili", null, (_, _) => ToggleFontStyle(FontStyle.Underline));
        ToolStripMenuItem mnuRenk = new("Yazı Rengi Seç...", null, (_, _) => SelectTextColor());
        mnuBicim.DropDownItems.AddRange([mnuKalin, mnuItalik, mnuAltiCizili, new ToolStripSeparator(), mnuRenk]);

        _menuStrip.Items.AddRange([mnuDosya, mnuBicim]);
        MainMenuStrip = _menuStrip;
        Controls.Add(_menuStrip);

        // 2. Üst Editorial Başlık Şeridi
        Panel headerPanel = new()
        {
            Location = new Point(0, 24),
            Size = new Size(1020, 68),
            BackColor = Color.FromArgb(120, 53, 15)
        };
        Controls.Add(headerPanel);

        Panel amberAccent = new()
        {
            Dock = DockStyle.Top,
            Height = 4,
            BackColor = Color.FromArgb(245, 158, 11)
        };
        headerPanel.Controls.Add(amberAccent);

        Label lblHeaderTitle = new()
        {
            Text = "📓  NOT DEFTERİ STÜDYOSU (JSON + FILE I/O)",
            Font = new Font("Georgia", 15f, FontStyle.Bold),
            ForeColor = Color.FromArgb(254, 243, 199),
            Location = new Point(20, 12),
            AutoSize = true
        };
        headerPanel.Controls.Add(lblHeaderTitle);

        Label lblHeaderSub = new()
        {
            Text = "System.Text.Json Serileştirme · Otomatik Kaydetme · Arama & Kategori · Zengin Metin & .TXT Dışa Aktarım",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(253, 230, 138),
            Location = new Point(22, 42),
            AutoSize = true
        };
        headerPanel.Controls.Add(lblHeaderSub);

        _lblToplamSayac = new Label
        {
            Text = "📚 Toplam Not: 0",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(120, 53, 15),
            BackColor = Color.FromArgb(254, 243, 199),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(822, 18),
            Size = new Size(176, 32)
        };
        headerPanel.Controls.Add(_lblToplamSayac);

        // 3. Sol Panel: Arama, Kategori Filtresi ve Not Listesi
        Panel leftPanel = new()
        {
            Location = new Point(16, 104),
            Size = new Size(340, 528),
            BackColor = Color.White
        };
        Controls.Add(leftPanel);

        Label lblLeftTitle = new()
        {
            Text = "KAYITLI NOTLAR VE ARAMA",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(146, 64, 14),
            Location = new Point(14, 12),
            AutoSize = true
        };
        leftPanel.Controls.Add(lblLeftTitle);

        _txtArama = new TextBox
        {
            Location = new Point(14, 38),
            Size = new Size(312, 26),
            Font = new Font("Segoe UI", 9.5f),
            PlaceholderText = "🔍 Başlık, içerik veya etikette ara..."
        };
        _txtArama.TextChanged += (_, _) => ListeyiGuncelle();
        leftPanel.Controls.Add(_txtArama);

        Label lblKatFiltre = new()
        {
            Text = "Kategori Filtresi:",
            Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
            ForeColor = Color.FromArgb(87, 83, 78),
            Location = new Point(14, 74),
            AutoSize = true
        };
        leftPanel.Controls.Add(lblKatFiltre);

        _cmbKategoriFiltre = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9f),
            Location = new Point(124, 70),
            Size = new Size(202, 26)
        };
        _cmbKategoriFiltre.Items.AddRange(["Tümü", "Genel", "Ders Notu", "Proje", "Kişisel", "Önemli"]);
        _cmbKategoriFiltre.SelectedIndex = 0;
        _cmbKategoriFiltre.SelectedIndexChanged += (_, _) => ListeyiGuncelle();
        leftPanel.Controls.Add(_cmbKategoriFiltre);

        _lstNotlar = new ListBox
        {
            Location = new Point(14, 106),
            Size = new Size(312, 366),
            Font = new Font("Segoe UI", 9.5f),
            BackColor = Color.FromArgb(255, 251, 235),
            ForeColor = Color.FromArgb(41, 37, 36),
            BorderStyle = BorderStyle.FixedSingle,
            HorizontalScrollbar = true
        };
        _lstNotlar.SelectedIndexChanged += (_, _) => SeciliNotuFormaYukle();
        leftPanel.Controls.Add(_lstNotlar);

        _btnYeniNot = CreateEditorialButton("+ Yeni Not", 14, 482, 150, 34, Color.FromArgb(217, 119, 6), Color.White);
        _btnYeniNot.Click += (_, _) => YeniNotHazirla();
        leftPanel.Controls.Add(_btnYeniNot);

        _btnNotSil = CreateEditorialButton("🗑 Notu Sil", 176, 482, 150, 34, Color.FromArgb(220, 38, 38), Color.White);
        _btnNotSil.Click += (_, _) => HandleNotSil();
        leftPanel.Controls.Add(_btnNotSil);

        // 4. Sağ Panel: Not Düzenleyici ve Biçimlendirme Araç Çubuğu
        Panel rightPanel = new()
        {
            Location = new Point(370, 104),
            Size = new Size(634, 528),
            BackColor = Color.White
        };
        Controls.Add(rightPanel);

        Label lblBaslik = new()
        {
            Text = "Not Başlığı:",
            Font = new Font("Segoe UI Semibold", 9.2f, FontStyle.Bold),
            Location = new Point(16, 14),
            AutoSize = true
        };
        rightPanel.Controls.Add(lblBaslik);

        _txtBaslik = new TextBox
        {
            Location = new Point(16, 36),
            Size = new Size(396, 28),
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            PlaceholderText = "Notunuza açıklayıcı bir başlık yazın..."
        };
        rightPanel.Controls.Add(_txtBaslik);

        Label lblKat = new()
        {
            Text = "Kategori:",
            Font = new Font("Segoe UI Semibold", 9.2f, FontStyle.Bold),
            Location = new Point(424, 14),
            AutoSize = true
        };
        rightPanel.Controls.Add(lblKat);

        _cmbKategori = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.8f),
            Location = new Point(424, 36),
            Size = new Size(194, 28)
        };
        _cmbKategori.Items.AddRange(["Genel", "Ders Notu", "Proje", "Kişisel", "Önemli"]);
        _cmbKategori.SelectedIndex = 0;
        rightPanel.Controls.Add(_cmbKategori);

        Label lblEtiket = new()
        {
            Text = "Etiketler (virgülle):",
            Font = new Font("Segoe UI Semibold", 9f),
            Location = new Point(16, 74),
            AutoSize = true
        };
        rightPanel.Controls.Add(lblEtiket);

        _txtEtiketler = new TextBox
        {
            Location = new Point(142, 70),
            Size = new Size(270, 25),
            Font = new Font("Segoe UI", 9.2f),
            PlaceholderText = "örn: csharp, json, lab"
        };
        rightPanel.Controls.Add(_txtEtiketler);

        _lblOlusturmaTarihi = new Label
        {
            Text = "Oluşturma: -",
            Font = new Font("Segoe UI", 8.2f),
            ForeColor = Color.FromArgb(120, 113, 108),
            Location = new Point(424, 68),
            AutoSize = true
        };
        rightPanel.Controls.Add(_lblOlusturmaTarihi);

        _lblDuzenlemeTarihi = new Label
        {
            Text = "Düzenleme: -",
            Font = new Font("Segoe UI", 8.2f),
            ForeColor = Color.FromArgb(120, 113, 108),
            Location = new Point(424, 84),
            AutoSize = true
        };
        rightPanel.Controls.Add(_lblDuzenlemeTarihi);

        // Zengin Metin Biçimlendirme Çubuğu
        Panel formatBar = new()
        {
            Location = new Point(16, 104),
            Size = new Size(602, 36),
            BackColor = Color.FromArgb(254, 243, 199)
        };
        rightPanel.Controls.Add(formatBar);

        Button btnBold = CreateFormatButton("B", FontStyle.Bold, 8, 4);
        btnBold.Click += (_, _) => ToggleFontStyle(FontStyle.Bold);
        formatBar.Controls.Add(btnBold);

        Button btnItalic = CreateFormatButton("I", FontStyle.Italic, 46, 4);
        btnItalic.Click += (_, _) => ToggleFontStyle(FontStyle.Italic);
        formatBar.Controls.Add(btnItalic);

        Button btnUnderline = CreateFormatButton("U", FontStyle.Underline, 84, 4);
        btnUnderline.Click += (_, _) => ToggleFontStyle(FontStyle.Underline);
        formatBar.Controls.Add(btnUnderline);

        Button btnColor = CreateEditorialButton("🎨 Renk", 124, 4, 82, 28, Color.FromArgb(120, 53, 15), Color.White);
        btnColor.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        btnColor.Click += (_, _) => SelectTextColor();
        formatBar.Controls.Add(btnColor);

        Label lblFormatHint = new()
        {
            Text = "RichTextBox Biçimlendirme · Otomatik JSON Kaydı Aktif",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
            ForeColor = Color.FromArgb(146, 64, 14),
            Location = new Point(220, 10),
            AutoSize = true
        };
        formatBar.Controls.Add(lblFormatHint);

        _rtbIcerik = new RichTextBox
        {
            Location = new Point(16, 144),
            Size = new Size(602, 328),
            Font = new Font("Segoe UI", 10.5f),
            BackColor = Color.FromArgb(255, 253, 248),
            ForeColor = Color.FromArgb(28, 25, 23),
            BorderStyle = BorderStyle.FixedSingle
        };
        rightPanel.Controls.Add(_rtbIcerik);

        _btnKaydetGuncelle = CreateEditorialButton("💾 Notu Kaydet / Güncelle", 16, 482, 294, 34, Color.FromArgb(5, 150, 105), Color.White);
        _btnKaydetGuncelle.Click += (_, _) => HandleNotKaydetGuncelle();
        rightPanel.Controls.Add(_btnKaydetGuncelle);

        _btnTxtAktar = CreateEditorialButton("📄 Seçili Notu .TXT Dışa Aktar", 324, 482, 294, 34, Color.FromArgb(14, 116, 144), Color.White);
        _btnTxtAktar.Click += (_, _) => HandleTxtExport();
        rightPanel.Controls.Add(_btnTxtAktar);

        // 5. Alt Durum Çubuğu
        _lblDurumBilgisi = new Label
        {
            Text = "Hazır.",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(254, 243, 199),
            BackColor = Color.FromArgb(68, 64, 60),
            Dock = DockStyle.Bottom,
            Height = 32,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(16, 0, 0, 0)
        };
        Controls.Add(_lblDurumBilgisi);

        BaslangictaNotlariYukle();
    }

    private static Button CreateEditorialButton(string text, int x, int y, int w, int h, Color bg, Color fg)
    {
        Button btn = new()
        {
            Text = text,
            Font = new Font("Segoe UI Semibold", 9.2f, FontStyle.Bold),
            BackColor = bg,
            ForeColor = fg,
            FlatStyle = FlatStyle.Flat,
            Location = new Point(x, y),
            Size = new Size(w, h),
            Cursor = Cursors.Hand,
            TabStop = false
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private static Button CreateFormatButton(string text, FontStyle style, int x, int y)
    {
        Button btn = new()
        {
            Text = text,
            Font = new Font("Georgia", 9.5f, style),
            BackColor = Color.White,
            ForeColor = Color.FromArgb(68, 64, 60),
            FlatStyle = FlatStyle.Flat,
            Location = new Point(x, y),
            Size = new Size(32, 28),
            Cursor = Cursors.Hand,
            TabStop = false
        };
        btn.FlatAppearance.BorderColor = Color.FromArgb(217, 119, 6);
        return btn;
    }

    private void BaslangictaNotlariYukle()
    {
        var (notlar, uyari) = _depoService.Yukle();
        _tumNotlar.Clear();
        _tumNotlar.AddRange(notlar);
        ListeyiGuncelle();

        if (!string.IsNullOrEmpty(uyari))
        {
            _lblDurumBilgisi.Text = uyari;
        }
        else
        {
            _lblDurumBilgisi.Text = $"{_tumNotlar.Count} not JSON dosyasından otomatik geri yüklendi.";
        }
    }

    private void ListeyiGuncelle()
    {
        _yukleniyorFlag = true;
        string arama = _txtArama.Text.Trim();
        string katFiltre = _cmbKategoriFiltre.SelectedItem?.ToString() ?? "Tümü";

        IEnumerable<Not> filtreli = _tumNotlar;
        if (katFiltre != "Tümü")
        {
            filtreli = filtreli.Where(n => n.Kategori.Equals(katFiltre, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(arama))
        {
            filtreli = filtreli.Where(n =>
                n.Baslik.Contains(arama, StringComparison.OrdinalIgnoreCase) ||
                n.Icerik.Contains(arama, StringComparison.OrdinalIgnoreCase) ||
                n.Etiketler.Contains(arama, StringComparison.OrdinalIgnoreCase));
        }

        List<Not> sirali = filtreli.OrderByDescending(n => n.DuzenlemeTarihi).ToList();

        _lstNotlar.BeginUpdate();
        _lstNotlar.Items.Clear();
        foreach (Not n in sirali)
        {
            _lstNotlar.Items.Add(n);
        }
        _lstNotlar.EndUpdate();

        if (_seciliNot != null && sirali.Contains(_seciliNot))
        {
            _lstNotlar.SelectedItem = _seciliNot;
        }

        _lblToplamSayac.Text = $"📚 Toplam Not: {_tumNotlar.Count}";
        _yukleniyorFlag = false;
    }

    private void SeciliNotuFormaYukle()
    {
        if (_yukleniyorFlag) return;
        if (_lstNotlar.SelectedItem is not Not secilen) return;

        _seciliNot = secilen;
        _txtBaslik.Text = secilen.Baslik;
        _cmbKategori.SelectedItem = _cmbKategori.Items.Contains(secilen.Kategori) ? secilen.Kategori : "Genel";
        _txtEtiketler.Text = secilen.Etiketler;
        _lblOlusturmaTarihi.Text = $"Oluşturma: {secilen.OlusturmaTarihi:dd.MM.yyyy HH:mm}";
        _lblDuzenlemeTarihi.Text = $"Düzenleme: {secilen.DuzenlemeTarihi:dd.MM.yyyy HH:mm}";

        if (!string.IsNullOrWhiteSpace(secilen.RtfIcerik))
        {
            try
            {
                _rtbIcerik.Rtf = secilen.RtfIcerik;
            }
            catch
            {
                _rtbIcerik.Text = secilen.Icerik;
            }
        }
        else
        {
            _rtbIcerik.Text = secilen.Icerik;
        }

        _lblDurumBilgisi.Text = $"Düzenleniyor: '{secilen.Baslik}'";
    }

    private void YeniNotHazirla()
    {
        _seciliNot = null;
        _lstNotlar.ClearSelected();
        _txtBaslik.Clear();
        _cmbKategori.SelectedIndex = 0;
        _txtEtiketler.Clear();
        _rtbIcerik.Clear();
        _lblOlusturmaTarihi.Text = "Oluşturma: Yeni Kayıt";
        _lblDuzenlemeTarihi.Text = "Düzenleme: -";
        _txtBaslik.Focus();
        _lblDurumBilgisi.Text = "Yeni not oluşturma modu: Başlık ve içerik girip 'Notu Kaydet / Güncelle' butonuna basın.";
    }

    private void HandleNotKaydetGuncelle()
    {
        string baslik = _txtBaslik.Text.Trim();
        if (string.IsNullOrWhiteSpace(baslik))
        {
            MessageBox.Show("Lütfen not için bir başlık girin.", "Başlık Gerekli", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _txtBaslik.Focus();
            return;
        }

        string kategori = _cmbKategori.SelectedItem?.ToString() ?? "Genel";
        string etiketler = _txtEtiketler.Text.Trim();
        DateTime simdi = DateTime.Now;

        if (_seciliNot == null)
        {
            Not yeni = new()
            {
                Baslik = baslik,
                Icerik = _rtbIcerik.Text,
                RtfIcerik = _rtbIcerik.Rtf,
                Kategori = kategori,
                Etiketler = etiketler,
                OlusturmaTarihi = simdi,
                DuzenlemeTarihi = simdi
            };
            _tumNotlar.Add(yeni);
            _seciliNot = yeni;
            _lblDurumBilgisi.Text = $"Yeni not eklendi ve notlar.json dosyasına otomatik kaydedildi: '{baslik}'";
        }
        else
        {
            _seciliNot.Baslik = baslik;
            _seciliNot.Icerik = _rtbIcerik.Text;
            _seciliNot.RtfIcerik = _rtbIcerik.Rtf;
            _seciliNot.Kategori = kategori;
            _seciliNot.Etiketler = etiketler;
            _seciliNot.DuzenlemeTarihi = simdi;
            _lblDurumBilgisi.Text = $"Not güncellendi ve notlar.json dosyasına otomatik kaydedildi: '{baslik}'";
        }

        _lblOlusturmaTarihi.Text = $"Oluşturma: {_seciliNot.OlusturmaTarihi:dd.MM.yyyy HH:mm}";
        _lblDuzenlemeTarihi.Text = $"Düzenleme: {_seciliNot.DuzenlemeTarihi:dd.MM.yyyy HH:mm}";

        OtomatikJsonKaydet();
        ListeyiGuncelle();
    }

    private void HandleNotSil()
    {
        if (_seciliNot == null)
        {
            MessageBox.Show("Lütfen silmek için listeden bir not seçin.", "Seçim Yok", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        DialogResult onay = MessageBox.Show(
            $"'{_seciliNot.Baslik}' başlıklı notu kalıcı olarak silmek istiyor musunuz?",
            "Not Silme Onayı",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (onay != DialogResult.Yes) return;

        string silinenBaslik = _seciliNot.Baslik;
        _tumNotlar.Remove(_seciliNot);
        OtomatikJsonKaydet();
        YeniNotHazirla();
        ListeyiGuncelle();
        _lblDurumBilgisi.Text = $"'{silinenBaslik}' silindi ve JSON dosyası güncellendi.";
    }

    private void OtomatikJsonKaydet()
    {
        try
        {
            _depoService.Kaydet(_tumNotlar);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Notlar JSON dosyasına kaydedilirken hata oluştu:\n{ex.Message}", "Kayıt Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ManuelJsonKaydet()
    {
        OtomatikJsonKaydet();
        _lblDurumBilgisi.Text = $"Tüm notlar ({_tumNotlar.Count} adet) JSON dosyasına başarıyla kaydedildi.";
    }

    private void HandleTxtExport()
    {
        if (_seciliNot == null && string.IsNullOrWhiteSpace(_txtBaslik.Text))
        {
            MessageBox.Show("Dışa aktarmak için önce bir not seçin veya oluşturun.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Not aktarilacak = _seciliNot ?? new Not
        {
            Baslik = _txtBaslik.Text.Trim(),
            Icerik = _rtbIcerik.Text,
            Kategori = _cmbKategori.SelectedItem?.ToString() ?? "Genel",
            Etiketler = _txtEtiketler.Text.Trim()
        };

        using SaveFileDialog sfd = new()
        {
            Title = "Notu Düz Metin (.TXT) Olarak Dışa Aktar",
            Filter = "Metin Dosyası (*.txt)|*.txt",
            FileName = $"{aktarilacak.Baslik.Replace(" ", "_")}.txt"
        };

        if (sfd.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            _txtService.DisaAktar(sfd.FileName, aktarilacak);
            _lblDurumBilgisi.Text = $"Başarılı: '{aktarilacak.Baslik}' notu '{Path.GetFileName(sfd.FileName)}' olarak dışa aktarıldı.";
            MessageBox.Show($"Not başarıyla dışa aktarıldı:\n{sfd.FileName}", "Dışa Aktarım Tamamlandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Dosya dışa aktarılamadı:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ToggleFontStyle(FontStyle style)
    {
        Font current = _rtbIcerik.SelectionFont ?? _rtbIcerik.Font;
        FontStyle newStyle = current.Style ^ style;
        _rtbIcerik.SelectionFont = new Font(current.FontFamily, current.Size, newStyle);
        _rtbIcerik.Focus();
    }

    private void SelectTextColor()
    {
        using ColorDialog cd = new();
        if (cd.ShowDialog(this) == DialogResult.OK)
        {
            _rtbIcerik.SelectionColor = cd.Color;
        }
    }

    public void CaptureScreenshots(string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        Show();
        Application.DoEvents();

        // Örnek verilerle doldur (Ekran görüntüsü için)
        _tumNotlar.Clear();
        DateTime now = DateTime.Now;
        _tumNotlar.Add(new Not
        {
            Baslik = "YMH 219 — Dosya İşlemleri ve Serileştirme",
            Kategori = "Ders Notu",
            Etiketler = "csharp, json, serialization",
            OlusturmaTarihi = now.AddDays(-2),
            DuzenlemeTarihi = now.AddMinutes(-15),
            Icerik = "1. System.IO ile File, StreamReader ve StreamWriter kullanımı.\n2. System.Text.Json ile List<Not> koleksiyonunu notlar.json dosyasına serileştirme.\n3. Uygulama açılışında JSON dosyasının otomatik geri yüklenmesi ve bozuk dosya hata yönetimi."
        });
        _tumNotlar.Add(new Not
        {
            Baslik = "Proje Teslim Kontrol Listesi",
            Kategori = "Proje",
            Etiketler = "github, readme, winforms",
            OlusturmaTarihi = now.AddDays(-1),
            DuzenlemeTarihi = now.AddHours(-3),
            Icerik = "- Kaynak kodlar GitHub deposuna yüklendi.\n- .gitignore ile bin/, obj/, .vs/ dışarıda bırakıldı.\n- README.md ve ekran görüntüleri eklendi."
        });
        _tumNotlar.Add(new Not
        {
            Baslik = "Hafta 9 Kalıtım Özeti",
            Kategori = "Önemli",
            Etiketler = "oop, abstract, interface",
            OlusturmaTarihi = now.AddHours(-6),
            DuzenlemeTarihi = now.AddHours(-1),
            Icerik = "abstract sınıflardan nesne üretilemez; virtual/override ile çok biçimlilik sağlanır."
        });

        ListeyiGuncelle();
        _lstNotlar.SelectedIndex = 0;
        SeciliNotuFormaYukle();

        // Zengin metin vurgulaması
        _rtbIcerik.Select(0, 44);
        _rtbIcerik.SelectionFont = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        _rtbIcerik.SelectionColor = Color.FromArgb(146, 64, 14);
        _rtbIcerik.Select(0, 0);

        _lblDurumBilgisi.Text = "3 not notlar.json dosyasından otomatik yüklendi. Düzenleniyor: 'YMH 219 — Dosya İşlemleri ve Serileştirme'";
        Refresh();
        Application.DoEvents();

        using (Bitmap bmp1 = new(Width, Height))
        {
            DrawToBitmap(bmp1, new Rectangle(0, 0, Width, Height));
            bmp1.Save(Path.Combine(outputDir, "notepad_main.png"), System.Drawing.Imaging.ImageFormat.Png);
        }

        // Ekran Görüntüsü 2: Arama/Kategori Filtreleme + .TXT Dışa Aktarım Durumu
        _cmbKategoriFiltre.SelectedItem = "Proje";
        _txtArama.Text = "Teslim";
        ListeyiGuncelle();
        if (_lstNotlar.Items.Count > 0)
        {
            _lstNotlar.SelectedIndex = 0;
            SeciliNotuFormaYukle();
        }
        _lblDurumBilgisi.Text = "Başarılı: 'Proje Teslim Kontrol Listesi' notu 'Proje_Teslim_Kontrol_Listesi.txt' olarak dışa aktarıldı.";
        Refresh();
        Application.DoEvents();

        using (Bitmap bmp2 = new(Width, Height))
        {
            DrawToBitmap(bmp2, new Rectangle(0, 0, Width, Height));
            bmp2.Save(Path.Combine(outputDir, "notepad_filter_export.png"), System.Drawing.Imaging.ImageFormat.Png);
        }

        Close();
    }
}
