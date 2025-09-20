using System.ComponentModel.DataAnnotations;

namespace qenem.Models
{
   
    public class RespostaUsuario
    {
        [Key]
        public int Id { get; set; }
        public int QuestaoId { get; set; }
        public string? Resposta { get; set; }
        public bool? EstaCorreta { get; set; }
        public DateTime? DataResposta { get; set; }
    }
}
