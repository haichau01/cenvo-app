using Cenvo.Modules.Cafe.Domain;
using Cenvo.Modules.Cafe.Domain.Calculations;
using Cenvo.SharedKernel;

namespace Cenvo.Tests.Unit;

public class PaymentCalculatorTests
{
    [Fact]
    public void Cash_overpay_records_only_amount_due_and_returns_change()
    {
        var result = PaymentCalculator.Calculate(54000, [new(PaymentMethod.Cash, 100000)]);

        Assert.Equal(100000, result.CashReceived);
        Assert.Equal(46000, result.Change);
        var cash = Assert.Single(result.Recorded);
        Assert.Equal((PaymentMethod.Cash, 54000m), (cash.Method, cash.Amount));
    }

    [Fact]
    public void Split_transfer_and_cash_records_both()
    {
        var result = PaymentCalculator.Calculate(90000, [new(PaymentMethod.Transfer, 50000, "FT123"), new(PaymentMethod.Cash, 50000)]);

        Assert.Equal(10000, result.Change);
        Assert.Equal(50000, result.Recorded.Single(p => p.Method == PaymentMethod.Transfer).Amount);
        Assert.Equal(40000, result.Recorded.Single(p => p.Method == PaymentMethod.Cash).Amount);
    }

    [Fact]
    public void Exact_transfer_records_no_cash_line()
    {
        var result = PaymentCalculator.Calculate(30000, [new(PaymentMethod.Transfer, 30000)]);

        Assert.Equal(0, result.Change);
        Assert.DoesNotContain(result.Recorded, p => p.Method == PaymentMethod.Cash);
    }

    [Fact]
    public void Customer_paying_less_than_total_is_rejected() =>
        Assert.Throws<BusinessException>(() => PaymentCalculator.Calculate(54000, [new(PaymentMethod.Cash, 50000)]));

    [Fact]
    public void Non_cash_above_total_is_rejected() =>
        Assert.Throws<BusinessException>(() => PaymentCalculator.Calculate(54000, [new(PaymentMethod.Card, 60000)]));

    [Fact]
    public void Negative_amount_is_rejected() =>
        Assert.Throws<BusinessException>(() => PaymentCalculator.Calculate(0, [new(PaymentMethod.Cash, -1)]));
}
