namespace Pedidos.Domain.Entities;

/// <summary>Forma de pagamento do pedido (alinhada às formas usadas via Asaas).</summary>
public enum PaymentMethod
{
    Pix = 0,
    Boleto = 1,
    CreditCard = 2,
    Cash = 3
}
