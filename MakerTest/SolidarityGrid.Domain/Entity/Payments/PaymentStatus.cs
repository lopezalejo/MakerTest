namespace SolidarityGrid.Domain.Entity.Payments
{
    public class PaymentStatus
    {
        public enum PaymentStatusEnum : byte
        {
            Pendiente = 0,
            Asigado = 1, //Claimed
            Procesando = 2,
            Completado = 3,
            Fallido = 4
        }
    }
}
