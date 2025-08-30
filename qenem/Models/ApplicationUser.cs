using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace qenem.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ApplicationUser() { }

        public virtual ICollection<UsuarioArea> UsuarioAreas { get; set; } = new List<UsuarioArea>();
    }
}
