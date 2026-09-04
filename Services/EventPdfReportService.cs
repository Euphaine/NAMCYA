using EventScoringSystem.Data;
using EventScoringSystem.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EventScoringSystem.Services
{
    public class EventPdfReportService
    {
        private readonly AppDbContext _db;
        private readonly TabulationService _tabulationService;
        private readonly IWebHostEnvironment _env;

        public EventPdfReportService(AppDbContext db, TabulationService tabulationService, IWebHostEnvironment env)
        {
            _db = db;
            _tabulationService = tabulationService;
            _env = env;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<byte[]> GenerateEventReportPdfAsync(int eventId)
        {
            var currentEvent = await _db.Events.FindAsync(eventId);
            if (currentEvent == null) throw new Exception("Event not found");

            var judges = await _db.Judges.Where(j => j.EventId == eventId).OrderBy(j => j.Id).ToListAsync();
            
            // Fetch and sort results by OverallRank ascending, then FinalScore descending
            var results = (await _tabulationService.ComputeEventResultsAsync(eventId))
                          .OrderBy(r => r.OverallRank)
                          .ThenByDescending(r => r.FinalScore)
                          .ToList();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(15, Unit.Millimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Grey.Darken3));

                    // Professional Header Layout
                    page.Header().Element(headerContainer =>
                    {
                        headerContainer.Column(headerCol =>
                        {
                            headerCol.Item().Row(row =>
                            {
                                var logoPath = Path.Combine(_env.WebRootPath, "images", "logo.png");
                                if (File.Exists(logoPath))
                                {
                                    row.ConstantItem(40).Image(logoPath);
                                    row.ConstantItem(10);
                                }

                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("NATIONAL MUSIC COMPETITIONS FOR YOUNG ARTISTS")
                                        .Bold().FontSize(11).FontColor(Colors.Black);
                                    col.Item().Text("OFFICIAL TABULATION & FINAL RANKING REPORT")
                                        .FontSize(8).Bold().FontColor(Colors.Grey.Medium);
                                });

                                row.ConstantItem(120).AlignRight().Column(col =>
                                {
                                    col.Item().Text($"Date: {DateTime.Now:yyyy-MM-dd}").FontSize(8).FontColor(Colors.Grey.Darken1);
                                    col.Item().Text("Status: Certified Official").FontSize(8).Bold().FontColor(Colors.Green.Darken2);
                                });
                            });

                            headerCol.Item().PaddingTop(8).LineHorizontal(1f).LineColor(Colors.Grey.Darken2);
                        });
                    });

                    // Main Content Layout
                    page.Content().Column(col =>
                    {
                        col.Spacing(12);

                        // Event Metadata Block
                        col.Item().PaddingTop(2).AlignCenter().Column(c =>
                        {
                            c.Item().Text(currentEvent.Title.ToUpper()).Bold().FontSize(13).FontColor(Colors.Black);
                            if (!string.IsNullOrEmpty(currentEvent.Description))
                            {
                                c.Item().PaddingTop(2).Text(currentEvent.Description).FontSize(8).FontColor(Colors.Grey.Darken1);
                            }
                            c.Item().PaddingTop(3).Text("Scoring System: Rank-Sum Method with Fractional Ranking System & Average Tie-Breaker").FontSize(7).Italic().FontColor(Colors.Grey.Medium);
                        });

                        // Robust Scalable Tabulation Table (Sorted by Rank)
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30); // Rank width
                                columns.RelativeColumn(4.5f); // Contestant Name & Region
                                
                                foreach (var _ in judges)
                                {
                                    columns.RelativeColumn(2.2f); 
                                }
                                
                                columns.ConstantColumn(38); // Sumrank width
                                columns.ConstantColumn(48); // Final Average Score width
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyleHeader).Text("Rank");
                                header.Cell().Element(CellStyleHeaderLeft).Text("Contestant Name & Region");
                                foreach (var judge in judges)
                                {
                                    header.Cell().Element(CellStyleHeader).Text($"{judge.Name}\n(Sc / Rk)");
                                }
                                header.Cell().Element(CellStyleHeader).Text("Sum");
                                header.Cell().Element(CellStyleHeader).Text("Final Avg");
                            });

                            foreach (var res in results)
                            {
                                string rankDisplay = res.OverallRank switch
                                {
                                    1 => "1st",
                                    2 => "2nd",
                                    3 => "3rd",
                                    1.5m => "1.5",
                                    2.5m => "2.5",
                                    3.5m => "3.5",
                                    _ => res.OverallRank.ToString("0.#")
                                };

                                headerCell(table, rankDisplay);
                                
                                string regionStr = !string.IsNullOrWhiteSpace(res.Contestant.Region) ? $" ({res.Contestant.Region})" : "";
                                headerCellLeft(table, $"#{res.Contestant.ContestantNumber} - {res.Contestant.Name}{regionStr}");

                                foreach (var jd in res.JudgeDetails)
                                {
                                    headerCell(table, $"{jd.Score:0.00}%\nRk: {jd.Rank:0.#}");
                                }

                                headerCell(table, res.RankSum.ToString("0.0"), isBold: true);
                                headerCell(table, $"{res.FinalScore:0.00}%", isBold: true);
                            }
                        });

                        // Official Revelation & Calculation Audit Breakdown Section (Sorted by Rank)
                        col.Item().PaddingTop(10).Column(auditCol =>
                        {
                            auditCol.Item().Text("OFFICIAL REVELATION & CALCULATION AUDIT BREAKDOWN").Bold().FontSize(8).FontColor(Colors.Grey.Darken2);
                            auditCol.Item().PaddingTop(2).LineHorizontal(0.8f).LineColor(Colors.Grey.Lighten1);

                            foreach (var res in results)
                            {
                                auditCol.Item().PaddingTop(5).Background(Colors.Grey.Lighten4).Padding(5).Column(itemCol =>
                                {
                                    string rankText = res.OverallRank % 1 != 0 ? res.OverallRank.ToString("0.#") : ((int)res.OverallRank).ToString();
                                    itemCol.Item().Text(text =>
                                    {
                                        text.Span($"#{res.Contestant.ContestantNumber} - {res.Contestant.Name} ").Bold().FontSize(8).FontColor(Colors.Black);
                                        text.Span($"| Overall Rank: {rankText} | Sumrank: {res.RankSum:0.0} | Final Average: {res.FinalScore:0.00}%").FontSize(7).FontColor(Colors.Grey.Darken2);
                                    });

                                    string explanation = "";
                                    if (res.OverallRank % 1 != 0)
                                    {
                                        explanation = "Contestant is tied with an identical Sumrank and Final Average score, resulting in a shared fractional rank.";
                                    }
                                    else if (results.Count(r => r.RankSum == res.RankSum) > 1)
                                    {
                                        explanation = $"Contestant tied on Sumrank ({res.RankSum:0.0}), but the tie was successfully broken by the Final Average score comparison ({res.FinalScore:0.00}%).";
                                    }
                                    else
                                    {
                                        explanation = "Contestant secured this position cleanly via unique primary Rank-Sum aggregation across all judges.";
                                    }

                                    itemCol.Item().PaddingTop(1).Text($"Rule Application: {explanation}").FontSize(7).Italic().FontColor(Colors.Grey.Darken1);
                                });
                            }
                        });

                        // Scalable Multi-Judge Signatures Block
                        col.Item().PaddingTop(15).Column(sigOuterCol =>
                        {
                            sigOuterCol.Item().Text("BOARD OF JUDGES AUTHENTICATION SIGNATURES").Bold().FontSize(8).FontColor(Colors.Grey.Darken2);
                            sigOuterCol.Item().PaddingTop(3).LineHorizontal(0.8f).LineColor(Colors.Grey.Lighten1);

                            var judgeChunks = judges.Chunk(3).ToList();
                            foreach (var chunk in judgeChunks)
                            {
                                sigOuterCol.Item().PaddingTop(15).Row(row =>
                                {
                                    foreach (var judge in chunk)
                                    {
                                        row.RelativeItem().PaddingHorizontal(8).Column(c => BuildSignatureColumn(c, judge));
                                    }

                                    for (int i = chunk.Length; i < 3; i++)
                                    {
                                        row.RelativeItem();
                                    }
                                });
                            }
                        });
                    });

                    // Professional Footer
                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().Text("NAMCYA Official Tabulation System — Generated Report").FontSize(7).FontColor(Colors.Grey.Medium);
                        row.RelativeItem().AlignRight().Text(text =>
                        {
                            text.Span("Page ").FontSize(7).FontColor(Colors.Grey.Medium);
                            text.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
                            text.Span(" of ").FontSize(7).FontColor(Colors.Grey.Medium);
                            text.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium);
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }

        private void BuildSignatureColumn(ColumnDescriptor c, Judge judge)
        {
            c.Item().Height(20);
            c.Item().LineHorizontal(0.8f).LineColor(Colors.Black);
            c.Item().PaddingTop(3).AlignCenter().Text(judge.Name).Bold().FontSize(8).FontColor(Colors.Black);
            c.Item().AlignCenter().Text("Member, Board of Judges").FontSize(7).FontColor(Colors.Grey.Darken1);
        }

        private IContainer CellStyleHeader(IContainer container) => 
            container.Background(Colors.Grey.Lighten3).Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(3).AlignCenter().AlignMiddle();
        
        private IContainer CellStyleHeaderLeft(IContainer container) => 
            container.Background(Colors.Grey.Lighten3).Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(3).AlignLeft().AlignMiddle();
        
        private void headerCell(TableDescriptor table, string text, bool isBold = false)
        {
            var cell = table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignCenter().AlignMiddle();
            if (isBold) cell.Text(text).Bold().FontSize(8).FontColor(Colors.Black);
            else cell.Text(text).FontSize(8).FontColor(Colors.Grey.Darken3);
        }
        
        private void headerCellLeft(TableDescriptor table, string text) => 
            table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3).AlignLeft().AlignMiddle()
                 .Text(text).Bold().FontSize(8).FontColor(Colors.Black);
    }
}