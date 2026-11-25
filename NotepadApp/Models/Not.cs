namespace NotepadApp.Models;

/// <summary>
/// Tek bir not kaydını, kategori/etiket bilgilerini ve oluşturma/düzenleme tarihlerini temsil eder.
/// </summary>
public sealed class Not
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Baslik { get; set; } = string.Empty;
    public string Icerik { get; set; } = string.Empty;
    public string? RtfIcerik { get; set; }
    public string Kategori { get; set; } = "Genel";
    public string Etiketler { get; set; } = string.Empty;
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
    public DateTime DuzenlemeTarihi { get; set; } = DateTime.Now;

    public string ListeGorunumu => $"[{Kategori}] {Baslik}  ({DuzenlemeTarihi:dd.MM.yyyy HH:mm})";

    public override string ToString() => ListeGorunumu;
}
