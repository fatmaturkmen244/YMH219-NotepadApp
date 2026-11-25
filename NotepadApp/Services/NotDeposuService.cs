using System.Text;
using System.Text.Json;
using NotepadApp.Models;

namespace NotepadApp.Services;

/// <summary>
/// Not koleksiyonunu (List&lt;Not&gt;) System.Text.Json ile diske serileştirir,
/// açılışta otomatik yükler ve bozuk/eksik dosya durumlarını güvenli biçimde yönetir.
/// </summary>
public sealed class NotDeposuService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public string DosyaYolu { get; }

    public NotDeposuService(string? ozelDosyaYolu = null)
    {
        DosyaYolu = ozelDosyaYolu ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "notlar.json");
    }

    public (List<Not> Notlar, string? UyariMesaji) Yukle()
    {
        try
        {
            if (!File.Exists(DosyaYolu))
            {
                return ([], "Kayıtlı not dosyası henüz yok; yeni not deposu başlatıldı.");
            }

            string json;
            using (StreamReader reader = new(DosyaYolu, Encoding.UTF8))
            {
                json = reader.ReadToEnd();
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                return ([], "Not dosyası boş bulundu; temiz liste ile başlatıldı.");
            }

            List<Not>? yuklenen = JsonSerializer.Deserialize<List<Not>>(json, JsonOptions);
            return (yuklenen ?? [], null);
        }
        catch (JsonException ex)
        {
            // Bozuk JSON dosyasını yedekle ve uygulamanın çökmesini engelle
            try
            {
                string backupPath = DosyaYolu + ".bozuk_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                File.Copy(DosyaYolu, backupPath, overwrite: true);
            }
            catch
            {
                // Yedekleme başarısızsa yutulur
            }

            return ([], $"JSON dosyası bozuk olduğu için okunamadı ({ex.Message}). Boş liste ile devam ediliyor.");
        }
        catch (Exception ex)
        {
            return ([], $"Dosya okuma hatası: {ex.Message}");
        }
    }

    public void Kaydet(IReadOnlyList<Not> notlar)
    {
        string? dir = Path.GetDirectoryName(DosyaYolu);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string json = JsonSerializer.Serialize(notlar, JsonOptions);
        using StreamWriter writer = new(DosyaYolu, append: false, Encoding.UTF8);
        writer.Write(json);
    }
}

/// <summary>
/// Seçili bir notu düz metin (.txt) dosyası olarak StreamWriter ile dışa aktarır.
/// </summary>
public sealed class TxtAktarimService
{
    public void DisaAktar(string hedefDosyaYolu, Not not)
    {
        using StreamWriter writer = new(hedefDosyaYolu, append: false, Encoding.UTF8);
        writer.WriteLine("======================================================================");
        writer.WriteLine($" BAŞLIK           : {not.Baslik}");
        writer.WriteLine($" KATEGORİ         : {not.Kategori}");
        writer.WriteLine($" ETİKETLER        : {(string.IsNullOrWhiteSpace(not.Etiketler) ? "-" : not.Etiketler)}");
        writer.WriteLine($" OLUŞTURMA TARİHİ : {not.OlusturmaTarihi:dd.MM.yyyy HH:mm:ss}");
        writer.WriteLine($" DÜZENLEME TARİHİ : {not.DuzenlemeTarihi:dd.MM.yyyy HH:mm:ss}");
        writer.WriteLine("======================================================================");
        writer.WriteLine();
        writer.WriteLine(not.Icerik);
    }
}
