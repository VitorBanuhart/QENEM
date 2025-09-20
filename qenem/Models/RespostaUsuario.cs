using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace qenem.Models
{
   
    public class RespostaUsuario
    {
        [Key]
        public int Id { get; set; }

        public int SimuladoId { get; set; }

        [ForeignKey("SimuladoId")]
        public Simulado? Simulado { get; set; }
        public string QuestaoId { get; set; }
        public string? Resposta { get; set; }
        public bool? EstaCorreta { get; set; }
        public DateTime? DataResposta { get; set; }
    }
}
