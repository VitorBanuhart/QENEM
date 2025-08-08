using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace qenem.Models
{
    [Table("Usuario")]
    public class UsuarioModel
    {
        [Key]
        [Column("id_usuario")]
        [Display(Name ="Id")]
        public int Id { get; set; }

        [Column("email")]
        [Display(Name = "E-mail")]
        public string Email { get; set; }

        [Column("senha")]
        [Display(Name = "Senha")]
        public string Senha { get; set; }

        [Column("nome_completo")]
        [Display(Name = "Nome Completo")]
        public string NomeCompleto { get; set; }

        [Column("data_ultimoAcesso")]
        [Display(Name = "Data do Último Acesso")]
        [DataType(DataType.DateTime)]
        public DateTime DataUltimoAcesso { get; set; }

        [Column("bAdministrador")]
        [Display(Name = "Administrador")]
        public Boolean BAdministrador { get; set; }

        [Column("bExcluido")]
        [Display(Name = "Usuário Excluído")]
        public Boolean BExcluido { get; set; }

        [Column("pontuacao")]
        [Display(Name = "Pontuação")]
        public int Pontuacao { get; set; }
        public int IdAreaInteresse { get; set; }

        [Column("tema_usuario")]
        [Display(Name = "Tema")]
        public Boolean TemaUsuario { get; set; }

        [Column("numero_deLista")]
        [Display(Name = "Número de Lista")]
        public int NumeroDeLista { get; set; }

        [Column("numero_deSimulado")]
        [Display(Name = "Número de Simulado")]
        public int NumeroDeSimulado { get; set; }

        [Column("limite_diarioQuestoes")]
        [Display(Name = "Limite Diário Questões")]
        public int LimiteDiarioQuestoes { get; set; }
    }
}
