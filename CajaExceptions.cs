// Services/Caja/CajaExceptions.cs
namespace LaOriginalBackend.Services
{
    /// <summary>
    /// Se lanza cuando un egreso intenta exceder el saldo disponible en la caja abierta.
    /// </summary>
    public class CajaFondosInsuficientesException : Exception
    {
        public decimal Disponible { get; }
        public decimal Solicitado { get; }

        public CajaFondosInsuficientesException(decimal disponible, decimal solicitado)
            : base($"Fondos insuficientes. Disponible Q {disponible:n2}, solicitado Q {solicitado:n2}.")
        {
            Disponible = disponible;
            Solicitado = solicitado;
        }
    }
}
