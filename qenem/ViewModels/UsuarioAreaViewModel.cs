using qenem.Models;

namespace qenem.ViewModels
{
    public class UsuarioAreaViewModel
    {
        public string IdUsuario { get; set; }
        public List<AreaInteresse> Materias { get; set; }
        public List<AreaInteresse> Linguagens { get; set; }
        public List<int> AreasSelecionadas { get; set; } = new();

        public string Mensagem { get; set; }
        public UsuarioAreaViewModel()
        {
            AreasSelecionadas = new List<int>();
            Materias = new List<AreaInteresse>();      // <--- Inicializa a lista de Matérias
            Linguagens = new List<AreaInteresse>();  // <--- Inicializa a lista de Linguagens
        }
    }
}
