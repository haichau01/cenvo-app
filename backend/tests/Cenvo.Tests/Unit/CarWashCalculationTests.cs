using Cenvo.Modules.CarWash.Domain;
using Cenvo.Modules.CarWash.Domain.Calculations;
using Cenvo.Modules.Organization.Contracts;
using Cenvo.SharedKernel;

namespace Cenvo.Tests.Unit;

public class CommissionCalculatorTests
{
    [Theory]
    [InlineData(100000, CommissionType.Percent, 20, 20000)]
    [InlineData(65000, CommissionType.Percent, 15, 9750)]
    [InlineData(55555, CommissionType.Percent, 10, 5556)] // 5555.5 rounds half away from zero
    [InlineData(450000, CommissionType.FixedAmount, 50000, 50000)]
    [InlineData(100000, CommissionType.Percent, 0, 0)]
    public void Service_commission_is_whole_vnd(decimal price, CommissionType type, decimal value, decimal expected) =>
        Assert.Equal(expected, CommissionCalculator.ForService(price, type, value));

    [Fact]
    public void Equal_split_adds_up_exactly_to_the_total()
    {
        var shares = CommissionCalculator.SplitEqually(20000, 3);

        Assert.Equal([6667m, 6667m, 6666m], shares);
        Assert.Equal(20000, shares.Sum());
    }

    [Fact]
    public void Split_without_staff_gives_nothing() => Assert.Empty(CommissionCalculator.SplitEqually(20000, 0));
}

public class PlateAndPhoneTests
{
    [Theory]
    [InlineData("51A-123.45", "51A12345")]
    [InlineData(" 51a 12345 ", "51A12345")]
    [InlineData("30K1-999.99", "30K199999")]
    public void Plates_are_normalized_so_spellings_match(string typed, string stored) =>
        Assert.Equal(stored, PlateNumber.Normalize(typed));

    [Theory]
    [InlineData("")]
    [InlineData("51-A")]
    [InlineData("51A-123.45.678.90")] // 13 characters
    public void Implausible_plates_are_rejected(string typed) =>
        Assert.Throws<BusinessException>(() => PlateNumber.Normalize(typed));

    [Theory]
    [InlineData("0909 123 456", "0909123456")]
    [InlineData("+84 909 123 456", "0909123456")]
    [InlineData("090.912.3456", "0909123456")]
    public void Phones_keep_digits_only(string typed, string stored) => Assert.Equal(stored, PhoneNumber.Normalize(typed));

    [Fact]
    public void Blank_phone_means_no_customer() => Assert.Null(PhoneNumber.Normalize("  "));

    [Fact]
    public void Too_short_phone_is_rejected() => Assert.Throws<BusinessException>(() => PhoneNumber.Normalize("12345"));
}

public class ImageFormatTests
{
    [Fact]
    public void Recognises_jpeg_png_and_webp_by_their_bytes()
    {
        Assert.Equal("image/jpeg", ImageFormat.Detect([0xFF, 0xD8, 0xFF, 0xE0, 0, 0, 0, 0, 0, 0, 0, 0])?.ContentType);
        Assert.Equal("image/png", ImageFormat.Detect([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0])?.ContentType);
        Assert.Equal("image/webp", ImageFormat.Detect("RIFF\0\0\0\0WEBP"u8)?.ContentType);
    }

    [Fact]
    public void Rejects_other_files_even_named_jpg()
    {
        Assert.Null(ImageFormat.Detect("<svg xmlns="u8));
        Assert.Null(ImageFormat.Detect("%PDF-1.7 abc"u8));
        Assert.Null(ImageFormat.Detect([]));
    }
}

public class WashTicketTests
{
    private static WashOffering Offering(string name, decimal price, CommissionType type, decimal commission)
    {
        var offering = new WashOffering();
        offering.Update(name, name, 30, type, commission, 1, isActive: true);
        offering.SetPrices([(VehicleType.Sedan, price)]);
        return offering;
    }

    private static WashTicket NewTicket()
    {
        var vehicle = Vehicle.Register("51A12345", VehicleType.Sedan);
        return WashTicket.Open(Guid.NewGuid(), "RX-1", vehicle, customer: null, walkInName: null, note: null);
    }

    private static readonly StaffMember[] TwoWashers = [new(Guid.NewGuid(), "A"), new(Guid.NewGuid(), "B")];

    [Fact]
    public void Completing_splits_commission_of_every_service_between_washers()
    {
        var ticket = NewTicket();
        ticket.AddService(Offering("Bọt tuyết", 100000, CommissionType.Percent, 20), membershipId: null); // 20,000
        ticket.AddService(Offering("Hút bụi", 40000, CommissionType.FixedAmount, 10000), membershipId: null); // 10,000
        ticket.AssignStaff(TwoWashers);

        ticket.Start(DateTime.UtcNow);
        ticket.Complete(DateTime.UtcNow);

        Assert.Equal(140000, ticket.Total);
        Assert.All(ticket.Staff, s => Assert.Equal(15000, s.Commission));
        var completed = Assert.IsType<WashTicketCompleted>(Assert.Single(ticket.DomainEvents));
        Assert.Equal(2, completed.OfferingIds.Count);
    }

    [Fact]
    public void Starting_needs_a_washer() =>
        Assert.Throws<BusinessException>(() =>
        {
            var ticket = NewTicket();
            ticket.AddService(Offering("Rửa", 60000, CommissionType.Percent, 20), null);
            ticket.Start(DateTime.UtcNow);
        });

    [Fact]
    public void Services_are_locked_once_paid()
    {
        var ticket = NewTicket();
        ticket.AddService(Offering("Rửa", 60000, CommissionType.Percent, 20), null);
        ticket.Pay(Guid.NewGuid(), null, [new(Cenvo.Modules.Cafe.Domain.PaymentMethod.Cash, 60000)], DateTime.UtcNow);

        Assert.Throws<BusinessException>(() => ticket.AddService(Offering("Hút bụi", 40000, CommissionType.FixedAmount, 0), null));
    }

    [Fact]
    public void Delivering_needs_payment()
    {
        var ticket = NewTicket();
        ticket.AddService(Offering("Rửa", 60000, CommissionType.Percent, 20), null);
        ticket.AssignStaff(TwoWashers);
        ticket.Start(DateTime.UtcNow);
        ticket.Complete(DateTime.UtcNow);

        Assert.Throws<BusinessException>(() => ticket.Deliver(DateTime.UtcNow));
    }

    [Fact]
    public void Cancelling_a_washed_ticket_needs_a_manager_and_reverses_stock()
    {
        var ticket = NewTicket();
        ticket.AddService(Offering("Rửa", 60000, CommissionType.Percent, 20), null);
        ticket.AssignStaff(TwoWashers);
        ticket.Start(DateTime.UtcNow);
        ticket.Complete(DateTime.UtcNow);

        Assert.Throws<BusinessException>(() => ticket.Cancel("Khách đổi ý", byManager: false, DateTime.UtcNow));
        ticket.Cancel("Khách đổi ý", byManager: true, DateTime.UtcNow);
        Assert.True(ticket.DomainEvents.OfType<WashTicketCancelled>().Single().WasCompleted);
    }
}
