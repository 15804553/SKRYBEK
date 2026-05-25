using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Models;
using SKRYBEK.Services.Logging;

namespace SKRYBEK.Services.Export;

public sealed class WordExportService
{
    public string ExportRozkaz(RozkazDzienny rozkaz, List<Samochod> samochody, string nrJrg, string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        var fileName = $"Rozkaz_{rozkaz.NumerRozkazu}_{rozkaz.Rok}_{rozkaz.Data:yyyyMMdd}.docx";
        var path = Path.Combine(outputDir, fileName);

        using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());
        var body = mainPart.Document.Body!;

        // Ustawienia strony A4, marginesy
        SetPageProperties(mainPart);

        // Nagłówek dokumentu
        AddHeader(body, rozkaz, nrJrg);

        // 1) SŁUŻBA
        AddSectionTitle(body, "1)   SŁUŻBA");
        AddSluzbaTable(body, rozkaz.Sluzba);

        // 2) PODZIAŁ BOJOWY
        AddSectionTitle(body, "2)   PODZIAŁ BOJOWY");
        AddPodzialBojowyTable(body, rozkaz.PodzialBojowy, samochody);

        // DYŻURNI RATOWNICY MEDYCZNI PSP
        AddRatwnicyMedyczni(body, rozkaz.RatwnicyMedyczni);

        // 3) ZAJĘCIA
        AddSectionTitle(body, "3)   ZAJĘCIA");
        AddZajecia(body, rozkaz.Zajecia);

        // 4) NIEOBECNI W SŁUŻBIE
        AddSectionTitle(body, "4)   NIEOBECNI W SŁUŻBIE");
        AddNieobecniTable(body, rozkaz.Nieobecni);

        // 5) UWAGI
        AddSectionTitle(body, "5)   UWAGI");
        AddUwagi(body, rozkaz.Uwagi);

        // Stopka
        AddFooter(body, nrJrg);

        mainPart.Document.Save();
        SkrybekLog.Info($"Wyeksportowano rozkaz: {path}");
        return path;
    }

    // ── Ustawienia strony ─────────────────────────────────────────────────────

    private static void SetPageProperties(MainDocumentPart mainPart)
    {
        var sectionProps = new SectionProperties(
            new PageSize { Width = 11906, Height = 16838 },
            new PageMargin
            {
                Top    = 720,
                Right  = 720,
                Bottom = 720,
                Left   = 1134,
                Header = 360,
                Footer = 360
            });
        mainPart.Document.Body!.AppendChild(sectionProps);
    }

    // ── Nagłówek ──────────────────────────────────────────────────────────────

    private static void AddHeader(Body body, RozkazDzienny rozkaz, string nrJrg)
    {
        // "Kraków, dn. DD.MM.RRRR"
        body.AppendChild(MakeParagraph(
            $"Kraków, dn. {rozkaz.Data.ToString("dd.MM.yyyy")}",
            alignment: JustificationValues.Right,
            fontSize: 20));

        // Boldowy tytuł
        body.AppendChild(MakeParagraph(
            $"ROZKAZ DZIENNY NR {rozkaz.NumerFormatowany}",
            bold: true,
            fontSize: 24));

        body.AppendChild(MakeParagraph(
            $"Dowódcy Jednostki Ratowniczo Gaśniczej Nr {nrJrg}",
            bold: true,
            fontSize: 20));

        body.AppendChild(MakeParagraph(
            $"na dzień {rozkaz.Data.ToString("dd.MM.yyyy")}",
            fontSize: 20));

        body.AppendChild(EmptyParagraph());
    }

    // ── Sekcja SŁUŻBA ─────────────────────────────────────────────────────────

    private static void AddSluzbaTable(Body body, List<PozycjaSluzby> sluzba)
    {
        var table = CreateBorderlessTable();
        var allPos = Enum.GetValues<StanowiskoSluzby>().Cast<StanowiskoSluzby>().ToList();

        foreach (var stanowisko in allPos)
        {
            var pozycja = sluzba.FirstOrDefault(s => s.Stanowisko == stanowisko);
            var osoba   = pozycja?.Nazwisko ?? ".............................";

            var row = new TableRow();
            row.AppendChild(MakeCell(
                new PozycjaSluzby { Stanowisko = stanowisko }.NazwaStanowiska,
                width: 4000));
            row.AppendChild(MakeCell("-", width: 400, center: true));
            row.AppendChild(MakeCell(osoba, width: 4000));
            row.AppendChild(MakeCell(string.Empty, width: 1000));
            table.AppendChild(row);
        }

        body.AppendChild(table);
        body.AppendChild(EmptyParagraph());
    }

    // ── Podział bojowy ────────────────────────────────────────────────────────

    private static void AddPodzialBojowyTable(Body body, List<PozycjaSamochodu> podział, List<Samochod> samochody)
    {
        var podstawowe = samochody.Where(s => s.CzyPodstawowy).OrderBy(s => s.Kolejnosc).ToList();
        var dodatkowe  = samochody.Where(s => !s.CzyPodstawowy).OrderBy(s => s.Kolejnosc).ToList();

        if (podstawowe.Count > 0)
        {
            var table = CreateBorderTable();

            // Wiersz nagłówków pojazdów
            var headerRow = new TableRow();
            foreach (var s in podstawowe)
                headerRow.AppendChild(MakeCell(s.Nazwa, bold: true, center: true, width: 3100, shading: "E0E0E0"));
            table.AppendChild(headerRow);

            // Wiersze z osobami (maks pozycji)
            int maxPoz = podstawowe.Max(s => s.LiczbaPozycji);
            for (int poz = 1; poz <= maxPoz; poz++)
            {
                var row = new TableRow();
                foreach (var sam in podstawowe)
                {
                    var wpis = podział.FirstOrDefault(p => p.SamochodId == sam.Id && p.Pozycja == poz);
                    row.AppendChild(MakeCell(
                        wpis?.Nazwisko ?? string.Empty,
                        width: 3100));
                }
                table.AppendChild(row);
            }
            body.AppendChild(table);
            body.AppendChild(EmptyParagraph());
        }

        // Grupy specjalne (dodatkowe)
        if (dodatkowe.Count > 0)
        {
            int cols = Math.Min(dodatkowe.Count, 3);
            var table = CreateBorderTable();

            var headerRow = new TableRow();
            for (int i = 0; i < cols && i < dodatkowe.Count; i++)
                headerRow.AppendChild(MakeCell(dodatkowe[i].Nazwa, bold: true, center: true, width: 3100, shading: "E0E0E0"));
            table.AppendChild(headerRow);

            int maxPoz = dodatkowe.Take(cols).Max(s => s.LiczbaPozycji);
            for (int poz = 1; poz <= maxPoz; poz++)
            {
                var row = new TableRow();
                for (int i = 0; i < cols && i < dodatkowe.Count; i++)
                {
                    var sam  = dodatkowe[i];
                    var wpis = podział.FirstOrDefault(p => p.SamochodId == sam.Id && p.Pozycja == poz);
                    row.AppendChild(MakeCell(wpis?.Nazwisko ?? string.Empty, width: 3100));
                }
                table.AppendChild(row);
            }
            body.AppendChild(table);
            body.AppendChild(EmptyParagraph());
        }
    }

    // ── Ratownicy medyczni ────────────────────────────────────────────────────

    private static void AddRatwnicyMedyczni(Body body, List<RatownikMedyczny> ratownicy)
    {
        body.AppendChild(MakeParagraph("DYŻURNI RATOWNICY MEDYCZNI PSP", bold: true, fontSize: 20));

        var r1 = ratownicy.FirstOrDefault(r => r.Pozycja == 1)?.Nazwisko ?? string.Empty;
        var r2 = ratownicy.FirstOrDefault(r => r.Pozycja == 2)?.Nazwisko ?? string.Empty;

        var table = CreateBorderlessTable();
        var row = new TableRow();
        row.AppendChild(MakeCell($"1.  {r1}", width: 4500));
        row.AppendChild(MakeCell($"2.  {r2}", width: 4500));
        table.AppendChild(row);
        body.AppendChild(table);
        body.AppendChild(EmptyParagraph());
    }

    // ── Zajęcia ───────────────────────────────────────────────────────────────

    private static void AddZajecia(Body body, string zajecia)
    {
        body.AppendChild(MakeParagraph(
            string.IsNullOrWhiteSpace(zajecia) ? "............................................................................" : zajecia,
            fontSize: 20));
        body.AppendChild(MakeParagraph("............................................................................", fontSize: 20));
        body.AppendChild(EmptyParagraph());
    }

    // ── Nieobecni ─────────────────────────────────────────────────────────────

    private static void AddNieobecniTable(Body body, List<NieobecnyWSluzbie> nieobecni)
    {
        var urlopy     = nieobecni.Where(n => n.TypNieobecnosci == TypNieobecnosci.Urlop).ToList();
        var wolny      = nieobecni.Where(n => n.TypNieobecnosci == TypNieobecnosci.CzasWolny).ToList();
        var chorzy     = nieobecni.Where(n => n.TypNieobecnosci == TypNieobecnosci.Chory).ToList();
        var delegowani = nieobecni.Where(n => n.TypNieobecnosci == TypNieobecnosci.Delegowany).ToList();
        var domowy     = nieobecni.Where(n => n.TypNieobecnosci == TypNieobecnosci.DyzurDomowy).ToList();

        var table = CreateBorderTable();

        // Nagłówek kolumn
        var hdr = new TableRow();
        hdr.AppendChild(MakeCell("URLOPY", bold: true, center: true, width: 3000, shading: "E8E8E8"));
        hdr.AppendChild(MakeCell("CZAS WOLNY", bold: true, center: true, width: 3000, shading: "E8E8E8"));
        hdr.AppendChild(MakeCell("CHORZY", bold: true, center: true, width: 2000, shading: "E8E8E8"));
        hdr.AppendChild(MakeCell("DELEGOWANI", bold: true, center: true, width: 2000, shading: "E8E8E8"));
        table.AppendChild(hdr);

        int maxRows = Math.Max(Math.Max(urlopy.Count, wolny.Count), Math.Max(chorzy.Count, delegowani.Count));
        maxRows = Math.Max(maxRows, 4);

        for (int i = 0; i < maxRows; i++)
        {
            var row = new TableRow();
            row.AppendChild(MakeCell(FormatNieobecny(urlopy, i), width: 3000));
            row.AppendChild(MakeCell(FormatNieobecny(wolny, i), width: 3000));
            row.AppendChild(MakeCell(FormatNieobecny(chorzy, i), width: 2000));
            row.AppendChild(MakeCell(FormatNieobecny(delegowani, i), width: 2000));
            table.AppendChild(row);
        }
        body.AppendChild(table);

        // DYŻUR DOMOWY
        body.AppendChild(EmptyParagraph());
        body.AppendChild(MakeParagraph("DYŻUR DOMOWY:", bold: true, fontSize: 20));

        var domTbl = CreateBorderlessTable();
        for (int i = 0; i < domowy.Count; i += 2)
        {
            var row = new TableRow();
            row.AppendChild(MakeCell($"{i + 1}.  {domowy[i].Nazwisko}", width: 4500));
            var n2 = i + 1 < domowy.Count ? $"{i + 2}.  {domowy[i + 1].Nazwisko}" : string.Empty;
            row.AppendChild(MakeCell(n2, width: 4500));
            domTbl.AppendChild(row);
        }
        if (domowy.Count == 0)
        {
            var row = new TableRow();
            row.AppendChild(MakeCell("1.  ......................................", width: 4500));
            row.AppendChild(MakeCell("2.  ......................................", width: 4500));
            domTbl.AppendChild(row);
        }
        body.AppendChild(domTbl);
        body.AppendChild(EmptyParagraph());
    }

    private static string FormatNieobecny(List<NieobecnyWSluzbie> lista, int idx)
        => idx < lista.Count ? $"{idx + 1}.  {lista[idx].Nazwisko}" : $"{idx + 1}.  .....................................";

    // ── Uwagi ─────────────────────────────────────────────────────────────────

    private static void AddUwagi(Body body, string uwagi)
    {
        var text = string.IsNullOrWhiteSpace(uwagi) ? string.Empty : uwagi;
        body.AppendChild(MakeParagraph(text, fontSize: 20));
        body.AppendChild(MakeParagraph("............................................................................", fontSize: 20));
        body.AppendChild(MakeParagraph("............................................................................", fontSize: 20));
        body.AppendChild(EmptyParagraph());
    }

    // ── Stopka ────────────────────────────────────────────────────────────────

    private static void AddFooter(Body body, string nrJrg)
    {
        body.AppendChild(MakeParagraph(
            $"Rozkaz podpisał D-ca JRG-{nrJrg}",
            alignment: JustificationValues.Right,
            bold: true,
            fontSize: 20));
        body.AppendChild(MakeParagraph(
            "................................",
            alignment: JustificationValues.Right,
            fontSize: 20));
    }

    // ── Pomocniki ─────────────────────────────────────────────────────────────

    private static void AddSectionTitle(Body body, string text)
    {
        body.AppendChild(MakeParagraph(text, bold: true, fontSize: 22));
    }

    private static Paragraph EmptyParagraph()
        => new(new ParagraphProperties(new SpacingBetweenLines { Before = "0", After = "60" }));

    private static Paragraph MakeParagraph(
        string text,
        bool bold = false,
        int fontSize = 20,
        JustificationValues? alignment = null)
    {
        var runProps = new RunProperties();
        if (bold) runProps.AppendChild(new Bold());
        runProps.AppendChild(new FontSize { Val = fontSize.ToString() });
        runProps.AppendChild(new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" });

        var align = alignment ?? JustificationValues.Left;
        var para = new Paragraph(
            new ParagraphProperties(
                new Justification { Val = align },
                new SpacingBetweenLines { Before = "0", After = "60" }),
            new Run(runProps, new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
        return para;
    }

    private static Table CreateBorderlessTable()
    {
        var tbl = new Table();
        tbl.AppendChild(new TableProperties(
            new TableBorders(
                new TopBorder    { Val = BorderValues.None },
                new BottomBorder { Val = BorderValues.None },
                new LeftBorder   { Val = BorderValues.None },
                new RightBorder  { Val = BorderValues.None },
                new InsideHorizontalBorder { Val = BorderValues.None },
                new InsideVerticalBorder   { Val = BorderValues.None })));
        return tbl;
    }

    private static Table CreateBorderTable()
    {
        var tbl = new Table();
        tbl.AppendChild(new TableProperties(
            new TableBorders(
                new TopBorder    { Val = BorderValues.Single, Size = 4 },
                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                new LeftBorder   { Val = BorderValues.Single, Size = 4 },
                new RightBorder  { Val = BorderValues.Single, Size = 4 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                new InsideVerticalBorder   { Val = BorderValues.Single, Size = 4 })));
        return tbl;
    }

    private static TableCell MakeCell(
        string text,
        int width = 2000,
        bool bold = false,
        bool center = false,
        string? shading = null)
    {
        var cell = new TableCell();
        var cellProps = new TableCellProperties(
            new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = width.ToString() });
        if (shading is not null)
            cellProps.AppendChild(new Shading { Fill = shading, Val = ShadingPatternValues.Clear });
        cell.AppendChild(cellProps);

        var runProps = new RunProperties();
        if (bold) runProps.AppendChild(new Bold());
        runProps.AppendChild(new FontSize { Val = "20" });
        runProps.AppendChild(new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" });

        var alignment = center ? JustificationValues.Center : JustificationValues.Left;
        cell.AppendChild(new Paragraph(
            new ParagraphProperties(
                new Justification { Val = alignment },
                new SpacingBetweenLines { Before = "0", After = "40" }),
            new Run(runProps, new Text(text) { Space = SpaceProcessingModeValues.Preserve })));
        return cell;
    }
}
