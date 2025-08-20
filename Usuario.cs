using System;
using System.ComponentModel.DataAnnotations;

namespace LaOriginalBackend.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required] public string PrimerNombre { get; set; }
        [Required] public string SegundoNombre { get; set; }
        [Required] public string PrimerApellido { get; set; }
        [Required] public string SegundoApellido { get; set; }

        [Required] public string NIT { get; set; }
        [Required] public string CUI { get; set; }

        [Required] public DateTime FechaNacimiento { get; set; }
        [Required] public DateTime FechaIngreso { get; set; }

        [Required] public string Celular { get; set; }
        [Required] public string Genero { get; set; }
        [Required] public string Estado { get; set; }
        [Required] public string Direccion { get; set; }

        [Required] public string Cargo { get; set; }
        [Required][EmailAddress] public string Email { get; set; }

        public string Username { get; set; } // Se genera automáticamente
        [Required] public string PasswordHash { get; set; } // Guardamos en hash
    }
}
