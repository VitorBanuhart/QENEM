using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using qenem.Data;
using qenem.DTO;
using qenem.Models;
using qenem.Services;
using qenem.ViewModels;

namespace qenem.Controllers
{
    public class SimuladoController : Controller
    {
        private readonly SimuladoService _simuladoService;
        private readonly QuestionService _questionService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly PontosService _pontosService;

        public SimuladoController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SimuladoService simuladoService, QuestionService questionService, PontosService pontosService)
        {
            _context = context;
            _userManager = userManager;
            _simuladoService = simuladoService;
            _questionService = questionService;
            _pontosService = pontosService;
        }
        public async Task<IActionResult> Index()
        {
            var simulados = await ObterSimuladosUsuario();
            return View(simulados);
        }

        [HttpGet]
        public IActionResult ListarSimulados()
        {
            return View();
        }


        [HttpGet]
        public IActionResult CriarSimulado()
        {
            // Retorna a view para o formulário de criação do simulado
            return View();
        }

        [HttpPost]
        //[ValidateAntiForgeryToken] 
        public async Task<IActionResult> CriarSimulado([FromBody] CriarSimuladoRequest data)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Usuário não autenticado." });
                }

                var novoSimulado = await _simuladoService.CriarSimulado(data, user.Id);

                if (novoSimulado != null)
                {
                    return Json(new { success = true, simulado = new { Id = novoSimulado.Id } });
                }

                // Simulado nulo
                return Json(new { success = false, message = "Não foi possível criar o simulado." });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ocorreu um erro inesperado. Tente novamente." });
            }
        }

        public async Task<IActionResult> RegistrarResposta([FromBody] RespostaUsuario data)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Usuário não autenticado." });
                }

                var novaResposta= await _simuladoService.RegistrarResposta(data.SimuladoId, data.QuestaoId, data.Resposta, user.Id);
                if (novaResposta != null)
                {
                    return Json(new { success = true, resposta = new { Id = novaResposta.Id } });
                }

                return Json(new { success = false, message = "Não foi possível registrar a resposta." });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ocorreu um erro inesperado. Tente novamente." });
            }
        }


        /// <summary>
        /// carrega dados para a view do simulado com questao inicial ou atual do progresso salvo
        /// </summary>
        /// <param name="id"></param>
        /// <param name="questaoIndex"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> RealizaSimulado(int id, int questaoIndex = 0)
        { 
           
            var simulado = await _context.Simulados //buscar o simulado
                .Include(s => s.Respostas)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (simulado == null)
                return NotFound();

            if (simulado.Finalizado)
            {
                return RedirectToAction("Resultado", new { id = simulado.Id });
            }

            var questoesDoSimulado = await _simuladoService.ObterQuestoesSimulado(id);
            //TO DO:
            //entender a lógica para redirecionar p/ resultado
            if (questaoIndex < 0 || questaoIndex >= questoesDoSimulado.Count)
            {
                //await _simuladoService.FinalizarSimulado(simulado.Id, /* tempoGasto */ null);
                return RedirectToAction("Resultado", new { id = simulado.Id });
            }
            var questaoAtual = questoesDoSimulado[questaoIndex];

            var respostasSalvas = (simulado.Respostas ?? new List<RespostaUsuario>())
                .Where(r => !string.IsNullOrEmpty(r.Resposta))
                .ToDictionary(r => r.QuestaoId, r => r.Resposta);

            var respostaUsuario = respostasSalvas.ContainsKey(questaoAtual.UniqueId) ? respostasSalvas[questaoAtual.UniqueId] : null;
            var dadosView = new SimuladoViewModel
            {
                SimuladoId = simulado.Id,
                SimuladoNome = simulado.Nome,
                TotalQuestoes = questoesDoSimulado.Count,
                QuestaoAtualIndex = questaoIndex,
                QuestaoAtual = questaoAtual,
                RespostaUsuario = respostaUsuario,
                RespostasSalvas = respostasSalvas
            };


            return View(dadosView);
        }

        [HttpGet]
        public async Task<IActionResult> Resultado(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var simulado = await _context.Simulados
                .FirstOrDefaultAsync(s => s.Id == id);
            if (simulado != null && !simulado.Finalizado)
            {
                await _simuladoService.FinalizarSimulado(simulado);
            }
            var resultado = await _simuladoService.ObterResultadoSimulado(id);
            _pontosService.PontosSimulado(user, resultado.TotalAcertos, resultado.TotalQuestoes);
            return View(resultado);
        }

        //[HttpGet]
        //public async Task<IActionResult> Resultado(int id)
        //{
        //    // Busca o simulado
        //    var simulado = await _context.Simulados
        //        .FirstOrDefaultAsync(s => s.Id == id);

        //    if (simulado == null)
        //        return NotFound();

        //    // Busca as respostas do usuário
        //    var respostas = await _context.RespostasUsuario
        //        .Where(r => r.SimuladoId == id)
        //        .ToListAsync();

        //    // Busca as questões do simulado
        //    var questoes = await _context.ListaSimulados
        //        .Where(q => q.SimuladoId == id)
        //        .ToListAsync();

        //    // Faz join das respostas com as questões para obter a área
        //    var respostasComArea = respostas
        //        .Join(
        //            questoes,
        //            r => r.QuestaoId,
        //            q => q.UniqueId,
        //            (r, q) => new
        //            {
        //                r.Resposta,
        //                r.EstaCorreta,
        //                Area = q.AreaQuestao
        //            }
        //        )
        //        .ToList();

        //    var totalQuestoes = respostasComArea.Count;
        //    var totalAcertos = respostasComArea.Count(r => r.EstaCorreta == true);

        //    // Agrupa por área usando AreaResultadoDto
        //    var resultadosPorArea = respostasComArea
        //        .GroupBy(r => r.Area)
        //        .Select(g => new AreaResultadoDto
        //        {
        //            Area = g.Key,
        //            TotalQuestoes = g.Count(),
        //            QuestoesRespondidas = g.Count(r => !string.IsNullOrEmpty(r.Resposta)),
        //            Acertos = g.Count(r => r.EstaCorreta == true),
        //            Porcentagem = g.Count() == 0 ? 0 : (double)g.Count(r => r.EstaCorreta == true) / g.Count() * 100
        //        })
        //        .ToList();

        //    // Cria DTO para a view
        //    var resultadoDto = new SimuladoResultadoDto
        //    {
        //        SimuladoId = simulado.Id,
        //        Nome = simulado.Nome,
        //        DataCriacao = simulado.DataCriacao,
        //        TempoGasto = simulado.TempoGasto,
        //        TotalQuestoes = totalQuestoes,
        //        QuestoesRespondidas = respostasComArea.Count(r => !string.IsNullOrEmpty(r.Resposta)),
        //        TotalAcertos = totalAcertos,
        //        PorcentagemGeral = totalQuestoes == 0 ? 0 : (double)totalAcertos / totalQuestoes * 100,
        //        ResultadosPorArea = resultadosPorArea
        //    };

        //    return View(resultadoDto);
        //}




        private async Task<List<Simulado>> ObterSimuladosUsuario()
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
            {
                return new List<Simulado>(); // Retorna uma lista vazia se o usuário não existir.
            }

            var userId = usuario.Id;

            var simuladosExistentes = await _context.Simulados
                .Where(s => s.UsuarioId == userId)
                .ToListAsync();
            if (!simuladosExistentes.Any())
            {
                return new List<Simulado>();
            }
            return simuladosExistentes;
        }

        [HttpGet]
        public async Task<IActionResult> GetTempo(int id)
        {
            var tempo = await _simuladoService.GetTempoGastoAsync(id);

            // envia para o front em segundos
            return Json(new { seconds = (int)tempo.TotalSeconds });
        }

        [HttpPost]
        public async Task<IActionResult> SetTempo(int id, [FromBody] TempoDto dto)
        {
            if (dto == null || dto.Seconds < 0)
                return BadRequest("seconds inválido");

            var tempo = TimeSpan.FromSeconds(dto.Seconds);

            await _simuladoService.SetTempoGastoAsync(id, tempo);
            return Ok();
        }   }
}
