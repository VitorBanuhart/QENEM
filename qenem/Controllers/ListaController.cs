// Controllers/ListaController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using qenem.Data;
using qenem.Models;
using qenem.Services;
using System.Security.Claims;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Fluent;
using System.IO;

namespace qenem.Controllers
{
    [Authorize] // Apenas usuários logados podem acessar
    public class ListaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly QuestionService _questionService;

        // Limites definidos pelos requisitos
        private const int MAX_LISTAS_POR_USUARIO = 10;
        private const int MAX_QUESTOES_POR_LISTA = 180;

        public ListaController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, QuestionService questionService)
        {
            _context = context;
            _userManager = userManager;
            _questionService = questionService;
        }

        // GET: /Lista (Página principal que mostra as listas do usuário)
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var listasDoUsuario = await _context.Listas
                .Where(l => l.UsuarioId == userId)
                .OrderBy(l => l.Nome)
                .ToListAsync();

            return View(listasDoUsuario);
        }

        // POST: /Lista/Criar
        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] ListaCreateModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Nome) || model.Nome.Length > 30)
            {
                return BadRequest(new { success = false, message = "O nome da lista é inválido ou excede 30 caracteres." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var totalListas = await _context.Listas.CountAsync(l => l.UsuarioId == userId);
            if (totalListas >= MAX_LISTAS_POR_USUARIO) // RNF 4.3 e 4.4
            {
                return BadRequest(new { success = false, message = "msg_maximo_lista" }); // Mensagem de erro do requisito
            }

            var novaLista = new Lista
            {
                Nome = model.Nome,
                UsuarioId = userId
            };

            _context.Listas.Add(novaLista);
            await _context.SaveChangesAsync();

            // Retorna a lista criada para que o front-end possa atualizar a UI
            return Json(new { success = true, lista = novaLista });
        }

        // POST: /Lista/AdicionarQuestao
        [HttpPost]
        public async Task<IActionResult> AdicionarQuestao([FromBody] AdicionarQuestaoModel model)
        {
            if (model == null || model.ListaId <= 0 || string.IsNullOrEmpty(model.QuestaoId))
            {
                return BadRequest(new { success = false, message = "Dados inválidos." });
            }

            Console.WriteLine("ESTOU AQUI");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var lista = await _context.Listas
                .Include(l => l.ListaQuestoes)
                .FirstOrDefaultAsync(l => l.Id == model.ListaId && l.UsuarioId == userId);

            if (lista == null)
            {
                return NotFound(new { success = false, message = "Lista não encontrada ou não pertence ao usuário." });
            }

            // RNF 4.2 - Verifica o limite de questões
            if (lista.ListaQuestoes.Count >= MAX_QUESTOES_POR_LISTA)
            {
                return BadRequest(new { success = false, message = "Esta lista já atingiu o limite de 180 questões." });
            }

            // Verifica se a questão já foi adicionada
            if (lista.ListaQuestoes.Any(q => q.QuestaoId == model.QuestaoId))
            {
                return BadRequest(new { success = false, message = "Esta questão já foi adicionada a esta lista." });
            }

            var novaQuestaoNaLista = new ListaQuestao
            {
                ListaId = model.ListaId,
                QuestaoId = model.QuestaoId
            };

            _context.ListaQuestoes.Add(novaQuestaoNaLista);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Questão adicionada com sucesso!" });
        }

        // POST: /Lista/Excluir/{id}
        [HttpPost]
        public async Task<IActionResult> Excluir(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var lista = await _context.Listas.FirstOrDefaultAsync(l => l.Id == id && l.UsuarioId == userId);

            if (lista == null)
            {
                return NotFound(new { success = false, message = "Lista não encontrada." });
            }

            _context.Listas.Remove(lista); // O EF Core removerá em cascata os ListaQuestoes associados
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Lista excluída com sucesso." });
        }

        [HttpGet]
        public async Task<IActionResult> ObterListasDoUsuario()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var listas = await _context.Listas
                .Where(l => l.UsuarioId == userId)
                .Select(l => new { l.Id, l.Nome }) // Seleciona apenas os dados necessários
                .ToListAsync();

            return Json(listas);
        }

        // GET /Lista/Questoes?id=123  -> carrega view com as questões da lista
        public async Task<IActionResult> Questoes(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var lista = await _context.Listas
                .Include(l => l.ListaQuestoes)
                .FirstOrDefaultAsync(l => l.Id == id && l.UsuarioId == userId);

            if (lista == null) return NotFound();

            List<Question> todasQuestoes;
            try
            {
                todasQuestoes = _questionService.GetAllQuestions();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro ao carregar questões do disco: {ex.Message}");
            }

            // Normaliza os IDs armazenados no banco (pode já ser full path; Path.GetFullPath também lida com caminhos "limpos")
            var questaoIdsNaListaNormalized = lista.ListaQuestoes
                .Select(lq =>
                {
                    try { return Path.GetFullPath(lq.QuestaoId).Trim(); }
                    catch { return lq.QuestaoId?.Trim() ?? ""; }
                })
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();

            var todasQuestoesByFullNormalized = todasQuestoes.ToDictionary(
                q =>
                {
                    try { return Path.GetFullPath(q.UniqueId).Trim(); }
                    catch { return q.UniqueId?.Trim() ?? ""; }
                },
                StringComparer.OrdinalIgnoreCase);

            var ordenadas = new List<Question>();

            foreach (var qidNormalized in questaoIdsNaListaNormalized)
            {
                // 1) tenta encontrar por caminho normalizado (case-insensitive)
                if (todasQuestoesByFullNormalized.TryGetValue(qidNormalized, out var qMatch))
                {
                    ordenadas.Add(qMatch);
                    continue;
                }

                // 2) fallback: tentar comparar apenas pelo nome do arquivo (ex: questionsdetails.json)
                var fileName = Path.GetFileName(qidNormalized);
                var fallback = todasQuestoes.FirstOrDefault(q =>
                    string.Equals(Path.GetFileName(q.UniqueId), fileName, StringComparison.OrdinalIgnoreCase));
                if (fallback != null)
                {
                    ordenadas.Add(fallback);
                    continue;
                }

                // 3) (opcional) procurar substrings - útil se o DB armazenou caminhos com drivers diferentes ou prefixos
                var partial = todasQuestoes.FirstOrDefault(q =>
                    q.UniqueId != null && q.UniqueId.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
                if (partial != null)
                {
                    ordenadas.Add(partial);
                    continue;
                }

                // se não achar nada, ignora essa entrada (ou você pode logar)
                // aqui você pode logar: Logger.LogWarning($"Questão {qidNormalized} não encontrada no JSON.");
            }

            ViewBag.ListaId = id;
            ViewBag.NomeLista = lista.Nome;

            return View("MostrarQuestoes", ordenadas);
        }


        // GET JSON: /Lista/ObterQuestoesDaLista?id=123
        [HttpGet]
        public async Task<IActionResult> ObterQuestoesDaLista(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { success = false, message = "Usuário não autenticado." });

            var lista = await _context.Listas
                .Include(l => l.ListaQuestoes)
                .FirstOrDefaultAsync(l => l.Id == id && l.UsuarioId == userId);

            if (lista == null) return NotFound(new { success = false, message = "Lista não encontrada." });

            List<Question> todasQuestoes;
            try
            {
                todasQuestoes = _questionService.GetAllQuestions();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Erro ao carregar questões.", details = ex.Message });
            }

            var questaoIdsNaLista = lista.ListaQuestoes.Select(lq => lq.QuestaoId).ToList();

            var ordenadas = questaoIdsNaLista
                .Select(qid => todasQuestoes.FirstOrDefault(q => q.UniqueId == qid))
                .Where(q => q != null)
                .Select(q => new
                {
                    uniqueId = q.UniqueId,
                    title = q.title,
                    year = q.year,
                    discipline = q.discipline,
                    language = q.language,
                    context = q.context,
                    alternativesIntroduction = q.alternativesIntroduction,
                    correctAlternative = q.correctAlternative,
                    alternatives = q.alternatives?.Select(a => new { letter = a.letter, text = a.text }).ToList()
                })
                .ToList();

            return Json(new { success = true, questoes = ordenadas });
        }

        // POST: RemoverQuestaoDaLista { ListaId, QuestaoId }
        [HttpPost]
        public async Task<IActionResult> RemoverQuestaoDaLista([FromBody] RemoverQuestaoDto dto)
        {
            if (dto == null) return BadRequest(new { success = false, message = "Dados inválidos." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { success = false, message = "Usuário não autenticado." });

            var lista = await _context.Listas.FirstOrDefaultAsync(l => l.Id == dto.ListaId && l.UsuarioId == userId);
            if (lista == null) return NotFound(new { success = false, message = "Lista não encontrada." });

            var rel = await _context.ListaQuestoes.FirstOrDefaultAsync(lq => lq.ListaId == dto.ListaId && lq.QuestaoId == dto.QuestaoId);
            if (rel == null) return NotFound(new { success = false, message = "Questão não encontrada nessa lista." });

            _context.ListaQuestoes.Remove(rel);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Questão removida da lista." });
        }

        [HttpPost]
        public async Task<IActionResult> RenomearLista([FromBody] RenomearListaDto dto)
        {
            if (dto == null || dto.ListaId <= 0 || string.IsNullOrWhiteSpace(dto.NovoNome) || dto.NovoNome.Length > 30)
            {
                return BadRequest(new { success = false, message = "Dados inválidos ou nome excede 30 caracteres." });
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { success = false, message = "Usuário não autenticado." });
            var lista = await _context.Listas.FirstOrDefaultAsync(l => l.Id == dto.ListaId && l.UsuarioId == userId);
            if (lista == null) return NotFound(new { success = false, message = "Lista não encontrada." });
            lista.Nome = dto.NovoNome;
            _context.Listas.Update(lista);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Lista renomeada com sucesso.", novoNome = dto.NovoNome });
        }

        public async Task<IActionResult> BaixarPDF(int? listaId)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            if (listaId == null || listaId.Value <= 0)
                return BadRequest("ID da lista inválido.");

            // Obtenha as questões da lista de forma síncrona
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var lista = await _context.Listas
                .Include(l => l.ListaQuestoes)
                .FirstOrDefaultAsync(l => l.Id == listaId.Value && l.UsuarioId == userId);

            if (lista == null) return NotFound();

            List<Question> todasQuestoes;
            try
            {
                todasQuestoes = _questionService.GetAllQuestions();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro ao carregar questões do disco: {ex.Message}");
            }

            var questaoIdsNaListaNormalized = lista.ListaQuestoes
                .Select(lq =>
                {
                    try { return Path.GetFullPath(lq.QuestaoId).Trim(); }
                    catch { return lq.QuestaoId?.Trim() ?? ""; }
                })
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();

            var todasQuestoesByFullNormalized = todasQuestoes.ToDictionary(
                q =>
                {
                    try { return Path.GetFullPath(q.UniqueId).Trim(); }
                    catch { return q.UniqueId?.Trim() ?? ""; }
                },
                StringComparer.OrdinalIgnoreCase);

            var ordenadas = new List<Question>();

            foreach (var qidNormalized in questaoIdsNaListaNormalized)
            {
                if (todasQuestoesByFullNormalized.TryGetValue(qidNormalized, out var qMatch))
                {
                    ordenadas.Add(qMatch);
                    continue;
                }

                var fileNameNormalized = Path.GetFileName(qidNormalized);
                var fallback = todasQuestoes.FirstOrDefault(q =>
                    string.Equals(Path.GetFileName(q.UniqueId), fileNameNormalized, StringComparison.OrdinalIgnoreCase));
                if (fallback != null)
                {
                    ordenadas.Add(fallback);
                    continue;
                }

                var partial = todasQuestoes.FirstOrDefault(q =>
                    q.UniqueId != null && q.UniqueId.EndsWith(fileNameNormalized, StringComparison.OrdinalIgnoreCase));
                if (partial != null)
                {
                    ordenadas.Add(partial);
                    continue;
                }
            }

            // ----------------------------
            // BLOCO ATUALIZADO: salvar MARKDOWN + baixar imagens e EMBUTIR no PDF
            // ----------------------------
            try
            {
                var baseDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "markdown", $"lista_{listaId.Value}");
                var imagesDir = Path.Combine(baseDir, "images");
                Directory.CreateDirectory(imagesDir);

                // Mapas para usar depois na geração do PDF
                var contextByQuestion = new Dictionary<Question, string>();
                var altIntroByQuestion = new Dictionary<Question, string>();
                var imagesByQuestion = new Dictionary<Question, List<string>>();

                using var http = new System.Net.Http.HttpClient()
                {
                    Timeout = System.TimeSpan.FromSeconds(10)
                };

                int mdIndex = 1;
                foreach (var q in ordenadas)
                {
                    // Texto original
                    var contextText = q.context ?? "";
                    var altIntroText = q.alternativesIntroduction ?? "";

                    var localImages = new List<string>();
                    int imgCounter = 0;

                    // Regex para encontrar imagens em Markdown: ![alt](https://...)
                    var imgRegex = new System.Text.RegularExpressions.Regex(@"\!\[.*?\]\((https?:\/\/[^\s\)]+)\)",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                    // Função local para processar um texto (download imagens e substituir por placeholder)
                    async Task<string> ProcessTextAsync(string text)
                    {
                        if (string.IsNullOrWhiteSpace(text)) return text;

                        var result = text;
                        var matches = imgRegex.Matches(text);
                        foreach (System.Text.RegularExpressions.Match m in matches)
                        {
                            var url = m.Groups[1].Value;
                            // tenta baixar
                            try
                            {
                                var ext = Path.GetExtension(url);
                                if (string.IsNullOrWhiteSpace(ext) || ext.Length > 6) ext = ".png";
                                var imgFileName = SanitizeFileName($"{mdIndex}_{imgCounter}{ext}");
                                var imgPath = Path.Combine(imagesDir, imgFileName);

                                // evita baixar novamente se já existe
                                if (!System.IO.File.Exists(imgPath))
                                {
                                    var bytes = await http.GetByteArrayAsync(url);
                                    System.IO.File.WriteAllBytes(imgPath, bytes);
                                }

                                // registra e substitui texto por placeholder
                                localImages.Add(imgPath);
                                var placeholder = $"[[IMAGE_{imgCounter}]]";
                                result = result.Replace(m.Value, "\n" + placeholder + "\n");
                                imgCounter++;
                            }
                            catch
                            {
                                // falha ao baixar — substitui por texto alternativo simples (remove markdown)
                                result = result.Replace(m.Value, "");
                            }
                        }

                        return result;
                    }

                    // Processa ambos (context e alternativas)
                    var processedContext = await ProcessTextAsync(contextText);
                    var processedAltIntro = await ProcessTextAsync(altIntroText);

                    // Salva os md (opcional) — aqui salvamos o texto processado (sem links remotos)
                    var uniquePart = !string.IsNullOrWhiteSpace(q.UniqueId) ? q.UniqueId : q.id.ToString();
                    var safeName = SanitizeFileName($"{mdIndex}_{uniquePart}");

                    var sbContext = new System.Text.StringBuilder();
                    sbContext.AppendLine($"# Questão {mdIndex}");
                    sbContext.AppendLine();
                    sbContext.AppendLine($"**Título:** {q.title}");
                    if (q.year != 0) { sbContext.AppendLine(); sbContext.AppendLine($"**Ano:** {q.year}"); }
                    if (!string.IsNullOrWhiteSpace(q.discipline)) { sbContext.AppendLine(); sbContext.AppendLine($"**Disciplina:** {q.discipline}"); }
                    if (!string.IsNullOrWhiteSpace(q.language)) { sbContext.AppendLine(); sbContext.AppendLine($"**Língua:** {q.language}"); }

                    sbContext.AppendLine();
                    sbContext.AppendLine("---");
                    sbContext.AppendLine();
                    sbContext.AppendLine(processedContext ?? "");

                    var contextPath = Path.Combine(baseDir, $"{safeName}_context.md");
                    System.IO.File.WriteAllText(contextPath, sbContext.ToString());

                    var sbAlt = new System.Text.StringBuilder();
                    sbAlt.AppendLine($"# Introdução às Alternativas - Questão {mdIndex}");
                    sbAlt.AppendLine();
                    sbAlt.AppendLine(processedAltIntro ?? "");
                    sbAlt.AppendLine();
                    sbAlt.AppendLine("---");
                    sbAlt.AppendLine();
                    if (q.alternatives != null && q.alternatives.Any())
                    {
                        sbAlt.AppendLine("## Alternativas");
                        foreach (var alt in q.alternatives)
                            sbAlt.AppendLine($"- **{alt.letter})** {alt.text}");
                    }

                    var alternativesPath = Path.Combine(baseDir, $"{safeName}_alternatives.md");
                    System.IO.File.WriteAllText(alternativesPath, sbAlt.ToString());

                    // grava nos dicionários para uso no PDF
                    contextByQuestion[q] = processedContext ?? "";
                    altIntroByQuestion[q] = processedAltIntro ?? "";
                    imagesByQuestion[q] = new List<string>(localImages);

                    mdIndex++;
                }

                // Agora: geração do PDF usando os textos processados + imagens baixadas abaixo
                var documentInner = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);

                        page.Header().AlignCenter().Text($"Lista ID {listaId.Value} - Questões").FontSize(16).Bold();

                        page.Content().PaddingVertical(8).Column(col =>
                        {
                            int index = 1;
                            foreach (var localQ in ordenadas)
                            {
                                col.Item().Element(item =>
                                {
                                    item.Column(questionCol =>
                                    {
                                        // Título
                                        questionCol.Item().Text($"{index}. {localQ.title} {(localQ.year != 0 ? $"({localQ.year})" : "")}")
                                                             .FontSize(12).Bold();

                                        // Meta
                                        var meta = localQ.discipline ?? "";
                                        if (!string.IsNullOrWhiteSpace(localQ.language)) meta += $" • Língua: {localQ.language}";
                                        if (!string.IsNullOrWhiteSpace(meta)) questionCol.Item().Text(meta).FontSize(10).Italic();

                                        // Função que renderiza texto+imagens (procura placeholders [[IMAGE_n]])
                                        void RenderTextAndImages(string text, List<string> images)
                                        {
                                            if (string.IsNullOrWhiteSpace(text))
                                                return;

                                            // Split por placeholder (mantendo-os)
                                            var parts = System.Text.RegularExpressions.Regex.Split(text, "(\\[\\[IMAGE_\\d+\\]\\])");

                                            foreach (var part in parts)
                                            {
                                                if (string.IsNullOrWhiteSpace(part)) continue;

                                                var m = System.Text.RegularExpressions.Regex.Match(part, "\\[\\[IMAGE_(\\d+)\\]\\]");
                                                if (m.Success)
                                                {
                                                    // índice da imagem referente a esta questão
                                                    if (int.TryParse(m.Groups[1].Value, out int imgIdx))
                                                    {
                                                        if (images != null && imgIdx >= 0 && imgIdx < images.Count)
                                                        {
                                                            try
                                                            {
                                                                var imgPath = images[imgIdx];
                                                                if (System.IO.File.Exists(imgPath))
                                                                {
                                                                    var bytes = System.IO.File.ReadAllBytes(imgPath);
                                                                    // insere a imagem (ajuste a altura conforme necessário)
                                                                    questionCol.Item().Image(bytes);
                                                                }
                                                            }
                                                            catch
                                                            {
                                                                // falha ao inserir imagem: ignora
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    // texto comum
                                                    questionCol.Item().Text(part).FontSize(11);
                                                }
                                            }
                                        }

                                        // Conteúdo: context
                                        if (contextByQuestion.TryGetValue(localQ, out var ctxt))
                                            RenderTextAndImages(ctxt, imagesByQuestion.GetValueOrDefault(localQ, new List<string>()));

                                        // introdução das alternativas
                                        if (altIntroByQuestion.TryGetValue(localQ, out var altTxt) && !string.IsNullOrWhiteSpace(altTxt))
                                            RenderTextAndImages(altTxt, imagesByQuestion.GetValueOrDefault(localQ, new List<string>()));

                                        // alternativas em texto
                                        if (localQ.alternatives != null && localQ.alternatives.Any())
                                        {
                                            questionCol.Item().Column(altCol =>
                                            {
                                                foreach (var alt in localQ.alternatives)
                                                {
                                                    altCol.Item().Text($"{alt.letter}) {alt.text}").FontSize(10);
                                                }
                                            });
                                        }

                                        // separador
                                        questionCol.Item().PaddingTop(6).LineHorizontal(1);
                                    });
                                });

                                index++;
                            }
                        });

                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.Span("Página ");
                            x.CurrentPageNumber();
                            x.Span(" de ");
                            x.TotalPages();
                        });
                    });
                });

                // substitui o document original pelo documentInner gerado com imagens
                // (vamos gerar direto o PDF depois do try/catch)
                var streamInner = new MemoryStream();
                documentInner.GeneratePdf(streamInner);
                streamInner.Position = 0;

                var fileNameInner = $"Lista_{listaId.Value}_Questoes.pdf";
                return File(streamInner, "application/pdf", fileNameInner);
            }
            catch (Exception ex)
            {
                // se der erro ao baixar/salvar imagens, continuamos sem imagens
                // se preferir, rethrow ou retornar erro 500
                // _logger?.LogWarning(ex, "Falha ao processar markdown/imagens: {Message}", ex.Message);
            }


            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);

                    page.Header().AlignCenter().Text($"Lista ID {listaId.Value} - Questões")
                                  .FontSize(16).Bold();

                    page.Content().PaddingVertical(8).Column(col =>
                    {
                        int index = 1;
                        foreach (var localQ in ordenadas)
                        {
                            col.Item().Element(item =>
                            {
                                item.Column(questionCol =>
                                {
                                    questionCol.Item().Text($"{index}. {localQ.title} {(localQ.year != 0 ? $"({localQ.year})" : "")}")
                                                      .FontSize(12).Bold();

                                    var meta = localQ.discipline ?? "";
                                    if (!string.IsNullOrWhiteSpace(localQ.language))
                                        meta += $" • Língua: {localQ.language}";
                                    if (!string.IsNullOrWhiteSpace(meta))
                                        questionCol.Item().Text(meta).FontSize(10).Italic();

                                    if (!string.IsNullOrWhiteSpace(localQ.context))
                                        questionCol.Item().Text(localQ.context).FontSize(11);

                                    if (!string.IsNullOrWhiteSpace(localQ.alternativesIntroduction))
                                        questionCol.Item().Text(localQ.alternativesIntroduction).FontSize(10).Italic();

                                    if (localQ.alternatives != null && localQ.alternatives.Any())
                                    {
                                        questionCol.Item().Column(altCol =>
                                        {
                                            foreach (var alt in localQ.alternatives)
                                            {
                                                altCol.Item().Text($"{alt.letter}) {alt.text}").FontSize(10);
                                            }
                                        });
                                    }

                                    questionCol.Item().PaddingTop(6).LineHorizontal(1);
                                });
                            });

                            index++;
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
                });
            });

            var stream = new MemoryStream();
            document.GeneratePdf(stream);
            stream.Position = 0;

            var fileName = $"Lista_{listaId.Value}_Questoes.pdf";
            return File(stream, "application/pdf", fileName);

            // Local function para sanitizar nomes de arquivo
            string SanitizeFileName(string name)
            {
                if (string.IsNullOrEmpty(name)) return "file";
                var invalids = Path.GetInvalidFileNameChars();
                foreach (var c in invalids)
                {
                    name = name.Replace(c, '_');
                }
                return name;
            }
        }
    }

    public class RemoverQuestaoDto { public int ListaId { get; set; } public string QuestaoId { get; set; } = ""; }
    public class CriarListaDto { public string Nome { get; set; } = ""; }

    // Modelos auxiliares para receber dados do front-end
    public class ListaCreateModel { public string Nome { get; set; } }
    public class AdicionarQuestaoModel { public int ListaId { get; set; } public string QuestaoId { get; set; } }
    public class RenomearListaDto { public int ListaId { get; set; } public string NovoNome { get; set; } }
}