using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace qenem.Models
{
    public class ListaSimulado
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SimuladoId { get; set; }

        [ForeignKey("SimuladoId")]
        public virtual Simulado Simulado { get; set; }

        [Required]
        public int QuestaoId { get; set; }
        /// <summary>
        /// Resposta selecionada pelo usuário (A, B, C, D, E)
        /// </summary>
        public string? RespostaUsuario { get; set; }

        /// <summary>
        /// Se a resposta está correta
        /// </summary>
        public bool? EstaCorreta { get; set; }

        /// <summary>
        /// Quando a questão foi respondida
        /// </summary>
        public DateTime? DataResposta { get; set; }

        /// <summary>
        /// Área da questão (matemática, linguagens, etc.)
        /// Armazenado aqui para facilitar cálculos de AcertosPorArea
        /// </summary>
        [StringLength(50)]
        public string? AreaQuestao { get; set; }
    }
}
